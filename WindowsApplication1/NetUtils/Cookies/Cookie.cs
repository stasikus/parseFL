using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;

namespace Fenryr.Http.Cookies
{
    public class CookieException : Exception
    {
        string m_Header;

        public string Header
        {
            get
            {
                return m_Header;
            }
        }

        public CookieException(string Message , string Header) :
            base(Message)
        {
            m_Header = Header;
        }
    }



    public class Cookie
    {
        string m_Path = "/";
        string m_Domain = ".";
        bool m_Secure = false;
        bool m_HttpOnly = false;
        bool m_Discarded = false;

        DateTime m_Expires = DateTime.Now.AddYears(100);
        string m_Name = "";
        string m_Value = "";
        string m_Version = "";
       

        List<int> m_Ports = new List<int>();


        #region Properties
        public string Name
        {
            get { return m_Name; }
            set { m_Name = value; }
        }

        public string Value
        {
            get { return m_Value; }
            set { m_Value = value; }
        }

        public string Path
        {
            get { return m_Path; }
            set { m_Path = value; }
        }

        public string Domain
        {
            get { return m_Domain; }
            set { m_Domain = value; }
        }

        public bool Secure
        {
            get { return m_Secure; }
            set { m_Secure = value; }
        }

        public bool Discard
        {
            get { return m_Discarded; }
            set { m_Discarded = value; }
        }

        public bool HttpOnly
        {
            get { return m_HttpOnly; }
            set { m_HttpOnly = value; }
        }

        public string Version
        {
            get { return m_Version; }
            set { m_Version = value; }
        }

        public DateTime Expires
        {
            get { return m_Expires; }
            set { m_Expires = value; }
        }

        public List<int> Ports
        {
            get
            {
                return m_Ports;
            }
        }
        #endregion


        public override string ToString()
        {
            return string.Concat(new object[] { this.Name, ";", this.Path, "; ", this.Domain, "; ", this.Version });
        }

        public Cookie()
        {
           int x =  string.Compare("aBBa", "ABba", StringComparison.OrdinalIgnoreCase);
        }

       

    }
}
