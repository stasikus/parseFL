using System;
using System.Collections.Generic;
using System.Text;
using System.Net;

namespace Fenryr.Net.Sockets.Socks
{
    public class ProxySocket : TcpSocket
    {
        /// <summary>
        /// Initializes a new instance of the ProxySocket class.
        /// </summary>
        /// <param name="addressFamily">One of the AddressFamily values.</param>
        /// <param name="socketType">One of the SocketType values.</param>
        /// <param name="protocolType">One of the ProtocolType values.</param>
        /// <exception cref="SocketException">The combination of addressFamily, socketType, and protocolType results in an invalid socket.</exception>
        public ProxySocket() : this("") { }
        /// <summary>
        /// Initializes a new instance of the ProxySocket class.
        /// </summary>
        /// <param name="addressFamily">One of the AddressFamily values.</param>
        /// <param name="socketType">One of the SocketType values.</param>
        /// <param name="protocolType">One of the ProtocolType values.</param>
        /// <param name="proxyUsername">The username to use when authenticating with the proxy server.</param>
        /// <exception cref="SocketException">The combination of addressFamily, socketType, and protocolType results in an invalid socket.</exception>
        /// <exception cref="ArgumentNullException"><c>proxyUsername</c> is null.</exception>
        public ProxySocket(string proxyUsername) : this(proxyUsername, "") { }
        /// <summary>
        /// Initializes a new instance of the ProxySocket class.
        /// </summary>
        /// <param name="addressFamily">One of the AddressFamily values.</param>
        /// <param name="socketType">One of the SocketType values.</param>
        /// <param name="protocolType">One of the ProtocolType values.</param>
        /// <param name="proxyUsername">The username to use when authenticating with the proxy server.</param>
        /// <param name="proxyPassword">The password to use when authenticating with the proxy server.</param>
        /// <exception cref="SocketException">The combination of addressFamily, socketType, and protocolType results in an invalid socket.</exception>
        /// <exception cref="ArgumentNullException"><c>proxyUsername</c> -or- <c>proxyPassword</c> is null.</exception>
        public ProxySocket(string proxyUsername, string proxyPassword)
        {
            ProxyUser = proxyUsername;
            ProxyPass = proxyPassword;
            ToThrow = new InvalidOperationException();
        }
        /// <summary>
        /// Establishes a connection to a remote device.
        /// </summary>
        /// <param name="remoteEP">An EndPoint that represents the remote device.</param>
        /// <exception cref="ArgumentNullException">The remoteEP parameter is a null reference (Nothing in Visual Basic).</exception>
        /// <exception cref="SocketException">An operating system error occurs while accessing the Socket.</exception>
        /// <exception cref="ObjectDisposedException">The Socket has been closed.</exception>
        /// <exception cref="ProxyException">An error occured while talking to the proxy server.</exception>
        public new void Connect(IPEndPoint remoteEP)
        {
            if (remoteEP == null)
                throw new ArgumentNullException("<remoteEP> cannot be null.");
            if (ProxyType == ProxyTypes.None || ProxyEndPoint == null)
                base.Connect(remoteEP);
            else
            {
                base.Connect(ProxyEndPoint);
                if (ProxyType == ProxyTypes.Socks4)
                    (new Socks4Handler(this, ProxyUser)).Negotiate((IPEndPoint)remoteEP);
                else if (ProxyType == ProxyTypes.Socks5)
                    (new Socks5Handler(this, ProxyUser, ProxyPass)).Negotiate((IPEndPoint)remoteEP);
            }
        }
        /// <summary>
        /// Establishes a connection to a remote device.
        /// </summary>
        /// <param name="host">The remote host to connect to.</param>
        /// <param name="port">The remote port to connect to.</param>
        /// <exception cref="ArgumentNullException">The host parameter is a null reference (Nothing in Visual Basic).</exception>
        /// <exception cref="ArgumentException">The port parameter is invalid.</exception>
        /// <exception cref="SocketException">An operating system error occurs while accessing the Socket.</exception>
        /// <exception cref="ObjectDisposedException">The Socket has been closed.</exception>
        /// <exception cref="ProxyException">An error occured while talking to the proxy server.</exception>
        /// <remarks>If you use this method with a SOCKS4 server, it will let the server resolve the hostname. Not all SOCKS4 servers support this 'remote DNS' though.</remarks>
        public new void Connect(string host, int port)
        {
            if (host == null)
                throw new ArgumentNullException("<host> cannot be null.");
            if (port <= 0 || port > 65535)
                throw new ArgumentException("Invalid port.");
            if (ProxyType == ProxyTypes.None || ProxyEndPoint == null)
                base.Connect(new IPEndPoint(Dns.Resolve(host).AddressList[0], port));
            else
            {
                base.Connect(ProxyEndPoint);
                if (ProxyType == ProxyTypes.Socks4)
                    (new Socks4Handler(this, ProxyUser)).Negotiate(host, port);
                else if (ProxyType == ProxyTypes.Socks5)
                    (new Socks5Handler(this, ProxyUser, ProxyPass)).Negotiate(host, port);
            }
        }





