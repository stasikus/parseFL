using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace Fenryr.Net.Sockets
{
    class TcpException : Exception
    {
        public TcpException(string message) : base(message) { }
        public TcpException(string message, Exception innerExeption) : base(message, innerExeption) { }
    }


    public class TcpSocket : IDisposable
    {
        Socket sock = null;
        bool m_Connected = false;
        bool m_Blocked = true;
        int m_ConnectTimeout = 10000;
        int m_ReceiveTimeout = 20000;
        int m_SendTimeout = 20000;

        IPEndPoint m_EndPoint;

        public IPEndPoint RemoteEndPoint
        {
            get { return m_EndPoint; }
        }


        #region Properties

        public Socket Socket
        {
            get { return sock; }
        }

        public bool Connected
        {
            get { return m_Connected; }
        }

        public bool Blocked
        {
            get { return m_Blocked; }
            set
            {
                m_Blocked = value;
                if (Connected)
                    sock.Blocking = value;
            }
        }

        public int ConnectTimeout
        {
            get { return m_ConnectTimeout; }
            set { m_ConnectTimeout = value; }
        }

        public int SendTimeout
        {
            get { return m_SendTimeout; }
            set { m_SendTimeout = value; }
        }

        public int ReceiveTimeout
        {
            get { return m_ReceiveTimeout; }
            set { m_ReceiveTimeout = value; }
        }


        public int AvailableData
        {
            get
            {
                return (sock.Available > 0x10000) ? 0x10000 : sock.Available;
            }
        }
        #endregion


        void DisconnectSocket()
        {
            try
            {
                if (m_Connected)
                {
                    try
                    {
                        sock.Shutdown(SocketShutdown.Send);
                       // if (AvailableData > 0)
                          //  ReceivePacket(0);
                    }
                    catch { }
                }
                sock.Close();
            }
            catch
            {
            }
            finally
            {
                m_Connected = false;
            }
        }


        public void Connect(IPEndPoint destPoint)
        {
            if (Connected)
            {
                DisconnectSocket();
            }


            bool isIpv6 = destPoint.Address.IsIPv6LinkLocal ||
                                    destPoint.Address.IsIPv6Multicast ||
                                    destPoint.Address.IsIPv6SiteLocal;

            AddressFamily family = (isIpv6) ?
                                    AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork;

            sock = new Socket(family, SocketType.Stream, ProtocolType.Tcp);
            sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, m_ReceiveTimeout);
            sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, m_SendTimeout);

            m_EndPoint = destPoint;
            try
            {
                sock.Blocking = Blocked;
                sock.Connect(destPoint);
                m_Connected = true;
            }
            catch (SocketException ex)
            {
                if (ex.ErrorCode == 10035) // would block
                {
                    int nIntervals = (int)ConnectTimeout / 50;
                    for (int i = 0; i < nIntervals; i++)
                    {
                        if (sock.Poll(10000, SelectMode.SelectWrite))
                        {
                            // connected
                            m_Connected = true;
                            return;
                        }

                        if (sock.Poll(0, SelectMode.SelectError))
                        {
                            //sock.Blocking = false;
                            throw new TcpException(String.Format("Error connecting to: {0}", destPoint.ToString()),
                                        new SocketException((int)SocketError.ConnectionRefused));
                        }
                        System.Threading.Thread.Sleep(40);
                    }
                    throw new SocketException((int)SocketError.TimedOut);
                }
                else throw;
            }
        }

        public int Send(byte[] buffer)
        {
            return Send(buffer, 0, buffer.Length);
        }


        public bool CanRead(int Timeout)
        {
            int nperiods = (int)Timeout / 50;
            for (int i = 0; i < nperiods; i++)
            {
                if (sock.Poll(0, SelectMode.SelectRead) ||
                    sock.Available > 0)
                    return true;

                System.Threading.Thread.Sleep(50);
            }
            return false;
        }

        public bool CanWrite(int Timeout)
        {
            return (sock.Poll(Timeout * 1000, SelectMode.SelectWrite));
        }

        public byte[] ReceivePacket()
        {
            return ReceivePacket(m_ReceiveTimeout);
        }


        Exception lastExcept = null;

        public Exception LastException
        {
            get
            {
                return lastExcept;
            }
        }

        public byte[] ReceivePacket(int Timeout)
        {
            lastExcept = null;
            try
            {
                int received = 0;
                if (Socket.Available == 0 && CanRead(Timeout) == false)
                {
                    DisconnectSocket();
                    throw new TcpException("Packet receive timeout", new SocketException((int)SocketError.TimedOut));
                 //   return null; //no data
                }
                else if (Socket.Available == 0 && CanRead(Timeout) == true)
                {
                    return null; //no data
                }

                byte[] buffer = new byte[Socket.Available];

                while (Socket.Available > 0)
                {
                    int nRecv = Receive(buffer, received, buffer.Length - received, m_ReceiveTimeout);
                    if (nRecv == 0) break;
                    received += nRecv;
                    if (Socket.Available > 0)
                    {
                        int len = Socket.Available + received;
                        if (len > buffer.Length)
                            Array.Resize(ref buffer, len);
                    }
                }

                if (received < buffer.Length)
                    Array.Resize(ref buffer, received);

                return buffer;
            }
            catch (Exception ex)
            {

                lastExcept = ex;
                return null;
            }
        }


        public int Receive(byte[] buffer, int offset, int size, int ReceiveTimeout)
        {
            long startTickCount = Fenryr.Utils.GetTimeStamp();
            int received = 0;


            while (received != size)
            {
                try
                {
                    int nRecv = sock.Receive(buffer, offset + received, size - received, SocketFlags.None);
                    received += nRecv;
                }
                catch (SocketException ex)
                {

                    if (ex.SocketErrorCode == SocketError.WouldBlock)
                    {
                        int nIntervals = (int)ReceiveTimeout / 10;
                        for (int i = 0; i < nIntervals; i++)
                        {
                            if (sock.Poll(10000, SelectMode.SelectRead))
                            {
                                received += sock.Receive(buffer, offset + received, size - received, SocketFlags.None);
                                if (received == size)
                                    return received;
                            }
                            else if (sock.Poll(0, SelectMode.SelectError))
                            {
                                DisconnectSocket();
                                throw new TcpException("Receive Error", new SocketException((int)SocketError.ConnectionAborted));
                            }
                        }
                        DisconnectSocket();
                        throw new TcpException("Receive timeout", new SocketException((int)SocketError.TimedOut));
                    }
                    else
                    {
                        DisconnectSocket();
                        throw;
                    }

                }
                sock.Poll(10000, SelectMode.SelectRead);

                if (Fenryr.Utils.GetTimeStamp() > startTickCount + ReceiveTimeout)
                {
                    DisconnectSocket();
                    throw new TcpException("Receive timeout", new SocketException((int)SocketError.TimedOut));
                }

            } // while
            return received;


        }

        public int Receive(byte[] buffer, int offset, int size)
        {
            return Receive(buffer, offset, size, m_ReceiveTimeout);
        }


        public int Send(byte[] buffer, int offset, int size)
        {
            long startTickCount = Fenryr.Utils.GetTimeStamp();
            int sent = 0;  // how many bytes is already sent

            do
            {

                try
                {
                    sent += this.sock.Send(buffer, offset + sent, size - sent, SocketFlags.None);
                }
                catch (SocketException ex)
                {
                    if (ex.SocketErrorCode == SocketError.WouldBlock ||
                        ex.SocketErrorCode == SocketError.IOPending ||
                        ex.SocketErrorCode == SocketError.NoBufferSpaceAvailable)
                    {
                        // socket buffer is probably full, wait and try again
                        while (Fenryr.Utils.GetTimeStamp() <= startTickCount + m_SendTimeout)
                        {
                            if (sock.Poll(10000, SelectMode.SelectWrite))
                                break;
                            System.Threading.Thread.Sleep(30);
                        }
                    }
                    else
                        throw ex;  // any serious error occurr
                }
                if (sent == size) break;

                if (Fenryr.Utils.GetTimeStamp() > startTickCount + m_SendTimeout)
                    throw new TcpException("Send timeout", new SocketException((int)SocketError.TimedOut));

            } while (sent < size);
            return sent;

        }

        public void Disconnect()
        {
            DisconnectSocket();
        }


        public void Dispose()
        {
            Close();
            GC.SuppressFinalize(this);
        }

        public void Close()
        {
            Disconnect();
        }

    }
}
