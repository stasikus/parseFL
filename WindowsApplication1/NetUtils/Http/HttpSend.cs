using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Security;
using Fenryr.IO;
using Fenryr.Net.Sockets;

namespace Fenryr.Http
{

   

    abstract class HttpSend : Fenryr.Classes.EventSupportable
    {     
       protected HttpClient _owner = null;

        public HttpSend(HttpClient owner)
        {
            _owner = owner;
        }

        public abstract WebRequest GetRequest(string Url,  string method);
        public abstract void PreworkResponse(string Url, WebResponse httpWebResponse, Stream recStream, long contentLen);
        public abstract void FinalizeResponse(WebResponse httpWebResponse);
        public abstract void Disconnect();
        public abstract HttpStatusCode GetStatusCode(WebResponse httpWebResponse);
        public abstract string GetStatusLine(WebResponse resp);
    }


    class HttpWebSend : HttpSend
    {

        public HttpWebSend(HttpClient owner) : base (owner)
        {
        }

 

        public override string GetStatusLine(WebResponse resp )
        {
            HttpWebResponse hresp = resp as HttpWebResponse;
            return String.Format("HTTP /{0}.{1} {2} {3}", _owner.ProtocolVersion.Major ,
                _owner.ProtocolVersion.Minor, (int)hresp.StatusCode, hresp.StatusCode.ToString()) ;
        }

        protected static bool CheckValidationResult(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }


        protected void SetCredencials(HttpWebRequest httpWebRequest)
        {
            X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);
            X509Certificate2Collection scollection = (X509Certificate2Collection)store.Certificates.Find(X509FindType.FindByTimeValid, DateTime.Now, false);

            foreach (X509Certificate2 x509 in scollection)
            {
                httpWebRequest.ClientCertificates.Add(x509);
            }

            httpWebRequest.Credentials = CredentialCache.DefaultCredentials;
            ServicePointManager.ServerCertificateValidationCallback +=
                                     new System.Net.Security.RemoteCertificateValidationCallback(CheckValidationResult);
        }

        public override WebRequest GetRequest(string Url,  string method)
        {
            if (!Uri.IsWellFormedUriString(Url, UriKind.Absolute))
                throw new UriFormatException();

            HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(Url);

            httpWebRequest.Referer = _owner.RequestHeaders["Referer"];
            httpWebRequest.AllowAutoRedirect = false;
            httpWebRequest.ReadWriteTimeout = _owner.Timeout;
            httpWebRequest.ProtocolVersion = _owner.ProtocolVersion;
            if (Url.Contains("https")) SetCredencials(httpWebRequest);

            if (_owner.Proxy != null)
            {
                httpWebRequest.Proxy = _owner.Proxy;
                if (Url.Contains("https")) httpWebRequest.Proxy.Credentials = CredentialCache.DefaultNetworkCredentials;
            }
            else httpWebRequest.Proxy = null;
            if (!String.IsNullOrEmpty(_owner.UserAgent)) httpWebRequest.UserAgent = _owner.UserAgent;
            if (!String.IsNullOrEmpty(_owner.Accept)) httpWebRequest.Accept = _owner.Accept;
           httpWebRequest.KeepAlive = _owner.KeepAlive;

            httpWebRequest.Method = method;
            if (_owner.UseCookies) //AddCookies();
            {
                string cookie =
                    _owner.Cookies.GetCookieHeader(Url);

                _owner.RequestHeaders["Cookie"] = cookie;
            }


            for (int i = 0; i < _owner.RequestHeaders.Count; i++)
                {
                    if (_owner.RequestHeaders.Keys[i] != "Referer" && !String.IsNullOrEmpty(_owner.RequestHeaders[_owner.RequestHeaders.Keys[i]]))
                        httpWebRequest.Headers.Add(_owner.RequestHeaders.Keys[i], _owner.RequestHeaders[_owner.RequestHeaders.Keys[i]]);
                }
            

            httpWebRequest.Timeout = _owner.Timeout;
            if (!String.IsNullOrEmpty(_owner.ContentType)) httpWebRequest.ContentType = _owner.ContentType;

            return httpWebRequest;
        }

