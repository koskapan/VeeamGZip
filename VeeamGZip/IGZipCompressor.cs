using System;

namespace VeeamGZip
{
    public interface IGZipCompressor
    {
        Exception InnerException { get; set; }
        int ProcessResult { get; set; }

        event EventHandler<CompressionCompleteEventArgs> OnCompressionComplete;
        event EventHandler<CompressionProgressChangedEventArgs> OnCompressionProgressChanged;

        int Compress(string sourceFileName, string destFileName);
        int Compress(string sourceFileName, string destFileName, CustomCancellationToken cancelToken);
        int Decompress(string sourceFileName, string destFileName);
        int Decompress(string sourceFileName, string destFileName, CustomCancellationToken cancelToken);
    }
}