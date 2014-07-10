using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using Fenryr.IO;
using System.IO;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using Fenryr.Net.Sockets;

namespace Fenryr.Http
{
    public class SocksHttpWebRequest : WebRequest
    {
        #region Member Variables

        TcpConnection connection ;
        private readonly Uri _requestUri;
        private WebHeaderCollection _requestHeaders;
        private string _method;
        private SocksHttpWebResponse _response;
        Encoding textEncoding = Encoding.UTF8;

        string _version = "1.1";
        bool keepAlive = true;
        int keepAliveTimeout = 115;

        TcpProxy _proxy;
        ICredentials _credencials;
        int _timeout;
        bool _requestSubmitted = false;
        string _requestMessage;
        long _contentLength;
        string _contentType;
        string _userAgent;
        string _accept;
        string _connection = "keep-alive";
        bool _isAlive = false;
        bool _isExternalConnection;
        int connectTimeOut = 0;

        static readonly string[] validHttpVerbs =
            new string[] { "GET", "HEAD", "POST", "PUT", "DELETE", "TRACE", "OPTIONS" };

        #endregion





        #region Properties

      //  public bool IsAlive
      //  {
      //      get { return connection.Alive; }
      //  }

        public string Connection
        {
            get { return _connection; }
            set { _connection = value; }
        }

        public int KeepAliveTimeout
        {
            get { return keepAliveTimeout; }
            set { keepAliveTimeout = value; }
        }

        public bool KeepAlive
        {
            get { return keepAlive; }
            set { keepAlive = value; }
        }

        public Encoding TextEncoding
        {
            get { return textEncoding; }
            set { textEncoding = value; }
        }


        public override long ContentLength
        {
            get { return _contentLength; }
            set { _contentLength = value; }
        }

        public int ConnectTimeout {
            get { return connectTimeOut; }
            set { connectTimeOut = value; }
        }

        public string Accept
        {
            get { return _accept; }
            set { _accept = value; }
        }

        public string UserAgent
        {
            get { return _userAgent; }
            set { _userAgent = value; }
        }

        public override string ContentType
        {
            get { return _contentType; }
            set { _contentType = value; }
        }


        public string RequestMessage
        {
            get
            {
                if (String.IsNullOrEmpty(_requestMessage))
                    _requestMessage = BuildHttpRequestMessage();
                return _requestMessage;
            }
        }

        public Version ProtocolVersion
        {
            set
            {
                _version = String.Format("{0}.{1}", value.Major, value.Minor);
            }
            get
            {
                return new Version(_version);
            }
        }

        public override IWebProxy Proxy
        {
            get { return _proxy; }
            set { _proxy = (TcpProxy)value; }
        }

        public ICredentials Credentials
        {
            get { return _credencials; }
            set { _credencials = value; }
        }

        public bool RequestSubmitted
        {
            get { return _requestSubmitted; }
            private set { _requestSubmitted = value; }
        }

        public TcpConnection HTTPConnection
        {
            get
            {
                return connection;
            }
            set
            {
                connection = value;
            }
        }

        public override string Method
        {
            get
            {
                return _method ?? "GET";
            }
            set
            {
                if (Array.IndexOf(validHttpVerbs, value) != -1)
                {
                    _method = value;
                }
                else
                {
                    throw new ArgumentOutOfRangeException("value", string.Format("'{0}' is not a known HTTP verb.", value));
                }
            }
        }

        public override Uri RequestUri
        {
            get { return _requestUri; }
        }


        public override int Timeout
        {
            get
            {
                return _timeout;
            }
            set
            {
                _timeout = value;
            }
        }

        public override WebHeaderCollection Headers
        {
            get
            {
                if (_requestHeaders == null)
                {
                    _requestHeaders = new WebHeaderCollection();
                }
                return _requestHeaders;
            }
            set
            {
                if (RequestSubmitted)
                {
                    throw new InvalidOperationException("This operation cannot be performed after the request has been submitted.");
                }
                _requestHeaders = value;
            }
        }


        #endregion

        #region Constructor
        private SocksHttpWebRequest()
        {
        }

        public SocksHttpWebRequest(Uri requestUri)
        {
            _requestUri = requestUri;
        }

        #endregion

        #region WebRequest Members



        public override WebResponse GetResponse()
        {
            if (String.IsNullOrEmpty(Method))
            {
                throw new InvalidOperationException("Method has not been set.");
            }

            if (!RequestSubmitted) GetRequestStream();

            StringBuilder response = new StringBuilder();

            string line = StreamUtils.ReadLineValidating(connection.ConnectStream, "HTTP/", new ProtocolViolationException("Server returned a non-HTTP response"));       

           if(String.IsNullOrEmpty(line) || line == "\r")
               line = StreamUtils.ReadLineValidating(connection.ConnectStream, "HTTP/", new ProtocolViolationException("Server returned a non-HTTP response"));   
    
    
            if (String.IsNullOrEmpty(line))
            {
                bool bConnected = connection.Alive;
                throw new ProtocolViolationException("Server returned no Data");
            }

            while (!String.IsNullOrEmpty(line))
            {
                response.Append(line + "\r\n");
                line = StreamUtils.ReadLine(connection.ConnectStream);    
                if (line == "\r") break;
            }
            response.Append("\r\n");
            RequestSubmitted = true;
            Tracer.Write("response:\n" + response.ToString());
            return new SocksHttpWebResponse(response.ToString(), connection);
        }

        public override IAsyncResult BeginGetResponse(AsyncCallback callback, object state)
        {
            return base.BeginGetResponse(callback, state);
        }

     

