using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using System.IO;
using Fenryr.IO;
using Fenryr.Net.Sockets.Socks;


namespace Fenryr.Net.Sockets
{

    public class SSLException : Exception
    {
        public SSLException(string Message)
            : base(Message)
        {

        }
    }

    class ConnectionFactory
    {
        public static TcpConnection CreateConnection(Uri uri , TcpProxy Proxy)
        {
            if (Proxy != null)
            {
                if (Proxy.ProxyType == ProxyTypes.Http)
                {
                    return new HttpProxyConnection(Proxy, uri.AbsoluteUri.StartsWith("https://"));
                }
                else
                {
                    return new SocksConnection(Proxy, uri.AbsoluteUri.StartsWith("https://"));
                }
            }
            return new TcpConnection(uri.AbsoluteUri.StartsWith("https://"));
        }

    }

   public class TcpConnection : IDisposable
   {
       
        public static IPAddress GetIpAddress(String Host)
        {
            IPAddress ipAddress;
            if (!IPAddress.TryParse(Host, out ipAddress))
            {
                try
                {
                    return Dns.GetHostEntry(Host).AddressList[0];
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException(
                        string.Format("Unable to resolve  hostname to a valid IP address. {0}", e));
                }
            }
            return ipAddress;
        }


        static bool ValidateServerCertificate(
          object sender,
          X509Certificate certificate,
          X509Chain chain,
          SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        public TcpConnection()
            : this(false)
        {
        }

        public TcpConnection(bool useSSL)
        {
            this.m_useSSL = useSSL;
            m_Socket.Blocked = false;
        }


        protected IPAddress m_Address = null;
       protected  string m_Host;
        protected int m_Port = 0;
        protected bool m_useSSL = false;

       public TcpSocket Socket
       {
           get
           {
               return m_Socket;
           }
       }


       public int ConnectTimeout 
        {
            get { return m_Socket.ConnectTimeout; }
            set { m_Socket.ConnectTimeout = value; }
       }

       public int WriteTimeout
        {
            get { return m_Socket.SendTimeout; }
            set { m_Socket.SendTimeout = value; }
        }

     public   int ReadTimeout
        {
            get { return m_Socket.ReceiveTimeout; }
            set { m_Socket.ReceiveTimeout = value; }
        }


      public  bool Alive
        {
            get
            {
                try
                {
                    bool bCanRead = m_Socket.CanRead(0);
                    if (bCanRead == false)
                        bCanRead = m_Socket.AvailableData > 0;
                    return !bCanRead && m_Socket.CanWrite(0);
                }
                catch (ObjectDisposedException)
                {
                    return false;
                }
            }
        }

       protected Stream m_Stream;
       protected TcpSocket m_Socket = new TcpSocket();

        public Stream ConnectStream
        {
            get
            {
                return m_Stream;
            }
        }


        public int Port
        {
            get
            {
                return m_Port;
            }
        }


        protected void ConnectSSL(string Host)
        {

            System.Net.Security.SslStream sslStream = new SslStream(m_Stream,
                                 false,
                                 new RemoteCertificateValidationCallback(ValidateServerCertificate),
                                 null);
            try
            {
                sslStream.AuthenticateAsClient(Host);
            }
            catch (Exception ex)
            {
                throw new SSLException("SSL Handshake failed: " + ex.Message);
            }

            m_Stream = sslStream;
        }

        
        public virtual bool IsMatch (string host , int port , TcpProxy proxy)
        {
         //   return true;
            return (m_Port == port && m_Address.ToString() == GetIpAddress(host).ToString());
        }

        public virtual void Connect (IPAddress addr, int port)
        {
            m_Address = addr;

            m_Socket.Connect(new IPEndPoint(m_Address , m_Port));
            m_Stream = new BufferedNetStream(m_Socket);

            if (m_useSSL)
            {
                ConnectSSL(m_Host);
            }
        }

        public virtual void Connect(string host, int port)
       {
           m_Host = host;
           m_Port = port;

           Connect(GetIpAddress(m_Host), port);
       }

       public void Close()
       {
           if (m_Stream != null)
           {
               m_Socket.Close();
               m_Stream.Close();             
           }
       }


       public void Dispose()
       {
           Close();
           GC.SuppressFinalize(this);
       }

   }

