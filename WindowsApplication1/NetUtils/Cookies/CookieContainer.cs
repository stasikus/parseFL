using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;

namespace Fenryr.Http.Cookies
{
    public class CookieContainer : ICloneable
    {
        Hashtable m_Cookies = new Hashtable();

        public void Clear()
        {
            m_Cookies.Clear();
        }

        public object Clone()
        {
            CookieContainer copy = new CookieContainer();
            foreach (object obj in m_Cookies.Keys)
            {
                Cookie c = (Cookie)m_Cookies[obj];
                copy.m_Cookies.Add(c.ToString(), c);
            }
            return copy;
        }


        void RemoveExpired()
        {
            lock (m_Cookies)
            {
                List<string> keys = new List<string>();
                foreach (object obj in m_Cookies.Keys)
                {
                    Cookie c = (Cookie)m_Cookies[obj];
                    if (c.Discard || c.Expires <= DateTime.Now)
                        keys.Add((string)obj);
                }

                foreach (string str in keys)
                {
                    m_Cookies.Remove(str);
                }
            }
        }

        public void AddCustomCookie(string Url , string cookieString)
        {
            if (cookieString.StartsWith("Cookie: "))
                cookieString = cookieString.Remove(0 ,8);
            string[] cookies = cookieString.Split(';');
            
            foreach (string cookie in cookies)
            {
                int pos = cookie.IndexOf('=');
                string Name = "";
                string Value = "";

                if (pos > -1)
                {
                    Name = cookie.Substring(0, pos).Trim();
                    Value = cookie.Remove(0, pos + 1).Trim();
                    AddCookie(Url , Name, Value);
                }
            }
        }


        public void AddCookie(string Url, string name, string value)
        {
            Cookie c = new Cookie();
            Uri uri = new Uri(Url);
            c.Name = name;
            c.Value = value;
            c.Domain = uri.Host;
            lock (m_Cookies)
            {
                m_Cookies[c.ToString()] = c;
            }
        }


        public void SetCookie(string Url, string Header)
        {
            try
            {
                Cookie c = CookieParser.CreateCookie(Header);
                if (c.Domain == ".")
                {
                    Uri uri = new Uri(Url);
                    c.Domain = uri.Host;
                }
                lock (m_Cookies)
                {
                    m_Cookies[c.ToString()] = c;
                }
            }
            catch
            {

            }
        }

        public string GetCookieHeader(string Url)
        {
            Cookie[] cookies = GetCookies(Url);
         
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < cookies.Length; i++)
            {
                sb.AppendFormat("{0}={1}", cookies[i].Name, cookies[i].Value);
                if (i < cookies.Length - 1)
                    sb.Append("; ");
            }

            return sb.ToString();
        }



        public Cookie[] GetCookies(string Url)
        {
            List<Cookie> cookies = new List<Cookie>();
            RemoveExpired();
            Uri uri = new Uri(Url);
            string path = uri.AbsolutePath.ToLower();
            if (!path.StartsWith("/"))
                path = "/" + path;
            string domain = uri.Host.ToLower();

            foreach (object obj in m_Cookies.Keys)
            {
                Cookie c = (Cookie)m_Cookies[obj];
                if (c.Ports.Count > 0 && c.Ports.Contains(uri.Port) == false)
                    continue;
                if (c.Secure && uri.Scheme.ToLower() != "https")
                    continue;
                if (!path.StartsWith(c.Path))
                    continue;

                if (domain.EndsWith(c.Domain))
                {
                    cookies.Add(c);
                }
            }

            return cookies.ToArray();
        }

    }

}
