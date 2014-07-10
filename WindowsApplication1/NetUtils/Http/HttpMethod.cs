using System;
using System.Collections.Generic;
using System.Text;

namespace Fenryr.Http
{
    class HttpMethod
    {
        string m_Method;

        HttpMethod(string method)
        {
            m_Method = method;
        }
       
        public static HttpMethod GET {
            get { return new HttpMethod("GET"); }        
       }

        public static HttpMethod POST
        {
            get { return new HttpMethod("POST"); }
        }

        public override bool Equals(object obj)
        {
            if (!(obj is HttpMethod))
                return false;
            return ((obj as HttpMethod).m_Method == m_Method);
        }

        public override int GetHashCode()
        {
            return m_Method.GetHashCode();
        }

        public override string ToString()
        {
            return m_Method;
        }
    }
}
