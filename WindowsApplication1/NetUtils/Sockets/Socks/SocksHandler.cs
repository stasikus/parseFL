using System;
using System.Collections.Generic;
using System.Text;
using System.Net;

namespace Fenryr.Net.Sockets.Socks
{
    internal sealed class Socks4Handler : SocksHandler
    {
        /// <summary>
        /// Initilizes a new instance of the SocksHandler class.
        /// </summary>
        /// <param name="server">The socket connection with the proxy server.</param>
        /// <param name="user">The username to use when authenticating with the server.</param>
        /// <exception cref="ArgumentNullException"><c>server</c> -or- <c>user</c> is null.</exception>
        public Socks4Handler(TcpSocket server, string user) : base(server, user) { }
        /// <summary>
        /// Creates an array of bytes that has to be sent when the user wants to connect to a specific host/port combination.
        /// </summary>
        /// <param name="host">The host to connect to.</param>
        /// <param name="port">The port to connect to.</param>
        /// <returns>An array of bytes that has to be sent when the user wants to connect to a specific host/port combination.</returns>
        /// <remarks>Resolving the host name will be done at server side. Do note that some SOCKS4 servers do not implement this functionality.</remarks>
        /// <exception cref="ArgumentNullException"><c>host</c> is null.</exception>
        /// <exception cref="ArgumentException"><c>port</c> is invalid.</exception>
        private byte[] GetHostPortBytes(string host, int port)
        {
            if (host == null)
                throw new ArgumentNullException();
            if (port <= 0 || port > 65535)
                throw new ArgumentException();
            byte[] connect = new byte[10 + Username.Length + host.Length];
            connect[0] = 4;
            connect[1] = 1;
            Array.Copy(PortToBytes(port), 0, connect, 2, 2);
            connect[4] = connect[5] = connect[6] = 0;
            connect[7] = 1;
            Array.Copy(Encoding.ASCII.GetBytes(Username), 0, connect, 8, Username.Length);
            connect[8 + Username.Length] = 0;
            Array.Copy(Encoding.ASCII.GetBytes(host), 0, connect, 9 + Username.Length, host.Length);
            connect[9 + Username.Length + host.Length] = 0;
            return connect;
        }
        /// <summary>
        /// Creates an array of bytes that has to be sent when the user wants to connect to a specific IPEndPoint.
        /// </summary>
        /// <param name="remoteEP">The IPEndPoint to connect to.</param>
        /// <returns>An array of bytes that has to be sent when the user wants to connect to a specific IPEndPoint.</returns>
        /// <exception cref="ArgumentNullException"><c>remoteEP</c> is null.</exception>
        private byte[] GetEndPointBytes(IPEndPoint remoteEP)
        {
            if (remoteEP == null)
                throw new ArgumentNullException();
            byte[] connect = new byte[9 + Username.Length];
            connect[0] = 4;
            connect[1] = 1;
            Array.Copy(PortToBytes(remoteEP.Port), 0, connect, 2, 2);
            Array.Copy(AddressToBytes(remoteEP.Address.Address), 0, connect, 4, 4);
            Array.Copy(Encoding.ASCII.GetBytes(Username), 0, connect, 8, Username.Length);
            connect[8 + Username.Length] = 0;
            return connect;
        }
        /// <summary>
        /// Starts negotiating with the SOCKS server.
        /// </summary>
        /// <param name="host">The host to connect to.</param>
        /// <param name="port">The port to connect to.</param>
        /// <exception cref="ArgumentNullException"><c>host</c> is null.</exception>
        /// <exception cref="ArgumentException"><c>port</c> is invalid.</exception>
        /// <exception cref="ProxyException">The proxy rejected the request.</exception>
        /// <exception cref="SocketException">An operating system error occurs while accessing the Socket.</exception>
        /// <exception cref="ObjectDisposedException">The Socket has been closed.</exception>
        public override void Negotiate(string host, int port)
        {
            Negotiate(GetHostPortBytes(host, port));
        }
        /// <summary>
        /// Starts negotiating with the SOCKS server.
        /// </summary>
        /// <param name="remoteEP">The IPEndPoint to connect to.</param>
        /// <exception cref="ArgumentNullException"><c>remoteEP</c> is null.</exception>
        /// <exception cref="ProxyException">The proxy rejected the request.</exception>
        /// <exception cref="SocketException">An operating system error occurs while accessing the Socket.</exception>
        /// <exception cref="ObjectDisposedException">The Socket has been closed.</exception>
        public override void Negotiate(IPEndPoint remoteEP)
        {
            Negotiate(GetEndPointBytes(remoteEP));
        }
        /// <summary>
        /// Starts negotiating with the SOCKS server.
        /// </summary>
        /// <param name="connect">The bytes to send when trying to authenticate.</param>
        /// <exception cref="ArgumentNullException"><c>connect</c> is null.</exception>
        /// <exception cref="ArgumentException"><c>connect</c> is too small.</exception>
        /// <exception cref="ProxyException">The proxy rejected the request.</exception>
        /// <exception cref="SocketException">An operating system error occurs while accessing the Socket.</exception>
        /// <exception cref="ObjectDisposedException">The Socket has been closed.</exception>
        private void Negotiate(byte[] connect)
        {
            if (connect == null)
                throw new ArgumentNullException();
            if (connect.Length < 2)
                throw new ArgumentException();
            Server.Send(connect);
            byte[] buffer = ReadBytes(8);
            if (buffer[1] != 90)
            {
                Server.Close();
                ThrowSocksException(SocksProxyExceptionStatus.Socks4Failure);
            }
        }


    }


