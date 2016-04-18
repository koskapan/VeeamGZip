using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.IO.Compression;

namespace VeeamGZip
{

    public delegate int GZipCompressionDelegate(string sourceFileName, string destFileName, CustomCancellationToken cancelToken );

    
    public class GZipCompressor : IGZipCompressor
    {
        public Exception InnerException { get; set; }
        public int ProcessResult { get; set; }

        public event EventHandler<CompressionCompleteEventArgs> OnCompressionComplete;
        public event EventHandler<CompressionProgressChangedEventArgs> OnCompressionProgressChanged;

        int ProcessorsCount;
        int jobResult;
        GZipThread[] threads;
        AutoResetEvent waitHandle;
        long BufferSize;
        object _readSync;
        object _writeSync;
        public GZipCompressor(AutoResetEvent waitHandle)
        {
            this.waitHandle = waitHandle;
            BufferSize = Properties.Settings.Default.BufferSize;
            ProcessorsCount =  Environment.ProcessorCount;
            threads = new GZipThread[ProcessorsCount];
            _readSync = new object();
            _writeSync = new object();
        }

        public int Compress(string sourceFileName, string destFileName)
        {
            return this.Compress(sourceFileName, destFileName, CustomCancellationTokenSource.GetToken());
        }

        public int Decompress(string sourceFileName, string destFileName)
        {
            return this.Decompress(sourceFileName, destFileName, CustomCancellationTokenSource.GetToken());
        }

        public int Compress(string sourceFileName, string destFileName, CustomCancellationToken cancelToken)
        {
            var sourceStream = File.OpenRead(sourceFileName);
            var destStream = new FileStream(destFileName, FileMode.OpenOrCreate, FileAccess.Write);
            var gzipStream = new GZipStream(destStream, CompressionMode.Compress);

            try
            {
                WriteToStreamMultiThread(sourceStream, gzipStream, cancelToken);
            }
            catch (OperationCanceledException cancelEx)
            {
                InnerException = cancelEx;
                jobResult = 1;
            }
            catch (Exception ex)
            {
                InnerException = ex;
                jobResult = 1;
            }
            finally
            {
                sourceStream.Close();
                gzipStream.Close();
                destStream.Close();
                waitHandle.Set();
                if (OnCompressionComplete != null) OnCompressionComplete(this, new CompressionCompleteEventArgs(jobResult, 0, InnerException));
            }
            return jobResult;
        }

        public int Decompress(string sourceFileName, string destFileName, CustomCancellationToken cancelToken)
        {
            var sourceStream = File.OpenRead(sourceFileName);
            var destStream = new FileStream(destFileName, FileMode.OpenOrCreate, FileAccess.Write);
            var gzipStream = new GZipStream(sourceStream, CompressionMode.Decompress);

            try
            {
                WriteToStreamMultiThread(gzipStream, destStream, cancelToken);
            }
            catch (OperationCanceledException cancelEx)
            {
                InnerException = cancelEx;
                jobResult = 1;
            }
            catch (Exception ex)
            {
                InnerException = ex;
                jobResult = 1;
            }
            finally
            {
                gzipStream.Close();
                sourceStream.Close();
                destStream.Close();
                waitHandle.Set();
                if (OnCompressionComplete != null) OnCompressionComplete(this, new CompressionCompleteEventArgs(jobResult, 0, InnerException));
            }
            return jobResult;
        }

        void WriteToStreamMultiThread(Stream from, Stream to, CustomCancellationToken cancelToken)
        {
            GZipThreadQueueProvider.ResetAll();
            WaitHandle[] handles = new WaitHandle[ProcessorsCount];
            for (int i=0; i<ProcessorsCount; i++)
            {
                GZipThread thr = new GZipThread(BufferSize, _readSync, _writeSync);
                thr.OnThreadWorkComplete += thr_OnThreadWorkComplete;
                thr.OnThreadIterationComplete += thr_OnThreadIterationComplete;
                threads[i] = thr;
                handles[i] = thr.waitHandle;
            }

            foreach (var thr in threads)
                thr.Start(from, to, cancelToken);

            foreach (var thr in threads)
                thr.waitHandle.WaitOne();
        }

        void thr_OnThreadWorkComplete(object sender, CompressionCompleteEventArgs e)
        {
            if (e.InnerException != null)
            {
                InnerException = e.InnerException;
                jobResult = 1;
            }//throw e.InnerException;
        }

        void thr_OnThreadIterationComplete(object sender, CompressionProgressChangedEventArgs e)
        {
            if (OnCompressionProgressChanged != null) OnCompressionProgressChanged(this, e);
        }
        

        void WriteToStream(Stream from, Stream to, CustomCancellationToken cancelToken, int readOffset = 0)
        {
            byte[] buffer = new byte[BufferSize];
            long readBytes = 0;
            long readBytesSum = 0;
            while ((readBytes = from.Read(buffer, readOffset, buffer.Length)) != 0)
            {
                if (!cancelToken.IsCancelled)
                {
                    to.Write(buffer, 0, (int)readBytes);
                    readBytesSum += readBytes; 
                    if (OnCompressionProgressChanged != null) OnCompressionProgressChanged(this, new CompressionProgressChangedEventArgs(from.Length, readBytesSum));
                }
                else
                    throw new OperationCanceledException("Operation was cancelled");
            }
        }
    }
}