        /// <summary>
        /// Gets or sets the EndPoint of the proxy server.
        /// </summary>
        /// <value>An IPEndPoint object that holds the IP address and the port of the proxy server.</value>
        public IPEndPoint ProxyEndPoint
        {
            get
            {
                return m_ProxyEndPoint;
            }
            set
            {
                m_ProxyEndPoint = value;
            }
        }
        /// <summary>
        /// Gets or sets the type of proxy server to use.
        /// </summary>
        /// <value>One of the ProxyTypes values.</value>
        public ProxyTypes ProxyType
        {
            get
            {
                return m_ProxyType;
            }
            set
            {
                m_ProxyType = value;
            }
        }
        /// <summary>
        /// Gets or sets a user-defined object.
        /// </summary>
        /// <value>The user-defined object.</value>
        private object State
        {
            get
            {
                return m_State;
            }
            set
            {
                m_State = value;
            }
        }
        /// <summary>
        /// Gets or sets the username to use when authenticating with the proxy.
        /// </summary>
        /// <value>A string that holds the username that's used when authenticating with the proxy.</value>
        /// <exception cref="ArgumentNullException">The specified value is null.</exception>
        public string ProxyUser
        {
            get
            {
                return m_ProxyUser;
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException();
                m_ProxyUser = value;
            }
        }
        /// <summary>
        /// Gets or sets the password to use when authenticating with the proxy.
        /// </summary>
        /// <value>A string that holds the password that's used when authenticating with the proxy.</value>
        /// <exception cref="ArgumentNullException">The specified value is null.</exception>
        public string ProxyPass
        {
            get
            {
                return m_ProxyPass;
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException();
                m_ProxyPass = value;
            }
        }
       
        /// <summary>
        /// Gets or sets the exception to throw when the EndConnect method is called.
        /// </summary>
        /// <value>An instance of the Exception class (or subclasses of Exception).</value>
        private Exception ToThrow
        {
            get
            {
                return m_ToThrow;
            }
            set
            {
                m_ToThrow = value;
            }
        }
        /// <summary>
        /// Gets or sets the remote port the user wants to connect to.
        /// </summary>
        /// <value>An integer that specifies the port the user wants to connect to.</value>
        private int RemotePort
        {
            get
            {
                return m_RemotePort;
            }
            set
            {
                m_RemotePort = value;
            }
        }
        // private variables
        /// <summary>Holds the value of the State property.</summary>
        private object m_State;
        /// <summary>Holds the value of the ProxyEndPoint property.</summary>
        private IPEndPoint m_ProxyEndPoint = null;
        /// <summary>Holds the value of the ProxyType property.</summary>
        private ProxyTypes m_ProxyType = ProxyTypes.None;
        /// <summary>Holds the value of the ProxyUser property.</summary>
        private string m_ProxyUser = null;
        /// <summary>Holds the value of the ProxyPass property.</summary>
        private string m_ProxyPass = null;
        /// <summary>Holds a pointer to the method that should be called when the Socket is connected to the remote device.</summary>
        private AsyncCallback CallBack = null;
        /// <summary>Holds the value of the AsyncResult property.</summary>
        /// <summary>Holds the value of the ToThrow property.</summary>
        private Exception m_ToThrow = null;
        /// <summary>Holds the value of the RemotePort property.</summary>
        private int m_RemotePort;
    }
}
