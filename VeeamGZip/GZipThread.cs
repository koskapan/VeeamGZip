using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;

namespace VeeamGZip
{
    public class GZipThread
    {
        Thread InnerThread;
        object _readSync;
        object _writeSync;
        public int ProcessResult;
        public Exception InnerException;
        public long ID;
        public byte[] InnerBuffer;
        public AutoResetEvent waitHandle;

        public event EventHandler<CompressionCompleteEventArgs> OnThreadWorkComplete;
        public event EventHandler<CompressionProgressChangedEventArgs> OnThreadIterationComplete;

        public GZipThread(long bufferSize, object _ReadSync, object _WriteSync)
        {
            this._readSync = _ReadSync;
            this._writeSync = _WriteSync;
            InnerBuffer = new byte[bufferSize];
            InnerThread = new Thread(Run);
            InnerThread.Name = ID.ToString();
            waitHandle = new AutoResetEvent(false);
        }

        public void Start(Stream fromS, Stream toS, CustomCancellationToken cancelToken)
        {
            InnerThread.Start(new GZipThreadArgs() { SourceStream = fromS, DestStream = toS, cancelToken = cancelToken });
        }

        void Run(object args)
        {
            GZipThreadArgs gzipArgs = args as GZipThreadArgs;
            if (gzipArgs == null) throw new ArgumentNullException();
            int readBytes = 0;
            try
            {
                while ((readBytes = ReadFromStream(gzipArgs.SourceStream)) != 0)
                {
                    do
                    {
                        if (gzipArgs.cancelToken.IsCancelled) throw new OperationCanceledException();
                        if (this.ID == Interlocked.Read(ref GZipThreadQueueProvider.CurrentWriteBlockNumber))
                        {
                            lock (_writeSync)
                            {
                                gzipArgs.DestStream.Write(InnerBuffer, 0, readBytes);
                                Interlocked.Increment(ref GZipThreadQueueProvider.CurrentWriteBlockNumber);
                            }
                            if (OnThreadIterationComplete != null) OnThreadIterationComplete(this, new CompressionProgressChangedEventArgs(0, readBytes));
                            break;
                        }
                    } while (true);
                }
                ProcessResult = 0;
            }
            catch (Exception ex)
            {
                InnerException = ex;
                ProcessResult = 1;
            }
            finally
            {
                if (OnThreadWorkComplete != null) OnThreadWorkComplete(this, new CompressionCompleteEventArgs(ProcessResult, 0, InnerException));
                waitHandle.Set();
            }
        }


        int ReadFromStream(Stream str)
        {
            int readBytes = 0;
            lock (_readSync)
            {
                readBytes = str.Read(InnerBuffer, 0, (int)InnerBuffer.Length);
                this.ID = Interlocked.Read(ref GZipThreadQueueProvider.ThreadQueueNumber);
                Interlocked.Increment(ref GZipThreadQueueProvider.ThreadQueueNumber);
            }
            return readBytes;
        }
    }
}