    internal sealed class Socks5Handler : SocksHandler
    {
        /// <summary>
        /// Initiliazes a new Socks5Handler instance.
        /// </summary>
        /// <param name="server">The socket connection with the proxy server.</param>
        /// <exception cref="ArgumentNullException"><c>server</c>  is null.</exception>
        public Socks5Handler(TcpSocket server) : this(server, "") { }
        /// <summary>
        /// Initiliazes a new Socks5Handler instance.
        /// </summary>
        /// <param name="server">The socket connection with the proxy server.</param>
        /// <param name="user">The username to use.</param>
        /// <exception cref="ArgumentNullException"><c>server</c> -or- <c>user</c> is null.</exception>
        public Socks5Handler(TcpSocket server, string user) : this(server, user, "") { }
        /// <summary>
        /// Initiliazes a new Socks5Handler instance.
        /// </summary>
        /// <param name="server">The socket connection with the proxy server.</param>
        /// <param name="user">The username to use.</param>
        /// <param name="pass">The password to use.</param>
        /// <exception cref="ArgumentNullException"><c>server</c> -or- <c>user</c> -or- <c>pass</c> is null.</exception>
        public Socks5Handler(TcpSocket server, string user, string pass)
            : base(server, user)
        {
            Password = pass;
        }
        /// <summary>
        /// Starts the synchronous authentication process.
        /// </summary>
        /// <exception cref="ProxyException">Authentication with the proxy server failed.</exception>
        /// <exception cref="ProtocolViolationException">The proxy server uses an invalid protocol.</exception>
        /// <exception cref="SocketException">An operating system error occurs while accessing the Socket.</exception>
        /// <exception cref="ObjectDisposedException">The Socket has been closed.</exception>
        private void Authenticate()
        {
            if (!string.IsNullOrEmpty(Username) && !string.IsNullOrEmpty(Password))
                Server.Send(new byte[] { 5, 2, 0, 2 });
            else Server.Send(new byte[] { 5, 1, 0 });

            byte[] buffer = ReadBytes(2);
            if (buffer[1] == 255)
                ThrowSocksException(SocksProxyExceptionStatus.NoMethodsSupported);

            AuthMethod authenticate = null;
            switch (buffer[1])
            {
                case 0:
                    authenticate = new AuthNone(Server);
                    break;
                case 2:
                    if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
                        ThrowSocksException(SocksProxyExceptionStatus.AuthRequired);

                    authenticate = new AuthUserPass(Server, Username, Password);
                    break;
                default:
                    ThrowSocksException(SocksProxyExceptionStatus.NoMethodsSupported);
                    break;
            }
            authenticate.Authenticate();
        }
        /// <summary>
        /// Creates an array of bytes that has to be sent when the user wants to connect to a specific host/port combination.
        /// </summary>
        /// <param name="host">The host to connect to.</param>
        /// <param name="port">The port to connect to.</param>
        /// <returns>An array of bytes that has to be sent when the user wants to connect to a specific host/port combination.</returns>
        /// <exception cref="ArgumentNullException"><c>host</c> is null.</exception>
        /// <exception cref="ArgumentException"><c>port</c> or <c>host</c> is invalid.</exception>
        private byte[] GetHostPortBytes(string host, int port)
        {
            if (host == null)
                throw new ArgumentNullException();
            if (port <= 0 || port > 65535 || host.Length > 255)
                throw new ArgumentException();
            byte[] connect = new byte[7 + host.Length];
            connect[0] = 5;
            connect[1] = 1;
            connect[2] = 0; //reserved
            connect[3] = 3;
            connect[4] = (byte)host.Length;
            Array.Copy(Encoding.ASCII.GetBytes(host), 0, connect, 5, host.Length);
            Array.Copy(PortToBytes(port), 0, connect, host.Length + 5, 2);
            return connect;
        }
        /// <summary>
        /// Creates an array of bytes that has to be sent when the user wants to connect to a specific IPEndPoint.
        /// </summary>
        /// <param name="remoteEP">The IPEndPoint to connect to.</param>
        /// <returns>An array of bytes that has to be sent when the user wants to connect to a specific IPEndPoint.</returns>
        /// <exception cref="ArgumentNullException"><c>remoteEP</c> is null.</exception>
        private byte[] GetEndPointBytes(IPEndPoint remoteEP)
        {
            if (remoteEP == null)
                throw new ArgumentNullException();
            byte[] connect = new byte[10];
            connect[0] = 5;
            connect[1] = 1;
            connect[2] = 0; //reserved
            connect[3] = 1;
            Array.Copy(AddressToBytes(remoteEP.Address.Address), 0, connect, 4, 4);
            Array.Copy(PortToBytes(remoteEP.Port), 0, connect, 8, 2);
            return connect;
        }
        /// <summary>
        /// Starts negotiating with the SOCKS server.
        /// </summary>
        /// <param name="host">The host to connect to.</param>
        /// <param name="port">The port to connect to.</param>
        /// <exception cref="ArgumentNullException"><c>host</c> is null.</exception>
        /// <exception cref="ArgumentException"><c>port</c> is invalid.</exception>
        /// <exception cref="ProxyException">The proxy rejected the request.</exception>
        /// <exception cref="SocketException">An operating system error occurs while accessing the Socket.</exception>
        /// <exception cref="ObjectDisposedException">The Socket has been closed.</exception>
        /// <exception cref="ProtocolViolationException">The proxy server uses an invalid protocol.</exception>
        public override void Negotiate(string host, int port)
        {
            Negotiate(GetHostPortBytes(host, port));
        }
        /// <summary>
        /// Starts negotiating with the SOCKS server.
        /// </summary>
        /// <param name="remoteEP">The IPEndPoint to connect to.</param>
        /// <exception cref="ArgumentNullException"><c>remoteEP</c> is null.</exception>
        /// <exception cref="ProxyException">The proxy rejected the request.</exception>
        /// <exception cref="SocketException">An operating system error occurs while accessing the Socket.</exception>
        /// <exception cref="ObjectDisposedException">The Socket has been closed.</exception>
        /// <exception cref="ProtocolViolationException">The proxy server uses an invalid protocol.</exception>
        public override void Negotiate(IPEndPoint remoteEP)
        {
            Negotiate(GetEndPointBytes(remoteEP));
        }
        /// <summary>
        /// Starts negotiating with the SOCKS server.
        /// </summary>
        /// <param name="connect">The bytes to send when trying to authenticate.</param>
        /// <exception cref="ArgumentNullException"><c>connect</c> is null.</exception>
        /// <exception cref="ArgumentException"><c>connect</c> is too small.</exception>
        /// <exception cref="ProxyException">The proxy rejected the request.</exception>
        /// <exception cref="SocketException">An operating system error occurs while accessing the Socket.</exception>
        /// <exception cref="ObjectDisposedException">The Socket has been closed.</exception>
        /// <exception cref="ProtocolViolationException">The proxy server uses an invalid protocol.</exception>
        private void Negotiate(byte[] connect)
        {
            Authenticate();
            Server.Send(connect);
            byte[] buffer = ReadBytes(4);
            if (buffer[1] != 0)
            {
                Server.Close();
                ThrowSocksException(SocksProxyExceptionStatus.NotAccepted);
            }
            switch (buffer[3])
            {
                case 1:
                    buffer = ReadBytes(6); //IPv4 address with port
                    break;
                case 3:
                    buffer = ReadBytes(1);
                    buffer = ReadBytes(buffer[0] + 2); //domain name with port
                    break;
                case 4:
                    buffer = ReadBytes(18); //IPv6 address with port
                    break;
                default:
                    Server.Close();
                    ThrowSocksException(SocksProxyExceptionStatus.UnknownAddressFormat);
                    break;
            }
        }



