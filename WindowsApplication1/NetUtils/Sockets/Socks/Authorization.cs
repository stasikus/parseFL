using System;
using System.Collections.Generic;
using System.Text;
using System.Net;

namespace Fenryr.Net.Sockets.Socks
{
    internal abstract class AuthMethod
    {
        IPEndPoint m_EndPoint;
        /// <summary>
        /// Initializes an AuthMethod instance.
        /// </summary>
        /// <param name="server">The socket connection with the proxy server.</param>
        public AuthMethod(TcpSocket server)
        {
            Server = server;
            m_EndPoint = server.RemoteEndPoint;
        }

        protected void ThrowSocksException(SocksProxyExceptionStatus status)
        {
            throw new TcpException("Failed to Connect to Socks proxy: " + m_EndPoint.ToString(),
                                        new SocksProxyException(status));
        }

        /// <summary>
        /// Authenticates the user.
        /// </summary>
        /// <exception cref="ProxyException">Authentication with the proxy server failed.</exception>
        /// <exception cref="ProtocolViolationException">The proxy server uses an invalid protocol.</exception>
        /// <exception cref="SocketException">An operating system error occurs while accessing the Socket.</exception>
        /// <exception cref="ObjectDisposedException">The Socket has been closed.</exception>
        public abstract void Authenticate();

        /// <summary>
        /// Gets or sets the socket connection with the proxy server.
        /// </summary>
        /// <value>The socket connection with the proxy server.</value>
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
        /// Gets or sets a byt array that can be used to store data.
        /// </summary>
        /// <value>A byte array to store data.</value>
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
        /// <summary>Holds the value of the Buffer property.</summary>
        private byte[] m_Buffer;
        /// <summary>Holds the value of the Server property.</summary>
        private TcpSocket m_Server;
        /// <summary>Holds the address of the method to call when the proxy has authenticated the client.</summary>
        protected HandShakeComplete CallBack;
        /// <summary>Holds the value of the Received property.</summary>
        private int m_Received;
    }

    internal sealed class AuthNone : AuthMethod
    {
        /// <summary>
        /// Initializes an AuthNone instance.
        /// </summary>
        /// <param name="server">The socket connection with the proxy server.</param>
        public AuthNone(TcpSocket server) : base(server) { }
        /// <summary>
        /// Authenticates the user.
        /// </summary>
        public override void Authenticate()
        {
            return; // Do Nothing
        }

    }

    internal sealed class AuthUserPass : AuthMethod
    {
        /// <summary>
        /// Initializes a new AuthUserPass instance.
        /// </summary>
        /// <param name="server">The socket connection with the proxy server.</param>
        /// <param name="user">The username to use.</param>
        /// <param name="pass">The password to use.</param>
        /// <exception cref="ArgumentNullException"><c>user</c> -or- <c>pass</c> is null.</exception>
        public AuthUserPass(TcpSocket server,  string user, string pass)
            : base(server)
        {
            Username = user;
            Password = pass;
        }
        /// <summary>
        /// Creates an array of bytes that has to be sent if the user wants to authenticate with the username/password authentication scheme.
        /// </summary>
        /// <returns>An array of bytes that has to be sent if the user wants to authenticate with the username/password authentication scheme.</returns>
        private byte[] GetAuthenticationBytes()
        {
            byte[] buffer = new byte[3 + Username.Length + Password.Length];
            buffer[0] = 1;
            buffer[1] = (byte)Username.Length;
            Array.Copy(Encoding.ASCII.GetBytes(Username), 0, buffer, 2, Username.Length);
            buffer[Username.Length + 2] = (byte)Password.Length;
            Array.Copy(Encoding.ASCII.GetBytes(Password), 0, buffer, Username.Length + 3, Password.Length);
            return buffer;
        }
        /// <summary>
        /// Starts the authentication process.
        /// </summary>
        public override void Authenticate()
        {
            Server.Send(GetAuthenticationBytes());
            byte[] buffer = new byte[2];
            int received = Server.Receive(buffer, 0, 2);

            if (buffer[1] != 0)
            {
                Server.Close();
                ThrowSocksException(SocksProxyExceptionStatus.UserPassRejected);                                 
            }
        }


        /// <summary>
        /// Gets or sets the username to use when authenticating with the proxy server.
        /// </summary>
        /// <value>The username to use when authenticating with the proxy server.</value>
        /// <exception cref="ArgumentNullException">The specified value is null.</exception>
        private string Username
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
        /// Gets or sets the password to use when authenticating with the proxy server.
        /// </summary>
        /// <value>The password to use when authenticating with the proxy server.</value>
        /// <exception cref="ArgumentNullException">The specified value is null.</exception>
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
        // private variables
        /// <summary>Holds the value of the Username property.</summary>
        private string m_Username;
        /// <summary>Holds the value of the Password property.</summary>
        private string m_Password;
    }
}
