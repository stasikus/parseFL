using System;
using System.Collections.Generic;
using System.Text;

namespace Fenryr.Http.Cookies
{
    public class CookieParser
    {
        internal static readonly char[] Reserved2Name = new char[] { ' ', '\t', '\r', '\n', '=', ';', ',' };
        internal static readonly char[] Reserved2Value = new char[] { ';', ',' };
        internal static readonly char[] PortSplitDelimiters = new char[] { ' ', ',', '"' };

        static bool ValidateName(string name)
        {
            return (name.Length > 0 &&
                    name != "$" &&
                    name.IndexOfAny(Reserved2Name) == -1);
        }

        static bool ValidateValue(string value)
        {
            return (value.Length > 0 &&
                    value.IndexOfAny(Reserved2Value) == -1);
        }

        static bool ValidateDomain(string name)
        {
            if ((name == null) || (name.Length == 0))
            {
                return false;
            }
            for (int i = 0; i < name.Length; i++)
            {
                char ch = name[i];
                if ((((ch < '0') || (ch > '9')) && (((ch != '.') && (ch != '-')) && ((ch < 'a') || (ch > 'z')))) && (((ch < 'A') || (ch > 'Z')) && (ch != '_')))
                {
                    return false;
                }
            }
            return true;
        }


        public static Cookie CreateCookie(string CookieString)
        {

            Cookie result = new Cookie();

            if (CookieString.ToLower().StartsWith("set-cookie2:"))
                CookieString = CookieString.Remove(0, 11).Trim();
            else if (CookieString.ToLower().StartsWith("set-cookie:"))
                CookieString = CookieString.Remove(0, 11).Trim();

            string[] attributes = CookieString.Split(';');
            if (attributes.Length > 0)
            {
                string atrName = "";
                string atrValue = "";
                int pos = attributes[0].IndexOf('=');
                if (pos > -1)
                {
                    atrName = attributes[0].Substring(0, pos);
                    atrValue = attributes[0].Remove(0, pos + 1);
                }
                else
                {
                    atrName = attributes[0];
                    atrValue = "";
                }
                if (!ValidateName(atrName))
                    throw new CookieException("Cookie name not valid", CookieString);

                if (!ValidateValue(atrValue))
                    throw new CookieException("Cookie value not valid", CookieString);

                result.Name = atrName;
                result.Value = atrValue;
            }
            else throw new CookieException("Name or value not set", CookieString);

            for (int i = 1; i < attributes.Length; i++)
            {
                int pos = attributes[i].IndexOf('=');
                string atrName = "";
                string atrValue = "";
                string[] ports = null;
                int port = 0;

                if (pos > -1)
                {
                    atrName = attributes[i].Substring(0, pos).Trim();
                    atrValue = attributes[i].Remove(0, pos + 1).Trim();
                }
                else
                {
                    atrName = attributes[i].Trim();
                }
                string[] pair = attributes[i].Split('=');
                DateTime expires = DateTime.Now;
                int sec = 0;
                //return string.Concat(new object[] { this.Name, "=", this.Value, ";", this.Path, "; ", this.Domain, "; ", this.Version }).GetHashCode();

                switch (atrName.ToUpper())
                {
                    case "EXPIRES":
                        if (DateTime.TryParse(atrValue, out expires))
                        {
                            result.Expires = expires;
                        }
                        break;

                    case "MAX-AGE":
                        if (int.TryParse(atrValue, out sec))
                        {
                            result.Expires = DateTime.Now.AddSeconds(sec);
                        }
                        break;

                    case "PATH":
                        result.Path = atrValue.ToLower();
                        break;

                    case "SECURE":
                        result.Secure = true;
                        break;

                    case "HTTPONLY":
                        result.HttpOnly = true;
                        break;

                    case "DISCARD":
                        result.Discard = true;
                        break;

                    case "VERSION":
                        result.Version = atrValue;
                        break;

                    case "DOMAIN":
                        if (!ValidateDomain(atrValue))
                            throw new CookieException("Cookie domain not valid", CookieString);
                        result.Domain = atrValue.ToLower();
                        result.Domain = atrValue.ToLower();
                        if (result.Domain.StartsWith("."))
                            result.Domain = result.Domain.Remove(0, 1);
                        break;

                    case "PORT":
                        ports = atrValue.Split(PortSplitDelimiters, StringSplitOptions.RemoveEmptyEntries);
                        foreach (string sPort in ports)
                        {
                            if (int.TryParse(sPort.Trim(), out port))
                            {
                                result.Ports.Add(port);
                            }
                        }
                        break;

                    default:
                        break;
                }
            }
            return result;
        }
    }
}
