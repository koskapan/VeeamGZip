using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace VeeamGZip
{
    public class CompressionProgressChangedEventArgs: EventArgs
    {
        public readonly long ReadBytes;
        public readonly long TotalFileSize;
        public CompressionProgressChangedEventArgs(long totalFileSize, long readBytes)
        {
            ReadBytes = readBytes;
            TotalFileSize = totalFileSize;
        }
    }


    public class CompressionCompleteEventArgs: EventArgs
    {
        public readonly int ProcessResult;
        public readonly long EndFileSize;
        public readonly Exception InnerException;
        public CompressionCompleteEventArgs(int processResult, long endFileSize, Exception innerException)
        {
            ProcessResult = processResult;
            EndFileSize = endFileSize;
            InnerException = innerException;
        }
    }


    public class GZipThreadReadCompleteEventArgs:EventArgs
    {
        public readonly int ReadBytes;
        public readonly long Position;
        public readonly Stream SourceStream;
        public readonly Stream DestStream;

        public GZipThreadReadCompleteEventArgs(int readBytes, long position, Stream sourceStream, Stream destStream)
        {
            this.ReadBytes = readBytes;
            this.Position = position;
            this.SourceStream = sourceStream;
            this.DestStream = destStream;
        }
    }


    public class GZipThreadArgs
    {
        public Stream SourceStream;
        public Stream DestStream;
        public CustomCancellationToken cancelToken;
    }

}