        public override Stream GetRequestStream()
        {

            if (RequestSubmitted)
            {
                throw new InvalidOperationException("This operation cannot be performed after the request has been submitted.");
            }

            if (connection == null)
            {
                connection = ConnectionFactory.CreateConnection(RequestUri , (TcpProxy)Proxy);
            }
      
            connection.ReadTimeout = Timeout;
            connection.WriteTimeout = Timeout;
            connection.ConnectTimeout = ConnectTimeout;
            connection.Connect(RequestUri.Host , RequestUri.Port);

            connection.ConnectStream.Write(TextEncoding.GetBytes(RequestMessage), 0, RequestMessage.Length);
            RequestSubmitted = true;
            return new WriteonlyStream(connection.ConnectStream, false);
        }

        #endregion

        #region Methods

        public static new WebRequest Create(string requestUri)
        {
            return new SocksHttpWebRequest(new Uri(requestUri));
        }

        public static new WebRequest Create(Uri requestUri)
        {
            return new SocksHttpWebRequest(requestUri);
        }

        private string BuildHttpRequestMessage()
        {
            if (RequestSubmitted)
            {
                throw new InvalidOperationException("This operation cannot be performed after the request has been submitted.");
            }

            StringBuilder message = new StringBuilder();

            string sHost = RequestUri.Host;
            int Port = RequestUri.Port;

            string Prot = (this.RequestUri.AbsoluteUri.StartsWith("https")) ? "https" : "http";

            if (new Regex("^([a-f0-9]{4}:){7}[a-f0-9]{4}$").Match(sHost).Success)
            {
                sHost = "[" + sHost + "]";
            }

            bool usingProxy = _proxy != null && _proxy.ProxyType == ProxyTypes.Http && Prot != "https";

            string queryPath = (usingProxy) ?
                               (Prot + "://" + sHost + ":" + Port.ToString() + RequestUri.PathAndQuery) :
                               (RequestUri.PathAndQuery);

            if (queryPath == "/*") queryPath = "*";

            if (_version == "0.9")
            {
                message.AppendFormat("{0} {1}\r\n", Method, queryPath);
            }
            else message.AppendFormat("{0} {1} HTTP/{2}\r\n", Method, queryPath, _version);

            if (((Prot == "https" && Port != 443) || (Prot == "http" && Port != 80)) /*|| usingProxy*/) 
                   message.AppendFormat("Host: {0}:{1}\r\n", sHost, Port);
            else message.AppendFormat("Host: {0}\r\n", sHost);

            if (usingProxy && _proxy.Credentials != null)
            {
                NetworkCredential nc = (NetworkCredential)_proxy.Credentials;
                string data = Convert.ToBase64String
                    (Encoding.Default.GetBytes(nc.UserName + ":" + nc.Password));
                message.AppendFormat("Proxy-Authorization: Basic {0}\r\n", data);
            }

            if (Credentials != null)
            {
                NetworkCredential nc = (NetworkCredential)Credentials;
                string data = Convert.ToBase64String
                    (Encoding.Default.GetBytes(nc.UserName + ":" + nc.Password));
                message.AppendFormat("Authorization: Basic {0}\r\n", data);
            }

            if (!string.IsNullOrEmpty(UserAgent))
            {
                message.AppendFormat("User-Agent: {0}\r\n", UserAgent);
            }

            if (!string.IsNullOrEmpty(Accept))
            {
                message.AppendFormat("Accept: {0}\r\n", Accept);
            }

            if (!string.IsNullOrEmpty(Headers["Accept-Language"]))
            {
                message.AppendFormat("Accept-Language: {0}\r\n", Headers["Accept-Language"]);
            }

            if (!string.IsNullOrEmpty(Headers["Accept-Encoding"]))
            {
                message.AppendFormat("Accept-Encoding: {0}\r\n", Headers["Accept-Encoding"]);
            }

            if (!string.IsNullOrEmpty(Headers["Accept-Charset"]))
            {
                message.AppendFormat("Accept-Charset: {0}\r\n", Headers["Accept-Charset"]);
            }

            if (KeepAlive)
            {
                message.AppendFormat("Keep-Alive: {0}\r\n", KeepAliveTimeout);
            }
            if (!string.IsNullOrEmpty(Connection))
            {
                if (usingProxy)
                    message.Append("Proxy-");
                message.AppendFormat("Connection: {0}\r\n", Connection);
            }
            string[] notCustomHeaders = new string[] { 
                "user-agent", "accept", "accept-language", "accept-encoding", "accept-charset",
                "cookie", "referer", "proxy-authorization", 
                "keep-alive" , "connection" , "host"
            };



            // add the headers
            for (int i = 0; i < Headers.Keys.Count; i++)
            {
                if (!String.IsNullOrEmpty(Headers[Headers.Keys[i].ToString()]) &&
                    Array.IndexOf(notCustomHeaders, Headers.Keys[i].ToString().ToLower()) == -1)
                    message.AppendFormat("{0}: {1}\r\n", Headers.Keys[i], Headers[Headers.Keys[i].ToString()]);
            }

            if (!string.IsNullOrEmpty(Headers["Referer"]))
            {
                message.AppendFormat("Referer: {0}\r\n", Headers["Referer"]);
            }

            if (!string.IsNullOrEmpty(Headers["Cookie"]))
            {
                message.AppendFormat("Cookie: {0}\r\n", Headers["Cookie"]);
            }

            if (!string.IsNullOrEmpty(ContentType))
            {
                message.AppendFormat("Content-Type: {0}\r\n", ContentType);
            }
            if ( Method == "POST"|| Method == "PUT" || ContentLength > 0)
            {
                message.AppendFormat("Content-Length: {0}\r\n", ContentLength);
            }

            // add a blank line to indicate the end of the headers
            message.Append("\r\n");

            Tracer.Write("Headers to send:\n" + message.ToString());
            return message.ToString();
        }




        #endregion




    }
}
