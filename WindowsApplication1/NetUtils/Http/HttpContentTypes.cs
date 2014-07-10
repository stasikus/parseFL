using System;
using System.Collections.Generic;
using System.Text;

namespace Fenryr.Http
{
    public class HttpContentTypes
    {
        public static string FormUrlencoded
        {
            get { return "application/x-www-form-urlencoded"; }
        }

        public static string FormUrlencodedUTF8
        {
            get { return "application/x-www-form-urlencoded; charset=UTF-8"; }
        }

        public static string AtomXmlUTF8
        {
            get { return "application/atom+xml; charset=UTF-8"; }
        }

        public static string AtomXml
        {
            get { return "application/atom+xml"; }
        }

        public static string MultiPartFormData(string Boundary)
        {
            return "multipart/form-data; boundary=" + Boundary;
        }
    }
}
