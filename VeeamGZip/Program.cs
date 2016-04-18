using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.Runtime.InteropServices;

namespace VeeamGZip
{
    class Program
    {
        static int jobResult;
        static AutoResetEvent waitHandle;
        static CustomCancellationToken token;
        static long totalReadBytes;
        static string SourceFileName;
        static string EndFileName;

        static int Main(string[] args)
        {
            waitHandle = new AutoResetEvent(false);
            GZipCompressor compressor = new GZipCompressor(waitHandle);
            token = CustomCancellationTokenSource.GetToken();
            compressor.OnCompressionComplete += compressor_OnCompressionComplete;
            compressor.OnCompressionProgressChanged += compressor_OnProgressChanged;
            Console.CancelKeyPress += Console_CancelKeyPress;
            jobResult = 0;
            try
            {

                SourceFileName = args[1];
                EndFileName = args[2];
                switch (args[0].ToLower())
                {
                    case "compress":
                        compressor.Compress(SourceFileName, EndFileName);
                        break;
                    case "decompress":
                        compressor.Decompress(SourceFileName, EndFileName);
                        break;
                    default:
                        throw new NotSupportedException();
                }
            }
            catch (NotSupportedException)
            {
                Console.WriteLine("Unknown method!");
                ShowHelp();
                waitHandle.Set();
                jobResult = 1;
            }
            catch (IndexOutOfRangeException)
            {
                Console.WriteLine("Not enought parameters!");
                ShowHelp();
                waitHandle.Set();
                jobResult = 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                ShowHelp();
                waitHandle.Set();
                jobResult = 1;
            }
            waitHandle.WaitOne();
            return jobResult;
        }

        public static void ShowHelp()
        {
            Console.WriteLine(@"
Usage:

    VeeamGZip.exe <Method> <Source File> <End File> 

where: 
    Method - compress or decompress;
    Source File - path to source file
    End File - path to end file"
                
                );

        }

        static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
            token.IsCancelled = true;
        }

        static void compressor_OnProgressChanged(object sender, CompressionProgressChangedEventArgs e)
        {
            totalReadBytes += e.ReadBytes;
            Console.WriteLine("{0} Kb", totalReadBytes / 1024);
        }

        static void compressor_OnCompressionComplete(object sender, CompressionCompleteEventArgs e)
        {
            jobResult = e.ProcessResult;
            if (jobResult == 1 && File.Exists(EndFileName))
            {
                Console.WriteLine(e.InnerException.Message);
                if (File.Exists(EndFileName)) File.Delete(EndFileName);
            }
            else
            {
                Console.WriteLine("Done!");
            }
        }
       
    }
}
