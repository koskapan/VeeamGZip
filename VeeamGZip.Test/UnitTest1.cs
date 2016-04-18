using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Threading;
using System.Security.Cryptography;
using System.Text;


namespace VeeamGZip.Test
{
    [TestClass]
    public class UnitTest1
    {
        GZipCompressor compressor;
        string startFileName = @"pic1.jpg";
        string endFileName = @"new.gz";
        delegate int AsyncCompressionCaller(string sourceFile, string destFile, CustomCancellationToken cancelToken);

        [TestMethod]
        public void CompressionTest()
        {
            AutoResetEvent waitHandle = new AutoResetEvent(false);
            compressor = new GZipCompressor(waitHandle); 
            //startFileName = @"video.avi";
            string decompressedFileName = @"Decompressed_" + startFileName;
            compressor.OnCompressionComplete += compressor_OnCompressionComplete;
            compressor.Compress(startFileName, endFileName);
            waitHandle.WaitOne();
            string startFileHash = "";
            string decompressedFileHash = "";

            MD5 md5hash = MD5.Create();

            compressor.Decompress(endFileName, decompressedFileName);
            waitHandle.WaitOne();

            using (Stream startFileStream = File.OpenRead(startFileName))
            using (Stream decompressedFIleStream = File.OpenRead(decompressedFileName))
            {
                startFileHash = Encoding.Default.GetString(md5hash.ComputeHash(startFileStream));
                decompressedFileHash = Encoding.Default.GetString(md5hash.ComputeHash(decompressedFIleStream));
            }
            Assert.AreEqual(startFileHash, decompressedFileHash);
            
        }

        void compressor_OnCompressionComplete(object sender, CompressionCompleteEventArgs e)
        {
            Assert.AreEqual(e.ProcessResult, 0);
        }

        [TestMethod]
        public void CompressionAsyncTest()
        {
            AutoResetEvent waitHandle = new AutoResetEvent(false);
            CustomCancellationToken token = CustomCancellationTokenSource.GetToken();
            compressor = new GZipCompressor(waitHandle);
            string decompressedFileName = @"Decompressed_" + startFileName;

            GZipCompressionDelegate del = compressor.Compress;
            del.BeginInvoke(startFileName, endFileName, token, OnCompressionComplete, del);
            waitHandle.WaitOne();
            string startFileHash = "";
            string decompressedFileHash = "";

            MD5 md5hash = MD5.Create();

            del = compressor.Decompress;
            del.BeginInvoke(endFileName, decompressedFileName, token, OnCompressionComplete, del);
            waitHandle.WaitOne();

            using (Stream startFileStream = File.OpenRead(startFileName))
            using (Stream decompressedFIleStream = File.OpenRead(decompressedFileName))
            {
                startFileHash = Encoding.Default.GetString(md5hash.ComputeHash(startFileStream));
                decompressedFileHash = Encoding.Default.GetString(md5hash.ComputeHash(decompressedFIleStream));
            }
            Assert.AreEqual(startFileHash, decompressedFileHash);
        }

        void OnCompressionComplete(IAsyncResult result)
        {
            Assert.AreNotEqual(result, null);
            GZipCompressionDelegate del = result.AsyncState as GZipCompressionDelegate;
            Assert.AreNotEqual(del, null);
            int jobResult = del.EndInvoke(result);
            Assert.AreEqual(jobResult, 0);
        }


        [TestMethod]
        public void CompressionWithCancellationTest()
        {
            AutoResetEvent waitHandle = new AutoResetEvent(false);
            CustomCancellationToken token = CustomCancellationTokenSource.GetToken();
            compressor = new GZipCompressor(waitHandle);
            //startFileName = @"video.avi";
            GZipCompressionDelegate del = compressor.Decompress;
            compressor.OnCompressionComplete += compressor_OnCompressionCompleteWithCancellation;
            del.BeginInvoke(startFileName, endFileName, token, null, del);
            Thread.Sleep(6000);
            token.IsCancelled = true;
            waitHandle.WaitOne();
        }

        void compressor_OnCompressionCompleteWithCancellation(object sender, CompressionCompleteEventArgs e)
        {
            Assert.AreEqual(e.ProcessResult, 1);
            Assert.AreEqual(e.InnerException.GetType(), typeof(OperationCanceledException));
        }
    }
}