        public override void PreworkResponse(string Url, WebResponse httpWebResponse, Stream recStream, long contentLen)
        {
            bool abort;
            Stream _stream = httpWebResponse.GetResponseStream();
            string Headers = httpWebResponse.Headers.ToString();
            System.Text.RegularExpressions.Regex reg = new System.Text.RegularExpressions.Regex(@"charset=([\w_\-]+)");

            try
            {
                _owner.TextEncoding = Encoding.GetEncoding(reg.Match(Headers).Groups[1].Value);
            }
            catch { }
            //Transfer-Encoding: chunked

            bool bChunked = (httpWebResponse.Headers["Transfer-Encoding"] != null && httpWebResponse.Headers["Transfer-Encoding"].ToLower().Contains("chunked"));
            bool isDeflate = (httpWebResponse.Headers["Content-Encoding"] != null && httpWebResponse.Headers["Content-Encoding"].Contains("deflate"));
            bool isGzip = (httpWebResponse.Headers["Content-Encoding"] != null && httpWebResponse.Headers["Content-Encoding"].Contains("gzip"));

            if (isGzip)
            {
                MemoryStream stream = new MemoryStream();
                if (contentLen == -1)
                    StreamUtils.StreamToStreamWhenLengthUnknown(_stream, stream, new StreamProgressHandler(_owner.DoProgress),
                        WorkMode.Read, out abort, _owner.DownloadStep, bChunked);
                else StreamUtils.StreamToStream(_stream, stream, contentLen, new StreamProgressHandler(_owner.DoProgress),
                      WorkMode.Read, out abort, _owner.DownloadStep);
                stream.Seek(0, SeekOrigin.Begin);
                StreamUtils.CopyStream(new System.IO.Compression.GZipStream(stream, System.IO.Compression.CompressionMode.Decompress), recStream);
            }
            else if (isDeflate)
            {
                MemoryStream stream = new MemoryStream();
                if (contentLen == -1)
                    StreamUtils.StreamToStreamWhenLengthUnknown(_stream, stream, new StreamProgressHandler(_owner.DoProgress),
                        WorkMode.Read, out abort, _owner.DownloadStep, bChunked);
                else StreamUtils.StreamToStream(_stream, stream, contentLen, new StreamProgressHandler(_owner.DoProgress),
                      WorkMode.Read, out abort, _owner.DownloadStep);
                stream.Seek(0, SeekOrigin.Begin);
                StreamUtils.CopyStream(new System.IO.Compression.DeflateStream(stream, System.IO.Compression.CompressionMode.Decompress), recStream);
            }
            else
            {
                if (contentLen == -1)
                    StreamUtils.StreamToStreamWhenLengthUnknown(_stream, recStream, new StreamProgressHandler(_owner.DoProgress),
                        WorkMode.Read, out abort, _owner.DownloadStep, bChunked);
                else StreamUtils.StreamToStream(_stream, recStream, contentLen, new StreamProgressHandler(_owner.DoProgress),
                      WorkMode.Read, out abort, _owner.DownloadStep);
            }
        }

        public override void FinalizeResponse(WebResponse httpWebResponse)
        {
            httpWebResponse.Close();
        }

        public override HttpStatusCode GetStatusCode(WebResponse httpWebResponse)
        {
            return (httpWebResponse as HttpWebResponse).StatusCode;
        }

