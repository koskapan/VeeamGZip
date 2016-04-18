using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VeeamGZip
{
    public static class GZipThreadQueueProvider
    {
        public static long ThreadQueueNumber;
        public static long CurrentWriteBlockNumber;



        public static void ResetAll()
        {
            ThreadQueueNumber = 0;
            CurrentWriteBlockNumber = 0;
        }
    }
}
