using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Fenryr.Http
{
    class HttpClientEx : HttpClient
    {
        int m_MaxConnectAttempts = 6;
        int m_ConnectDelay = 500;

        public HttpClientEx()
            : base()
        {
        }

        public int MaxConnectAttempts
        {
            get { return m_MaxConnectAttempts; }
            set { m_MaxConnectAttempts = value; }
        }

        public int ConnectDelay
        {
            get { return m_ConnectDelay; }
            set { m_ConnectDelay = value; }
        }

        public override string Get(string sUrl)
        {

            for (int i = 0; i < MaxConnectAttempts; i++)
            {
                try
                {
                    return base.Get(sUrl);
                }
                catch (Exception ex)
                {
                    Disconnect();
                    if (i == MaxConnectAttempts - 1)
                        throw ex;
                    if (this.MustChangeProxy())
                        throw ex;
                    else if (LastSocketError != System.Net.Sockets.SocketError.ConnectionRefused &&
                        LastSocketError != System.Net.Sockets.SocketError.ConnectionAborted &&
                        LastSocketError != System.Net.Sockets.SocketError.ConnectionReset)
                        throw ex;
                    System.Threading.Thread.Sleep(ConnectDelay);
                }
            }
            return string.Empty;
        }

        public override void Get(string sUrl, Stream stream)
        {
            for (int i = 0; i < MaxConnectAttempts; i++)
            {
                try
                {
                     base.Get(sUrl, stream);
                     return;
                }
                catch (Exception ex)
                {
                    
                    Disconnect();
                    stream.SetLength(0);
                    stream.Seek(0 , SeekOrigin.Begin);
                    if (i == MaxConnectAttempts - 1)
                        throw ex;
                    if (this.MustChangeProxy())
                        throw ex;
                    else if (LastSocketError != System.Net.Sockets.SocketError.ConnectionRefused &&
                        LastSocketError != System.Net.Sockets.SocketError.ConnectionAborted &&
                        LastSocketError != System.Net.Sockets.SocketError.ConnectionReset)
                        throw ex;
                    System.Threading.Thread.Sleep(ConnectDelay);
                }
            }
        }

        public override string Post(string sUrl, string Content)
        {
            MemoryStream ms = new MemoryStream();
            byte[] data = TextEncoding.GetBytes(Content);
            ms.Write(data, 0, data.Length);
            ms.Seek(0 , SeekOrigin.Begin);
            return Post(sUrl , ms);
        }
        public override string Post(string sUrl, Stream Content)
        {

            string contType = this.ContentType;
            for (int i = 0; i < MaxConnectAttempts; i++)
            {
                try
                {
                    ContentType = contType;
                    return base.Post(sUrl, Content);
                }
                catch (Exception ex)
                {
                    Disconnect();
                    if (i == MaxConnectAttempts - 1)
                        throw ex;
                    if (this.MustChangeProxy())
                        throw ex;
                    else if (LastSocketError != System.Net.Sockets.SocketError.ConnectionRefused &&
                        LastSocketError != System.Net.Sockets.SocketError.ConnectionAborted &&
                        LastSocketError != System.Net.Sockets.SocketError.ConnectionReset)
                        throw ex;
                    System.Threading.Thread.Sleep(ConnectDelay);
                }
            }
            return string.Empty;
        }
    }
}
