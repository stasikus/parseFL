using System;
using System.Collections.Generic;
using System.Text;

namespace Fenryr.Net.Sockets.Socks
{
    public enum SocksProxyExceptionStatus
    {
        AuthRequired,
        UserPassRejected,
        NoMethodsSupported,
        InvalidAuthMethod,
        NotAccepted,
        UnknownAddressFormat,
        Socks4Failure
    }

    public class SocksProxyException : Exception
    {
       static string TranslateErr(SocksProxyExceptionStatus status)
        {
           switch (status) {
               case SocksProxyExceptionStatus.AuthRequired :
                return "Auth Required for socks proxy. Specify username and password";
          
               case SocksProxyExceptionStatus.UserPassRejected:
                return "Invalid Credentials";

            case SocksProxyExceptionStatus.NoMethodsSupported:
                return "Socks5 Server does not support any of auth methods";

            case SocksProxyExceptionStatus.NotAccepted:
                return "Socks5 Server did not accept connection";

            case SocksProxyExceptionStatus.InvalidAuthMethod:
                return "Socks5 Server required auth method that could not be understood";

            case SocksProxyExceptionStatus.UnknownAddressFormat:
                return "Socks5 Server return an Address format that could not be understood";

            case SocksProxyExceptionStatus.Socks4Failure:
                return "Socks4 Server did not allow connection";
               default:
                return "";
       }
        }

        public SocksProxyException(SocksProxyExceptionStatus status) :
            base(TranslateErr(status))
        {

        }

    }

}
