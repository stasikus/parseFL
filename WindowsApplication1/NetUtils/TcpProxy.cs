using System;
using System.Collections.Generic;
using System.Text;
using System.Net;

namespace Fenryr
{
    public enum ProxyTypes
    {
        None,
        Http,
        Socks4,
        Socks5
    }

    public class TcpProxy : IWebProxy
    {
        public Object Tag = null;     
        NetworkCredential _credentials;

        public ICredentials Credentials
        {
            get { return _credentials; }
            set { _credentials = (NetworkCredential)value; }
        }

        string host;
        int port;
      

        ProxyTypes proxyType = ProxyTypes.Http;

        public ProxyTypes ProxyType { get { return proxyType; } set { proxyType = value; } }
        public string Host { get { return host; } }
        public int Port { get { return port; } }
        public string User { get { return (_credentials != null) ? _credentials.UserName : null; } }
        public string Pass { get { return (_credentials != null) ? _credentials.Password : null; } }

        public bool AuthRequired
        {
            get { return (!String.IsNullOrEmpty(User) && !String.IsNullOrEmpty(Pass)); }
        }

        public Uri GetProxy(Uri destination)
        {
            return new Uri("http://" + Host + ":" + Port);
        }

        public bool IsBypassed(Uri host)
        {
            return false;
        }

        public TcpProxy(string host, int port, ProxyTypes proxyType)
        {
            this.host = host;
            this.port = port;
            ProxyType = proxyType;
        }

        public TcpProxy(string proxystr, ProxyTypes proxyType)
        {
            this.host = proxystr.Split(':')[0].Trim();
            this.port = int.Parse(proxystr.Split(':')[1].Trim());
            ProxyType = proxyType;
        }

        public TcpProxy(string host, int port, string user, string pass , ProxyTypes proxyType) :
           this (host, port , proxyType)
        {
            this.Credentials = new NetworkCredential(user, pass);
        }

        private TcpProxy() { }

        public static TcpProxy Parse(string sProxy)
        {
            string[] data = sProxy.Split(':');
            TcpProxy proxy = new TcpProxy();
            proxy.host = data[0].Trim();
            proxy.port = int.Parse(data[1].Trim());
            if (data.Length > 3)
            {
                proxy.Credentials = new NetworkCredential(data[2].Trim() , data[3].Trim());
            }
            if (data.Length == 5)
            {
                if (data[4].Contains("4")) data[2] = "Socks4";
                else if (data[4].Contains("5")) data[2] = "Socks5";
                else data[4] = "Http";
                proxy.proxyType = (ProxyTypes)Enum.Parse(typeof(ProxyTypes) , data[4].Trim());
            }
            else if (data.Length == 3)
            {
                if (data[2].Contains("4")) data[2] = "Socks4";
                else if (data[2].Contains("5")) data[2] = "Socks5";
                else data[2] = "Http";
                proxy.proxyType = (ProxyTypes)Enum.Parse(typeof(ProxyTypes), data[2].Trim());
            }
            return proxy;
        }
        public override string ToString()
        {
            return (Credentials == null)?
                String.Format("{0}:{1}", Host, Port) :
                String.Format("{0}:{1}:{2}:{3}", Host, Port, User, Pass)  ;
        }

      

     

    }
       

}
