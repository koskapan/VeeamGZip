using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace VeeamGZip
{
    public delegate int GZipCompressionDelegate(string sourceFile, string destFile, CustomCancellationToken cancelToken);

    public class GzipAsyncResultBase: IDisposable, IAsyncResult
    {
        AsyncCallback callback;
        object state;
        ManualResetEvent waitHandle;


        public GzipAsyncResultBase(AsyncCallback callback, object state)
        {
            this.callback = callback;
            this.state = state;
            this.waitHandle = new ManualResetEvent(false);
        }

        public void Complete()
        {
            try
            {
                waitHandle.Set();
                if(null != callback)
                    callback(this);
            }
            catch
            {
                throw;
            }
        }

        public void Dispose()
        {
            if(waitHandle != null)
            {
                waitHandle.Close();
                waitHandle = null;
                state = null;
                callback = null;
            }
        }

        public void Validate()
        {
            if(waitHandle == null)
                throw new InvalidOperationException();
        }

        public object AsyncState
        {
            get
            {
                return state;
            }
        }

        public ManualResetEvent AsyncWaitHandle
        {
            get
            {
                return waitHandle;
            }
        }

        WaitHandle IAsyncResult.AsyncWaitHandle
        {
            get
            {
                return this.AsyncWaitHandle;
            }
        }

        public bool CompletedSynchronously
        {
            get
            {
                return false;
            }
        }

        public bool IsCompleted
        {
            get
            {
                return waitHandle.WaitOne(0, false);
            }
        }
    }

    public class GzipAsyncResult : GzipAsyncResultBase
    {
        internal GzipAsyncResult(string sourceFileName, string destFileName, CustomCancellationToken cancelToken, AsyncCallback callback, object state):
        base(callback, state)
        {
            this.SourceFileName = sourceFileName;
            this.DestFileName = destFileName;
            this.CancelToken = cancelToken;
        }

    internal int Result;

    internal readonly string SourceFileName;

    internal readonly string DestFileName;

    internal readonly CustomCancellationToken CancelToken;
    }
}
