using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Security;
using System.Threading;
using System.Reflection;
using System.Collections;
using Fenryr;
using Fenryr.IO;



namespace Fenryr.Http
{

    public enum HttpClientType
    {
        SystemNetHttpBased,
        SocketBased
    }

    public class HttpException : Exception
    {
        string response;
        public String Response { get { return response; } }
        public HttpException(Exception innerException, string Response) : base(innerException.Message , innerException)
        {
            response = Response;
        }
    }

 

    
    public class HttpClient : Classes.EventSupportable , ICloneable , IDisposable
    {     
        WebHeaderCollection _responseHeaders = new WebHeaderCollection();
        WebHeaderCollection _requestHeaders = new WebHeaderCollection();
       

        TcpProxy _proxy = null;
        bool allowControlRedirect = true;
        bool useCookies = true;
        Encoding encoding = Encoding.UTF8; // кодировка
        private HttpStatusCode responseCode;
        private string responseUrl;
        string userAgent;
        string accept;
        int timeout = 100000000;
        int downloadStep = 1024*16;
        int connectTimeout = 99990000;


        bool autoFollowMetaRefresh = false;

        int maxRedirects = 5;
        bool keepAlive = false;
        int keepAliveTimeout = 115;
       protected Exception _lastException = null;
       // CookieStorage _storage = new CookieStorage();
        Fenryr.Http.Cookies.CookieContainer _storage = new Fenryr.Http.Cookies.CookieContainer();

        public string ContentType;
        HttpClientType httpType = HttpClientType.SystemNetHttpBased;

        public Version ProtocolVersion = new Version("1.1");
        HttpSend httpSend = null;
        System.Net.Sockets.SocketError lastSocketError = System.Net.Sockets.SocketError.NotSocket;
        string responseHead = "";
        bool isSilent = false;
        bool m_ContinueIfResponseOver400 = false;



        #region Properties

        public bool AutoFollowMetaRefresh
        {
            get { return autoFollowMetaRefresh; }
            set { autoFollowMetaRefresh = value; }
        }

        public bool ContinueIfResponseOver400 
        {
            get { return m_ContinueIfResponseOver400; }
            set { m_ContinueIfResponseOver400 = value; }
        }

        public bool Silent
        {
            get { return isSilent; }
            set { isSilent = value; }
        }

        public string ResponseHead
        {
            get
            {
                return responseHead;
            }
        }

        public System.Net.Sockets.SocketError LastSocketError
        {
            get { return lastSocketError; }
       }

        public HttpClientType HttpType
        {
            get { return httpType; }
            set
            {
                //httpType = value;
              /*  switch (value)
                {
                    case HttpClientType.SocketBased: httpSend = new HttpSocksSend(this);
                        break;
                    case HttpClientType.SystemNetHttpBased: httpSend = new HttpWebSend(this);
                        break;
                }*/
            }
        }

        public bool KeepAlive
        {
            get { return keepAlive; }
            set { keepAlive = value; }
        }

        public int KeepAliveTimeout
        {
            get { return keepAliveTimeout; }
            set { keepAliveTimeout = value; }
        }

        public int DownloadStep
        {
            get { return downloadStep; }
            set { downloadStep = value; }
        }

        public int MaxRedirects
        {
            get { return maxRedirects; }
            set { maxRedirects = value; }
        }

        public int Timeout
        {
            get { return timeout; }
            set { timeout = value; }
        }

        public String UserAgent
        {
            get { return userAgent; }
            set { userAgent = value; }
        }

        public String Accept
         {
            get { return accept; }
            set { accept = value; }
        }

        public Encoding TextEncoding
        {
            get { return encoding; }
            set { encoding = value; }
        }

        public bool UseCookies
        {
            get { return useCookies; }
            set { useCookies = value;}
        }

        public TcpProxy Proxy
        {
            get { return _proxy; }
            set { _proxy = value; }
        }

        public bool AllowRedirect
        {
            get { return allowControlRedirect; }
            set { allowControlRedirect = value; }
        }

        public WebHeaderCollection ResponseHeaders
        {
            get { return _responseHeaders; }
        }

        public WebHeaderCollection RequestHeaders
        {
            get { return _requestHeaders; }
        }

    

        public HttpStatusCode ResponseCode
        {
            get { return responseCode; }
        }
       
        public string ResponseUrl
        {
            get {  return responseUrl; }
        }

        public String Referer
        {
           set { RequestHeaders["Referer"] = value;}
           get { return RequestHeaders["Referer"]; }
        }