        public override void Disconnect() { }
    }

    class HttpSocksSend : HttpSend , IDisposable
    {

        TcpConnection connection = null;
        ConnectionCache _cache = new ConnectionCache();
        string _Url = null;




        public override string GetStatusLine(WebResponse resp)
        {
            return (resp as SocksHttpWebResponse).StatusLine;
        }

        public HttpSocksSend(HttpClient owner)
            : base(owner)
        {
        }

        public override WebRequest GetRequest(string Url, string method)
        {
            Uri _uri = new Uri(Url);
            SocksHttpWebRequest httpWebRequest = new SocksHttpWebRequest (_uri);
            _Url = Url;
            if (_owner.Proxy != null)
                httpWebRequest.Proxy = _owner.Proxy;
            httpWebRequest.ProtocolVersion = _owner.ProtocolVersion;
            httpWebRequest.KeepAlive = _owner.KeepAlive;
            if (_owner.KeepAlive)
            {
                httpWebRequest.Connection = "keep-alive";
            }
            else httpWebRequest.Connection = "close";
            httpWebRequest.KeepAliveTimeout = _owner.KeepAliveTimeout;
            httpWebRequest.ConnectTimeout = _owner.ConnectTimeout;
            if (!String.IsNullOrEmpty(_owner.UserAgent)) httpWebRequest.UserAgent = _owner.UserAgent;
            if (!String.IsNullOrEmpty(_owner.Accept)) httpWebRequest.Accept = _owner.Accept;

            httpWebRequest.Method = method;
            
            if (_owner.UseCookies) 
            {
                string cookie =
                   _owner.Cookies.GetCookieHeader(Url);

                _owner.RequestHeaders["Cookie"] = cookie;
            }

            httpWebRequest.Headers.Add(_owner.RequestHeaders); // добавляем заголовки в запрос


            TcpConnection connection = this._cache.GetConnection(_uri.Host , _uri.Port , _owner.Proxy);

            if (connection == null)
                connection = ConnectionFactory.CreateConnection(_uri, _owner.Proxy);
          

            httpWebRequest.HTTPConnection = connection;
            httpWebRequest.Timeout = _owner.Timeout;
            if (!String.IsNullOrEmpty(_owner.ContentType)) httpWebRequest.ContentType = _owner.ContentType;
            return httpWebRequest;
        }


        
        public override void PreworkResponse(string Url , WebResponse httpWebResponse, Stream recStream, long contentLen)
        {
            bool abort;
            Stream _stream = httpWebResponse.GetResponseStream();
            string Headers = httpWebResponse.Headers.ToString();
            System.Text.RegularExpressions.Regex reg = new System.Text.RegularExpressions.Regex(@"charset=([\w_\-]+)");

            try
            {
                _owner.TextEncoding = Encoding.GetEncoding(reg.Match(Headers).Groups[1].Value);
            }
            catch { }

            bool isDeflate = (httpWebResponse.Headers["Content-Encoding"] != null && httpWebResponse.Headers["Content-Encoding"].Contains("deflate"));
            bool isGzip = (httpWebResponse.Headers["Content-Encoding"] != null && httpWebResponse.Headers["Content-Encoding"].Contains("gzip"));
            bool bChunked = (httpWebResponse.Headers["Transfer-Encoding"] != null && httpWebResponse.Headers["Transfer-Encoding"].ToLower().Contains("chunked"));

            if (_owner.UseCookies)
            {
                SocksHttpWebResponse resp = (SocksHttpWebResponse)httpWebResponse;
               foreach( string sCookie in resp.SetCookieHeaders)
                 _owner.Cookies.SetCookie(Url, sCookie);
            }

            if (contentLen == -1 )
            {
                  if (isGzip)
                {
                    Stream stream = new System.IO.MemoryStream();
                    StreamUtils.StreamToStreamWhenLengthUnknown(_stream, stream, _owner.DoProgress, WorkMode.Read, out abort, _owner.DownloadStep, bChunked);
                    stream.Seek(0, SeekOrigin.Begin);
                    StreamUtils.CopyStream(new System.IO.Compression.GZipStream(stream, System.IO.Compression.CompressionMode.Decompress), recStream);
                }
                else if (isDeflate)
                {
                    Stream stream = new System.IO.MemoryStream();
                    StreamUtils.StreamToStreamWhenLengthUnknown(_stream, stream, _owner.DoProgress, WorkMode.Read, out abort, _owner.DownloadStep, bChunked);
                    stream.Seek(0, SeekOrigin.Begin);
                    StreamUtils.CopyStream(new System.IO.Compression.DeflateStream(stream, System.IO.Compression.CompressionMode.Decompress), recStream);
                }
                  else StreamUtils.StreamToStreamWhenLengthUnknown
               (_stream, recStream, _owner.DoProgress, WorkMode.Read, out abort, _owner.DownloadStep, bChunked);
            }
     
            else
            {
                if (isGzip)
                {
                    Stream stream = new System.IO.MemoryStream();
                    StreamUtils.StreamToStream(_stream, stream, contentLen, _owner.DoProgress, WorkMode.Read, out abort, _owner.DownloadStep);
                    stream.Seek(0, SeekOrigin.Begin);
                    StreamUtils.CopyStream(new System.IO.Compression.GZipStream(stream, System.IO.Compression.CompressionMode.Decompress), recStream);
                }
                else if (isDeflate)
                {
                    Stream stream = new System.IO.MemoryStream();
                    StreamUtils.StreamToStream(_stream, stream, contentLen, _owner.DoProgress, WorkMode.Read, out abort, _owner.DownloadStep);
                    stream.Seek(0, SeekOrigin.Begin);
                    StreamUtils.CopyStream(new System.IO.Compression.DeflateStream(stream, System.IO.Compression.CompressionMode.Decompress), recStream);
                }
                else StreamUtils.StreamToStream(_stream, recStream, contentLen, _owner.DoProgress, WorkMode.Read, out abort, _owner.DownloadStep);
            }
        }

        public override void FinalizeResponse(WebResponse httpWebResponse)
        {
            bool bClose = false;
            bool bProxyClose = false;

            if (!String.IsNullOrEmpty(httpWebResponse.Headers["Connection"]) &&
                httpWebResponse.Headers["Connection"].ToLower().Contains("close"))
            {
                bClose = true;
            }
            else if (!String.IsNullOrEmpty(httpWebResponse.Headers["Proxy-Connection"]) &&
                httpWebResponse.Headers["Proxy-Connection"].ToLower().Contains("close"))
            {
                bProxyClose = true;
            }

            bool bchunked = (httpWebResponse.Headers["Transfer-Encoding"] != null && httpWebResponse.Headers["Transfer-Encoding"].ToLower().Contains("chunked"));

            if (!bchunked && httpWebResponse.ContentLength < 0)
            {
                bClose = true;
                bProxyClose = true;
            }

           
            if ( bClose)
            {
                httpWebResponse.Close();
            }
            else if (_owner.Proxy != null && bProxyClose)
            {
                 httpWebResponse.Close();
            }
            else
            {
                SocksHttpWebResponse response = (SocksHttpWebResponse)httpWebResponse;
               
                bool bAlive = response.HTTPConnection.Alive;
                if (bAlive)
                {
                    _cache.AddConnection(response.HTTPConnection);
                }
                else  httpWebResponse.Close();
            }       
        }

        public override HttpStatusCode GetStatusCode(WebResponse httpWebResponse)
        {
            return (httpWebResponse as SocksHttpWebResponse).StatusCode;
        }


        public void Close()
        {
            Disconnect();
        }

        public void Dispose()
        {
            Close();
            GC.SuppressFinalize(this);
        }

        public override void Disconnect() {
            try
            {
                if (connection != null) connection.Close();            
            }
            catch { }
            finally
            {
                connection = null;
            }
            _cache.Clear();
        }

    }

}
