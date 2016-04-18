using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VeeamGZip
{
    public class CustomCancellationToken
    {
        public volatile bool IsCancelled;

        public CustomCancellationToken()
        {
            IsCancelled = false;
        }
    }

    public static class CustomCancellationTokenSource
    {
        private static CustomCancellationToken token;

        public static CustomCancellationToken GetToken()
        {
            if (token == null)
                token =  new CustomCancellationToken();
            return token;
        }
    }

}