        public Fenryr.Http.Cookies.CookieContainer Cookies
        {
            get
            {
                return _storage;
            }

            set
            {
                _storage = value;
            }
        }

        public Exception LastException { get { return _lastException; } } 

        #endregion



        public object Clone()
        {
            HttpClient httpClient = (HttpClient) this.MemberwiseClone();
            httpClient.RequestHeaders.Add(this.RequestHeaders);
            httpClient.UseCookies = this.UseCookies;
            httpClient._storage = (Fenryr.Http.Cookies.CookieContainer) Cookies.Clone();
            httpClient.encoding = this.encoding;
            httpClient.UserAgent = UserAgent;
            httpClient.Accept = Accept;
            return httpClient;
        }


        public int ConnectTimeout
        {
            get { return connectTimeout; }
            set { connectTimeout = value; }
        }
     

        public HttpClient() 
        {
            Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
            UserAgent = "Mozilla/5.0 (Windows; U; Windows NT 5.1; ru; rv:1.9.2.12) Gecko/20101026 MRA 5.7 (build 03686) Firefox/3.6.12";
            RequestHeaders["Accept-Language"] = "ru-ru,ru;q=0.8,en-us;q=0.5,en;q=0.3";
            RequestHeaders["Accept-Encoding"] = "gzip,deflate";          
            RequestHeaders["Accept-Charset"] = "windows-1251,utf-8;q=0.7,*;q=0.7";           
            RequestHeaders["Keep-Alive"] = "115";
            RequestHeaders["Referer"] = "";
            RequestHeaders["Cookie"] = "";
            UseCookies = true;
            httpSend = new HttpSocksSend(this);
            keepAlive = true;
        }
    

        #region Post and Get Method


        void ThrowException(Exception ex)
        {
            if (false == Silent)
                throw ex;
        }


        public bool MustChangeProxy()
        {
            if (Proxy == null || LastException == null)
                return false;
            
            if (Proxy.ProxyType == ProxyTypes.Http)
            {
                if (LastException is ProtocolViolationException)
                    return true;
                if ((int)responseCode == 502 ||
                    (int)responseCode == 503 ||
                    (int)responseCode == 504 ||
                    (int)responseCode == 405 ||
                    (int)responseCode == 403 ||
                    (int)responseCode == 407)
                    return true;
            }

            if ((Proxy.ProxyType == ProxyTypes.Socks4 || Proxy.ProxyType == ProxyTypes.Socks5) &&
                LastException is Fenryr.Net.Sockets.Socks.SocksProxyException)
            {
                return true;
            }
            return false;
        }



        void HandleException(Exception ex)
        {
            if (ex is ObjectDisposedException)
            {
                ex = new System.Net.Sockets.SocketException((int)System.Net.Sockets.SocketError.ConnectionAborted);
            }
            _lastException = ex;
            httpSend.Disconnect();
            Exception innerException = ex.InnerException;
            lastSocketError = System.Net.Sockets.SocketError.Success;

            if (ex is System.Net.Sockets.SocketException)
            {
                lastSocketError = (ex as System.Net.Sockets.SocketException).SocketErrorCode;
            }
            else if (innerException != null && innerException is System.Net.Sockets.SocketException)
            {
                lastSocketError = (innerException as System.Net.Sockets.SocketException).SocketErrorCode;
            }
            else if (ex is WebException )
            {
                WebException wEx = ex as WebException;
                if ((ex as WebException).Response != null)
                {
                    HttpWebResponse resp = wEx.Response as HttpWebResponse;
                    responseCode = resp.StatusCode;
                    string Content = "";
                    try
                    {
                        Content = new StreamReader(resp.GetResponseStream()).ReadToEnd();
                    }
                    catch
                    {
                    }
                    ThrowException(new HttpException(ex, Content));
                }
                else
                {
                    string Code = System.Text.RegularExpressions.Regex.Match(wEx.Message , @"HTTP/\d+.\d+ (\d{3})").Groups[1].Value;
                    int iCode = 0;
                    if (int.TryParse(Code, out iCode)) {
                        try
                        {
                            responseCode = (HttpStatusCode)iCode;
                        }
                        catch
                        {

                        }
                   }
                   ThrowException(new HttpException(ex, ex.Message));
                }
            } // if webexception
            else ThrowException (ex);

        }


    