        /// <summary>
        /// Gets or sets the password to use when authenticating with the SOCKS5 server.
        /// </summary>
        /// <value>The password to use when authenticating with the SOCKS5 server.</value>
        private string Password
        {
            get
            {
                return m_Password;
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException();
                m_Password = value;
            }
        }
        /// <summary>
        /// Gets or sets the bytes to use when sending a connect request to the proxy server.
        /// </summary>
        /// <value>The array of bytes to use when sending a connect request to the proxy server.</value>
        private byte[] HandShake
        {
            get
            {
                return m_HandShake;
            }
            set
            {
                m_HandShake = value;
            }
        }
        // private variables
        /// <summary>Holds the value of the Password property.</summary>
        private string m_Password;
        /// <summary>Holds the value of the HandShake property.</summary>
        private byte[] m_HandShake;
    }
    /// <summary>
    /// References the callback method to be called when the protocol negotiation is completed.
    /// </summary>
    internal delegate void HandShakeComplete(Exception error);
    /// <summary>
    /// Implements a specific version of the SOCKS protocol. This is an abstract class; it must be inherited.
    /// </summary>
    internal abstract class SocksHandler
    {
        protected void ThrowSocksException(SocksProxyExceptionStatus status)
        {
            throw new TcpException("Failed to Connect to Socks proxy: " + m_EndPoint.ToString(),
                                        new SocksProxyException(status));
        }