  public  class HttpProxyConnection : TcpConnection 
    {
      bool TunelConnect(IWebProxy proxy, string sHost, int Port)
      {

          if (new System.Text.RegularExpressions.Regex("^([a-f0-9]{4}:){7}[a-f0-9]{4}$").Match(sHost.ToLower()).Success)
          {
              sHost = "[" + sHost + "]";
          }

          StreamUtils.WriteLine(ConnectStream, String.Format("CONNECT {0}:{1} HTTP/1.0", sHost, Port));
          StreamUtils.WriteLine(ConnectStream, String.Format("Host: {0}", sHost));
          StreamUtils.WriteLine(ConnectStream, "Proxy-Connection: keep-alive");

          if (proxy.Credentials != null)
          {
              NetworkCredential nc = (NetworkCredential)proxy.Credentials;
              string data = Convert.ToBase64String
                  (Encoding.Default.GetBytes(nc.UserName + ":" + nc.Password));
              data = "Proxy-Authorization: Basic " + data;
              StreamUtils.WriteLine(ConnectStream, data);
          }
          StreamUtils.WriteLine(ConnectStream, string.Empty);
          string line = null;
          bool result = false;
          int nEmpty = 0;
          string statusLine = "";
         
          while ((line = StreamUtils.ReadLine(ConnectStream)) != null)
          {
              if ((String.IsNullOrEmpty(line) || line[0] == '\r'))
              {
                  if (nEmpty > 3 || result) break;
                  nEmpty++;
              }
              if (line.Contains("HTTP/"))
              {
                  statusLine = line;
                  result = true;
              }
          }

          if (result )
          {
              int code = int.Parse(statusLine.Substring(9,3));
              if (code < 200 || code >= 300 ) {
                  throw new WebException("CONNECT ERROR: " + statusLine);
              }
          }

          return result;
      }

      TcpProxy m_Proxy;
      public HttpProxyConnection(TcpProxy Proxy, bool useSSL)
          : base(useSSL)
      {
          m_Proxy = Proxy;
      }

      public override void Connect(string host, int port)
      {
          m_Host = m_Proxy.Host;
          m_Port = m_Proxy.Port;

          m_Address = GetIpAddress(m_Host);

          m_Socket.Connect(new IPEndPoint(m_Address, m_Port));
          m_Stream = new BufferedNetStream(m_Socket);

          if (m_useSSL)
          {
              if (!TunelConnect(m_Proxy, host, port))
                  throw new ProtocolViolationException("Proxy does support SSL");

              ConnectSSL(m_Proxy.Host);
          }
      }

      public override bool IsMatch(string host, int port, TcpProxy proxy)
      {
          return (m_Proxy.Host == proxy.Host && m_Proxy.Port == proxy.Port 
              && m_Host == host && m_Port == port);
      }
    }

    public class SocksConnection : TcpConnection
    {
        TcpProxy m_Proxy;

        public override bool IsMatch(string host, int port, TcpProxy proxy)
        {
            return (m_Proxy.Host == proxy.Host && m_Proxy.Port == proxy.Port
                && m_Host == host && m_Port == port);
        }


        public SocksConnection (TcpProxy Proxy, bool useSSL)
          : base(useSSL)
      {
          m_Proxy = Proxy;
          m_Socket = new ProxySocket();
      }

        public override void Connect(IPAddress addr, int port)
        {
            m_Address = addr;

            ((ProxySocket)m_Socket).ProxyType = m_Proxy.ProxyType;
            ((ProxySocket)m_Socket).ProxyEndPoint = new IPEndPoint(GetIpAddress(m_Proxy.Host), m_Proxy.Port);

            if (m_Proxy.Credentials != null)
            {
                ((ProxySocket)m_Socket).ProxyUser = m_Proxy.User;
                ((ProxySocket)m_Socket).ProxyPass = m_Proxy.Pass;
            }

            m_Socket.Blocked = false;
            ((ProxySocket)m_Socket).Connect(new IPEndPoint(m_Address, m_Port));
            m_Stream = new BufferedNetStream(m_Socket);

            if (m_useSSL)
            {
                ConnectSSL(m_Host);
            }
        }

    }
}