        void PerformMethod(string sUrl, string method, Stream recStream , Stream sendStream , int numRedirect , bool SendRanged , int RangeStart , int RangeEnd)
        {
            
            bool wasAborted = false;

            do {
                responseHead = "";
                responseUrl = "";

                if (recStream != null && recStream.CanSeek)
                {
                    recStream.SetLength(0);
                    recStream.Seek(0, SeekOrigin.Begin);
                }

            WebRequest request = httpSend.GetRequest (sUrl , method);
            responseCode = HttpStatusCode.NoContent;            
            _lastException = null;
            lastSocketError = System.Net.Sockets.SocketError.Success;
           
               try
                {
                    if (method == "POST" || method == "PUT")
                    {                        
                        sendStream.Seek(0, SeekOrigin.Begin);
                        long len = sendStream.Length;
                        request.ContentLength = len;
                        using (Stream _stream = request.GetRequestStream())
                        {
                            bool abort;                          
                            StreamUtils.StreamToStream(sendStream, _stream, len, new StreamProgressHandler(DoProgress), WorkMode.Write, out abort, DownloadStep);
                            if (abort) return;
                        }
                    }//


                    WebResponse httpWebResponse = request.GetResponse();
                    try
                    {
                        responseUrl = sUrl;                                           
                        responseCode = httpSend.GetStatusCode(httpWebResponse);
                        long contentLen = httpWebResponse.ContentLength;
                        ResponseHeaders.Clear();
                        ResponseHeaders.Add(httpWebResponse.Headers);
                        responseHead = httpSend.GetStatusLine(httpWebResponse) + "\r\n" +
                                       ResponseHeaders.ToString();

                        if (contentLen == -1)
                            try
                            {
                                contentLen = long.Parse(httpWebResponse.Headers.Get("Content-Length"));
                            }
                            catch (ArgumentNullException)
                            {
                                contentLen = -1;
                            }

                        /*
                         if (UseCookies)
                        {
                            string sCookie = httpWebResponse.Headers["Set-Cookie"];
                            Cookies.SetCookie(sUrl, sCookie);
                        }
                         */

                        if ((responseCode == HttpStatusCode.MovedPermanently || responseCode == HttpStatusCode.SeeOther ||
                            responseCode == HttpStatusCode.Found)
                            && AllowRedirect && numRedirect < MaxRedirects)
                        {
                            bool Handled = false;
                            string path = httpWebResponse.Headers["location"];

                            path = UriUtils.BuildUrl(sUrl, path);
                            responseUrl = path;

                            DoRedirect(request, httpWebResponse, ref Handled, ref path);

                            if (method != "HEAD")
                            {
                                httpSend.PreworkResponse(sUrl , httpWebResponse, new MemoryStream(), contentLen);
                            }
                            if (!Handled)
                            {
                                httpSend.FinalizeResponse(httpWebResponse);
                                httpWebResponse = null;
                                PerformMethod(path, "GET", recStream, null, numRedirect + 1, false, 0, 0);
                                return;
                            }
                        }
                       
                        if (method != "HEAD")
                        {
                            httpSend.PreworkResponse(sUrl, httpWebResponse, recStream, contentLen);
                        }

                        if ((int)responseCode >= 400 && !ContinueIfResponseOver400)
                        {
                            recStream.Seek(0 , SeekOrigin.Begin);
                            throw new HttpException(new WebException("Error: " + ((int)responseCode).ToString()),
                                                   new StreamReader(recStream).ReadToEnd());
                        }
                        return; 
                    }
                    finally
                    {
                        if (httpWebResponse != null) httpSend.FinalizeResponse(httpWebResponse);
                        ContentType = null;
                    }

                    }
                       catch (Exception ex)
                       {
                           HandleException(ex);

                           if (LastSocketError != System.Net.Sockets.SocketError.ConnectionAborted ||
                               wasAborted)
                           {
                               ThrowException(ex);
                               return;
                           }

                           wasAborted = true;
                       }            
            }
            while (true);
        }

        // получение данных страницы
        public virtual string Get(string sUrl)
        {
            StartRequest();
            try
            {
                MemoryStream ms = new MemoryStream();
                PerformMethod(sUrl, "GET", ms, null, 0, false, 0, 0);
                ms.Seek(0, SeekOrigin.Begin);
                StreamReader reader = new StreamReader(ms , encoding);
                return reader.ReadToEnd();
            }
            finally
            {
                EndRequest();
            }
        }