        IPEndPoint m_EndPoint;

        /// <summary>
        /// Initilizes a new instance of the SocksHandler class.
        /// </summary>
        /// <param name="server">The socket connection with the proxy server.</param>
        /// <param name="user">The username to use when authenticating with the server.</param>
        /// <exception cref="ArgumentNullException"><c>server</c> -or- <c>user</c> is null.</exception>
        public SocksHandler(TcpSocket server, string user)
        {
            Server = server;
            Username = user;
            m_EndPoint = server.RemoteEndPoint;
        }
        /// <summary>
        /// Converts a port number to an array of bytes.
        /// </summary>
        /// <param name="port">The port to convert.</param>
        /// <returns>An array of two bytes that represents the specified port.</returns>
        protected byte[] PortToBytes(int port)
        {
            byte[] ret = new byte[2];
            ret[0] = (byte)(port / 256);
            ret[1] = (byte)(port % 256);
            return ret;
        }
        /// <summary>
        /// Converts an IP address to an array of bytes.
        /// </summary>
        /// <param name="address">The IP address to convert.</param>
        /// <returns>An array of four bytes that represents the specified IP address.</returns>
        protected byte[] AddressToBytes(long address)
        {
            byte[] ret = new byte[4];
            ret[0] = (byte)(address % 256);
            ret[1] = (byte)((address / 256) % 256);
            ret[2] = (byte)((address / 65536) % 256);
            ret[3] = (byte)(address / 16777216);
            return ret;
        }
        /// <summary>
        /// Reads a specified number of bytes from the Server socket.
        /// </summary>
        /// <param name="count">The number of bytes to return.</param>
        /// <returns>An array of bytes.</returns>
        /// <exception cref="ArgumentException">The number of bytes to read is invalid.</exception>
        /// <exception cref="SocketException">An operating system error occurs while accessing the Socket.</exception>
        /// <exception cref="ObjectDisposedException">The Socket has been closed.</exception>
        protected byte[] ReadBytes(int count)
        {
            if (count <= 0)
                throw new ArgumentException();
            byte[] buffer = new byte[count];
            int received = Server.Receive(buffer, 0, count);
            return buffer;
        }
        /// <summary>
        /// Gets or sets the socket connection with the proxy server.
        /// </summary>
        /// <value>A Socket object that represents the connection with the proxy server.</value>
        /// <exception cref="ArgumentNullException">The specified value is null.</exception>
        protected TcpSocket Server
        {
            get
            {
                return m_Server;
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException();
                m_Server = value;
            }
        }
        /// <summary>
        /// Gets or sets the username to use when authenticating with the proxy server.
        /// </summary>
        /// <value>A string that holds the username to use when authenticating with the proxy server.</value>
        /// <exception cref="ArgumentNullException">The specified value is null.</exception>
        protected string Username
        {
            get
            {
                return m_Username;
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException();
                m_Username = value;
            }
        }

