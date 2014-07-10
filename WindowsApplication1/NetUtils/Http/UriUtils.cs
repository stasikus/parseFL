using System;
using System.Collections.Generic;
using System.Text;

namespace Fenryr.Http
{
    public static class UriUtils
    {
        public static string BuildUrl(string Url, string path)
        {
            return BuildUrl(Url, path, "http");
        }

        public static string NormalizeUrl(string Url, string prefix)
        {

            int index = Url.IndexOf('?');
            if (index > -1)
            {
                Url = Url.Substring(0, index);
            }
            if (Url.EndsWith("/")) Url.Remove(Url.Length - 1);
            if (!Uri.IsWellFormedUriString(Url, UriKind.Absolute))
                return prefix + "://" + Url;
            else return Url;
        }

        public static string BuildUrl(string Url, string path, string prefix)
        {
            string prevUrl = Url;
            Url = NormalizeUrl(Url, prefix).Trim();
            if (Uri.IsWellFormedUriString(path, UriKind.Absolute) || path.Contains(Url)) return path;
            if (path.StartsWith("?")) return Url + path;

            if (path.StartsWith("/"))
            {
                path = path.Remove(0, 1);
                prefix = (Url.StartsWith("https")) ? "https" : "http";
                return prefix + "://" + new Uri(Url).Authority + "/" + path;
            }

            else if (path.StartsWith("./"))
            {
                while (path.StartsWith("./"))
                {
                    path = path.Remove(0, 2);
                    int ind = Url.LastIndexOf("/");
                    if (ind > 6) Url = Url.Remove(ind);
                }
                return Url + "/" + path;
            }

            if (Url.LastIndexOf('.') >= Url.Length - 5)
            {
            int index = Url.LastIndexOf('/');
            if (index > 6)
            {
                Url = Url.Substring(0, index);
            }
            }
             
            return Url + "/" + path;
        }
    }
}
