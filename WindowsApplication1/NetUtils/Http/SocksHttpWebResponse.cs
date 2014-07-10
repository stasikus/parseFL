using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Fenryr.IO;
using System.Text.RegularExpressions;
using System.IO;
using Fenryr.Net.Sockets;

namespace Fenryr.Http
{
    internal class SocksHttpWebResponse : WebResponse
    {

        #region Member Variables

        private WebHeaderCollection _httpResponseHeaders;
        private string _responseContent;
        string _headerStr;
        string statusLine;
        List<string> m_SetCookieHeaders = new List<string>();

        public string StatusLine {
        get {return statusLine; }
      }

        public string[] SetCookieHeaders
        {
            get
            {
                return m_SetCookieHeaders.ToArray();
            }
        }

        public string HeadersString
        {
            get
            {
                return _headerStr;
            }
        }

        HttpStatusCode _httpStatusCode;
        public HttpStatusCode StatusCode
        {
            get { return _httpStatusCode; }
        }
       ReadonlyStream _responseStream;

        public override Stream GetResponseStream()
        {
            return _responseStream;
        }


        CookieContainer _cookies = new CookieContainer();

        CookieContainer SetCookies
        {
            get
            {
                return _cookies;
            }
        }

        #endregion

        #region Constructors

        public SocksHttpWebResponse(string httpResponseMessage, TcpConnection conn)
        {
            SetHeadersAndResponseContent(httpResponseMessage);
            _headerStr = httpResponseMessage;
            if (ContentLength > -1) 
            {
                _responseStream = new IdentityStream(conn.ConnectStream, false, ContentLength);
            }
            else if (Headers["Transfer-Encoding"] != null && Headers["Transfer-Encoding"].ToLower().Contains("chunked"))
            {
                _responseStream = new ChunkStream(conn.ConnectStream, false);
            }
            else _responseStream = new ReadonlyStream(conn.ConnectStream, false);
            _connection = conn;
        }



        #endregion

        #region WebResponse Members

        TcpConnection _connection;

        public TcpConnection HTTPConnection
        {
            get
            {
                return _connection;
            }
        }


        public override void Close()
        {
            HTTPConnection.Close();
        }

        public override WebHeaderCollection Headers
        {
            get
            {
                if (_httpResponseHeaders == null)
                {
                    _httpResponseHeaders = new WebHeaderCollection();
                }
                return _httpResponseHeaders;
            }
        }

        public override long ContentLength
        {
            get
            {
                return (Headers["Content-Length"] != null) ?
                long.Parse(Headers["Content-Length"]) : -1;
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        #endregion

        #region Methods

        private void SetHeadersAndResponseContent(string responseMessage)
        {
            // the HTTP headers can be found before the first blank line
            int indexOfFirstBlankLine = responseMessage.IndexOf("\r\n\r\n");

            string headers = responseMessage.Substring(0, indexOfFirstBlankLine);
            string[] headerValues = headers.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            // ignore the first line in the header since it is the HTTP response code
            Regex reg = new Regex(@"\d{3}(\.\d+)?");
            string sResp = reg.Match(headerValues[0]).Value;
            try
            {
                _httpStatusCode = (System.Net.HttpStatusCode)Enum.Parse(typeof(System.Net.HttpStatusCode), sResp);
            }
            catch { }
            statusLine = headerValues[0];
            for (int i = 1; i < headerValues.Length; i++)
            {
                int pos = headerValues[i].IndexOf(":");
                string headerName = headerValues[i].Substring(0, pos);
                if (headerName.ToLower() == "set-cookie" ||
                    headerName.ToLower() == "set-cookie2")
                {
                    m_SetCookieHeaders.Add(headerValues[i]);
                }

                try
                {
                    Headers.Add(headerName,
                                headerValues[i].Substring(pos + 1, headerValues[i].Length - pos - 1));
                }
                catch
                {

                }
            }

            ResponseContent = responseMessage.Substring(indexOfFirstBlankLine + 4);

        }

        #endregion

        #region Properties

        private string ResponseContent
        {
            get { return _responseContent ?? string.Empty; }
            set { _responseContent = value; }
        }

        #endregion




    }
}