        /// <summary>
        /// Gets or sets a byte buffer.
        /// </summary>
        /// <value>An array of bytes.</value>
        protected byte[] Buffer
        {
            get
            {
                return m_Buffer;
            }
            set
            {
                m_Buffer = value;
            }
        }
        /// <summary>
        /// Gets or sets the number of bytes that have been received from the remote proxy server.
        /// </summary>
        /// <value>An integer that holds the number of bytes that have been received from the remote proxy server.</value>
        protected int Received
        {
            get
            {
                return m_Received;
            }
            set
            {
                m_Received = value;
            }
        }
        // private variables
        /// <summary>Holds the value of the Server property.</summary>
        private TcpSocket m_Server;
        /// <summary>Holds the value of the Username property.</summary>
        private string m_Username;
        /// <summary>Holds the value of the Buffer property.</summary>
        private byte[] m_Buffer;
        /// <summary>Holds the value of the Received property.</summary>
        private int m_Received;
        /// <summary>Holds the address of the method to call when the SOCKS protocol has been completed.</summary>
        protected HandShakeComplete ProtocolComplete;
        /// <summary>
        /// Starts negotiating with a SOCKS proxy server.
        /// </summary>
        /// <param name="host">The remote server to connect to.</param>
        /// <param name="port">The remote port to connect to.</param>
        public abstract void Negotiate(string host, int port);
        /// <summary>
        /// Starts negotiating with a SOCKS proxy server.
        /// </summary>
        /// <param name="remoteEP">The remote endpoint to connect to.</param>
        public abstract void Negotiate(IPEndPoint remoteEP);

    }
}