        public virtual string Post(string sUrl, string sPostData)
        {
            StartRequest();
            try
            {
                MemoryStream ms = new MemoryStream();
                MemoryStream sendMs = new MemoryStream(encoding.GetBytes(sPostData));
                PerformMethod(sUrl, "POST", ms, sendMs, 0, false, 0, 0);
                ms.Seek(0, SeekOrigin.Begin);
                StreamReader reader = new StreamReader(ms, encoding);
                return reader.ReadToEnd();
            }
            finally
            {
                EndRequest();
            }
        }

        public virtual string Head(string sUrl)
        {
            StartRequest();
            try
            {
                PerformMethod(sUrl, "HEAD", null, null, 0, false, 0, 0);
                return ResponseHead;
            }
            finally
            {
                EndRequest();
            }
        }

        public virtual void Get(string sUrl, Stream stream)
        {
            StartRequest();
            try
            {
                PerformMethod(sUrl, "GET", stream, null, 0, false, 0, 0);
            }
            finally
            {
                EndRequest();
            }
        }
      

        public virtual string Post(string sUrl, Stream upStream)
        {
            StartRequest();
            try
            {
                MemoryStream ms = new MemoryStream();
                upStream.Seek(0, SeekOrigin.Begin);
                PerformMethod(sUrl, "POST", ms, upStream, 0, false, 0, 0);
                ms.Seek(0, SeekOrigin.Begin);
                StreamReader reader = new StreamReader(ms, encoding);
                return reader.ReadToEnd();
            }
            finally
            {
                EndRequest();
            }
        }

        public virtual string Put(string sUrl, Stream upStream)
        {
            StartRequest();
            try
            {
                MemoryStream ms = new MemoryStream();
                PerformMethod(sUrl, "PUT", ms, upStream, 0, false, 0, 0);
                ms.Seek(0, SeekOrigin.Begin);
                StreamReader reader = new StreamReader(ms, encoding);
                return reader.ReadToEnd();
            }
            finally
            {
                EndRequest();
            }
        }

        #endregion
 

        #region ClassEvents
        public class RedirectEventArgs : EventArgs
        {
            public readonly WebRequest httpWebRequest;
            public readonly WebResponse httpWebResponse;
            public bool Handled;
            public string Path;

            public RedirectEventArgs (WebRequest httpWebRequest , WebResponse httpWebResponse, string path)
            {
                 this.httpWebRequest = httpWebRequest;
                 this.httpWebResponse = httpWebResponse;
                 Handled = false;
                 Path = path;
            }
        }

        public event StreamProgressEventHandler ProcessProgress;
        
        public event EventHandler RequestStart; // перед загрузкой
        public event EventHandler RequestEnd; // после загрузки

        public delegate void RedirectHandler(object sender, RedirectEventArgs e);
        public event RedirectHandler Redirect;

        protected virtual void OnRedirect(object sender , RedirectEventArgs e)
        {
            if (Redirect != null)
            {
                Redirect (this, e);
            }
        }

        protected void DoRedirect(WebRequest httpWebRequest, WebResponse httpWebResponse, ref bool Handled, ref string path)
        {
            RedirectEventArgs e = new RedirectEventArgs(httpWebRequest, httpWebResponse, path);
            OnRedirect(this, e);
            Handled = e.Handled;
            path = e.Path;
        }


        protected virtual void OnRequestStart (EventArgs e)
        {
            if (RequestStart != null)
            {
                RequestStart (this, e);
            }
        }

        protected virtual void OnRequestEnd (EventArgs e)
        {
            if (RequestEnd != null)
            {
                RequestEnd (this, e);
            }
        }

        protected virtual void OnProcessProgress (DownloadEventArgs e)
        {
            if (ProcessProgress != null)
            {
                ProcessProgress(this, e);
            }
        }

  

        public void DoProgress(long bytesRead, long bytesAll, WorkMode mode , ref bool IsInterrupted , string what)
        {
            DownloadEventArgs de = new DownloadEventArgs(bytesRead, bytesAll, mode,  what);
            OnProcessProgress(de);
            IsInterrupted = de.IsInterrupted;
        }


       
        protected void StartRequest()
        {
            OnRequestStart(EventArgs.Empty);
        }

        protected void EndRequest()
        {
            OnRequestEnd (EventArgs.Empty);
        }
        #endregion


        public void Close()
        {
            if (httpSend is IDisposable)
                (httpSend as IDisposable).Dispose();
        }

        public void Disconnect()
        {
            httpSend.Disconnect();
        }

        public void Dispose()
        {
            Close();
            GC.SuppressFinalize(this);
        }


      
    }
}
