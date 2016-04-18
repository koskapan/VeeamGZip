using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.IO.Compression;

namespace VeeamGZip
{

    public class CustomCompressor
    {
        public Exception InnerException;
        public event EventHandler<CompressionProgressChangedEventArgs> OnProgressChanged;
        delegate int AsyncCompressionCaller(string sourceFile, string destFile, CustomCancellationToken cancelToken);

        int threadCount;
        object _sync;

        public CustomCompressor()
        {
            threadCount = 0;
            _sync = new object();
        }
        
        public int Compress(string sourceFile, string destFile)
        {
            return this.Compress(sourceFile, destFile, CustomCancellationTokenSource.GetToken());
        }

        public int Decompress(string sourceFile, string destFile)
        {
            return this.Decompress(sourceFile, destFile, CustomCancellationTokenSource.GetToken());
        }

        public int Compress(string sourceFile, string destFile, CustomCancellationToken cancelToken)
        {
            int operationResult = 0;
            using (var sourceFileStream = File.OpenRead(sourceFile))
            {
                using (var destFileStream = new FileStream(destFile, FileMode.Create, FileAccess.Write))
                {
                    using (var gzipStream = new GZipStream(destFileStream, CompressionMode.Compress, false))
                    {
                        try
                        {
                            WriteToStream(sourceFileStream, gzipStream, cancelToken);
                            operationResult = 0;
                        }
                        catch (OperationCanceledException cancelledEx)
                        {
                            InnerException = cancelledEx;
                            operationResult = 1;
                        }
                        catch (Exception ex)
                        {
                            InnerException = ex;
                            operationResult = 1;
                        }
                    }
                        
                }
            }
            if (operationResult != 0)
            {
                if (File.Exists(destFile))
                    File.Delete(destFile);
            }
            return operationResult;
        }

        public int Decompress(string sourceFile, string destFile, CustomCancellationToken cancelToken)
        {
            int operationResult = 0;
            using (var sourceFileStream = File.OpenRead(sourceFile))
            {
                using (var destFileStream = new FileStream(destFile, FileMode.Create, FileAccess.Write))
                {
                    using (var gzipStream = new GZipStream(sourceFileStream, CompressionMode.Decompress, false))
                    {
                        try
                        {
                            WriteToStream(gzipStream, destFileStream, cancelToken);
                            operationResult = 0;
                        }
                        catch (OperationCanceledException cancelledEx)
                        {
                            InnerException = cancelledEx;
                            operationResult = 1;
                        }
                        catch (Exception ex)
                        {
                            InnerException = ex;
                            operationResult = 1;
                        }
                    }
                }
            }
            if (operationResult != 0)
            {
                if (File.Exists(destFile))
                    File.Delete(destFile);
            }
            return operationResult;
        }

        /*
        void RunCompression(object state)
        {
            GzipAsyncResult asyncResult = state as GzipAsyncResult;
            if (asyncResult == null)
                throw new InvalidDataException("state");
            try
            {
                asyncResult.Result = Compress(asyncResult.SourceFileName, asyncResult.DestFileName, asyncResult.CancelToken);
            }
            finally
            {
                asyncResult.Complete();
            }
        }


        public IAsyncResult BeginCompress(string sourceFile, string destFile, CustomCancellationToken cancelToken, AsyncCallback callback, object asyncState)
        {
            IAsyncResult result = new GzipAsyncResult(sourceFile, destFile, cancelToken, callback, asyncState);
            var waitCallback = new WaitCallback(RunCompression);
            ThreadPool.QueueUserWorkItem(waitCallback, result);
            return result;
        }

        public int EndCompress(IAsyncResult result)
        {
            if (null == result)
                throw new ArgumentNullException("result");
            GzipAsyncResult asyncResult = result as GzipAsyncResult;
            if (null == asyncResult)
                throw new ArgumentException("", "result");
            asyncResult.Validate();
            asyncResult.AsyncWaitHandle.WaitOne();
            int res = asyncResult.Result;
            asyncResult.Dispose();
            return res;
        }


        void RunDecompression(object state)
        {
            GzipAsyncResult asyncResult = state as GzipAsyncResult;
            if (asyncResult == null)
                throw new InvalidDataException("state");
            try
            {
                asyncResult.Result = Decompress(asyncResult.SourceFileName, asyncResult.DestFileName, asyncResult.CancelToken);
            }
            finally
            {
                asyncResult.Complete();
            }
        }

        public IAsyncResult BeginDecompress(string sourceFile, string destFile, CustomCancellationToken cancelToken, AsyncCallback callback, object asyncState)
        {
            IAsyncResult result = new GzipAsyncResult(sourceFile, destFile, cancelToken, callback, asyncState);
            var waitCallback = new WaitCallback(RunDecompression);
            ThreadPool.QueueUserWorkItem(waitCallback, result);
            return result;
        }

        public int EndDecompress(IAsyncResult result)
        {
            if (null == result)
                throw new ArgumentNullException("result");
            GzipAsyncResult asyncResult = result as GzipAsyncResult;
            if (null == asyncResult)
                throw new ArgumentException("", "result");
            asyncResult.Validate();
            asyncResult.AsyncWaitHandle.WaitOne();
            int res = asyncResult.Result;
            asyncResult.Dispose();
            return res;
        }
        */
        void WriteToStream(Stream from, Stream to, CustomCancellationToken cancelToken)
        {
            byte[] buffer = new byte[Properties.Settings.Default.BufferSize];
            int readBytesCount = 0;
            while ((readBytesCount = from.Read(buffer, 0, buffer.Length)) != 0)
            {
                if (!cancelToken.IsCancelled)
                {
                    to.Write(buffer, 0, readBytesCount);
                    if (OnProgressChanged != null) OnProgressChanged(this, new CompressionProgressChangedEventArgs(from.Length, readBytesCount));
                }
                else
                    throw new OperationCanceledException("Operation was cancelled");
            }
        }
    }
}
