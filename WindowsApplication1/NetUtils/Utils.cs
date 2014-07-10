using System;
using System.Collections.Generic;
using System.Text;

namespace Fenryr
{
    class Utils
    {
        public static long GetTimeStamp()
        {
            long ts = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds;
            return ts;
        }

        public static string MD5(string val)
        {
            System.Security.Cryptography.MD5CryptoServiceProvider provider = new System.Security.Cryptography.MD5CryptoServiceProvider();
            byte[] data = provider.ComputeHash(Encoding.UTF8.GetBytes(val));
            StringBuilder sBuilder = new StringBuilder();
            foreach (byte b in data)
            {
                sBuilder.Append(b.ToString("x2"));
            }
            return sBuilder.ToString();
        }
    }
}
