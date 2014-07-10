using System;
using System.Collections.Generic;
using System.Text;
using Fenryr.Net.Sockets;
using System.Net;
using System.Text.RegularExpressions;

namespace Fenryr
{
    public class ProxyProtocolDetector
    {
        Exception except = null;

        public Exception LastException {
            get { return except; }
    }

        public bool Detect(TcpProxy proxy, Uri RequestUri, int TimeOut)
        {
            except = null;
            try
            {
             using(   TcpSocket sock = new TcpSocket()) {
                sock.Blocked = false;
                sock.ConnectTimeout = TimeOut;
               // IPAddress addr =Dns.Resolve(uri.Host)
                
                string sHost = RequestUri.Host;
                int Port = RequestUri.Port;

                string Prot = (RequestUri.AbsoluteUri.StartsWith("https")) ? "https" : "http";

                if (new Regex("^([a-f0-9]{4}:){7}[a-f0-9]{4}$").Match(sHost).Success)
                {
                    sHost = "[" + sHost + "]";
                }

                string queryPath = (Prot + "://" + sHost + ":" + Port.ToString() + RequestUri.PathAndQuery);


                string str = "GET " + queryPath + " HTTP/1.0\r\nHost: " + RequestUri.Host + "\r\n\r\n";

                
                // byte[] data = new byte[4] { 5, 2, 0, 2 };
                byte[] data = Encoding.Default.GetBytes(str);

                for (int i = 0; i < 3; i++)
                {
                    sock.Connect(new IPEndPoint(TcpConnection.GetIpAddress(proxy.Host), proxy.Port));
                    sock.ReceiveTimeout = 20000;
                    
                    sock.Send(data, 0, data.Length);


                    byte[] buffer = null;

                    try
                    {
                        buffer = sock.ReceivePacket();
                    }
                    catch
                    {
                        sock.Close();
                        System.Threading.Thread.Sleep(5000);
                        data = new byte[4] { 5, 2, 0, 2 };
                        continue;
                    }


                    string resp = Encoding.Default.GetString(buffer);
                    if (resp.StartsWith("HTTP/"))
                    {
                        string Code = Regex.Match(resp, @"HTTP/\d+\.\d+ (\d{3})").Groups[1].Value;
                        int code = 0;
                        if (!int.TryParse(Code, out code))
                        {
                            throw new ProtocolViolationException("No response code for HTTP Proxy");
                        }

                        if (code >= 400)
                            throw new ProtocolViolationException("Not Valid Proxy");

                        proxy.ProxyType = ProxyTypes.Http;
                        return true;
                    }

                    //HTTP/1.1 301 Moved Permanently



                    if (buffer.Length == 8)
                    {
                        if (buffer[0] == 0 && buffer[1] >= 0x5a && buffer[1] <= 0x5d)
                        {
                            proxy.ProxyType = ProxyTypes.Socks4;
                            return true;
                        }
                    }

                    if (buffer.Length == 2 && buffer[0] == 5)
                    {
                        proxy.ProxyType = ProxyTypes.Socks5;
                        return true;
                    }

                    if (buffer.Length == 0)
                    {
                        sock.Close();
                        System.Threading.Thread.Sleep(5000);
                        data = new byte[4] { 5, 2, 0, 2 };
                        continue;
                    }
                    break;
                }

                throw new ProtocolViolationException("Protocol Not Detected");
               
             } // using

            }
            catch (Exception ex)
            {
                except = ex;
                return false;
            }
        }
    }
}
