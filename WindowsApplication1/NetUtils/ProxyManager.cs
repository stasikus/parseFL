using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.IO;

namespace Fenryr
{

    public class ProxyItem
    {
        public static int MaxProxyRefuses = 1000;
        public static int MaxProxyResets = 1000;
        public static int MaxProxyConnectionAborts = 1000;
        public static int MaxProxyConnectionTimeouts = 300;


        bool isUnvalid = false;
        
        int m_ProxyRefuses = 0;
        int m_ProxyResets = 0;
        int m_ProxyConnectionAborts = 0;
        int m_ProxyConnectionTimeouts = 0;

        public  void Reset()
        {
             m_ProxyRefuses = 0;
             m_ProxyResets = 0;
             m_ProxyConnectionAborts = 0;
             m_ProxyConnectionTimeouts = 0;
        }


        public int ProxyRefuses {get { return m_ProxyRefuses; } }
        public int ProxyResets {get  {return m_ProxyResets ; } }
        public int ProxyConnectionAborts {get { return m_ProxyConnectionAborts ; } }
        public int ProxyConnectionTimeouts {get {return  m_ProxyConnectionTimeouts ; } }

        TcpProxy m_Proxy;

        public ProxyItem(TcpProxy proxy)
        {
            m_Proxy = proxy;
        }

        public TcpProxy Proxy
        {
            get { return m_Proxy; }
        }

        public void SetSocketError(SocketError socketError)
        {
            if (socketError == SocketError.ConnectionAborted && ++m_ProxyConnectionAborts > MaxProxyConnectionAborts)
                isUnvalid = true;
            else if (socketError == SocketError.ConnectionReset && ++m_ProxyResets > MaxProxyResets)
                isUnvalid = true;
            else if (socketError == SocketError.ConnectionRefused && ++m_ProxyRefuses > MaxProxyRefuses)
                isUnvalid = true;
            else if (socketError == SocketError.TimedOut && ++m_ProxyConnectionTimeouts > MaxProxyConnectionTimeouts)
                isUnvalid = true;
            else isUnvalid = true;
        }

        public bool IsUnvalid
        {
            get { return isUnvalid; }
        }


    }


    
    public class ProxyManager
   {

       Hashtable m_Proxies = new Hashtable();
       List<Object> m_Keys = new List<Object>();

        public string[] GetProxies()
        {
            lock (m_Keys)
            {
                string [] data = new string[m_Proxies.Count];
                for (int i = 0; i < data.Length; i++)
                {
                    data[i] = (string)m_Keys[i];
                }
                return data;
            }
        }


        int cursor = 0;

        public void Reset()
        {
            cursor = 0;
        }


        public event Action<object> ProxiesChanged;
        public event Action<TcpProxy> ProxyRemoved;

        void ProxiesChangedNotify()
        {
            if (ProxiesChanged != null)
                ProxiesChanged(this);
        }

        void ProxyRemovedNotify(TcpProxy proxy)
        {
            if (ProxyRemoved != null)
                ProxyRemoved(proxy);
        }

        public Hashtable Proxies {
            get {
               return m_Proxies;
            }
        }

       public void RemoveProxy(TcpProxy proxy)
       {
           if (proxy != null)
           {
               lock (m_Proxies)
               {
                   string key = proxy.Host + ":" + proxy.Port.ToString();
                   
                   if (m_Proxies.ContainsKey(key)) {
                       m_Proxies.Remove(key);
                       m_Keys.Remove(key);

                       ProxiesChangedNotify();
                       ProxyRemovedNotify(proxy);
                   }
               }
           }
       }

        public void AddProxy(string strProxy, ProxyTypes pType)
        {
            AddProxy(StrToProxy(strProxy, pType));
        }

        public void AddProxy(TcpProxy proxy)
        {
            lock (m_Proxies)
            {
                if (proxy != null)
                {
                    string Key = proxy.Host + ":" + proxy.Port.ToString();
                    if (!m_Proxies.ContainsKey(Key))
                    {
                        m_Keys.Add(Key);
                    }
                    m_Proxies[Key] = proxy;
                }
            }
        }


        public void ErrorUsingProxy(TcpProxy proxy, SocketError socketError)
        {
            if (proxy != null)
            {
                lock (m_Proxies)
                {
                    string key = proxy.Host + ":" + proxy.Port.ToString();

                    if (m_Proxies.ContainsKey(key))
                    {
                        ((ProxyItem)m_Proxies[key]).SetSocketError(socketError);
                        if (((ProxyItem)m_Proxies[key]).IsUnvalid)
                        {
                            m_Proxies.Remove(key);
                            m_Keys.Remove(key);
                            ProxiesChangedNotify();
                            ProxyRemovedNotify(proxy);
                        }
                    }
                }
            }
        }

        public void ProxyWorked(TcpProxy proxy)
        {
            if (proxy != null)
            {
                lock (m_Proxies)
                {
                    string key = proxy.Host + ":" + proxy.Port.ToString();

                    if (m_Proxies.ContainsKey(key))
                    {
                        ((ProxyItem)m_Proxies[key]).Reset();
                    }
                }
            }
        }

        public static TcpProxy StrToProxy(string str, ProxyTypes pType)
        {
            Match m = Regex.Match(str, @"^(?<host>[\w_\-\.]+)[;:](?<port>\d+)(\S(?<user>[^;:]+)[;:](?<pass>[^;:]+))?$");
            string Host = m.Groups["host"].Value;
            string Port = m.Groups["port"].Value;
            string User = m.Groups["user"].Value;
            string Pass = m.Groups["pass"].Value;

            int iPort = 0;
            if (Host == "" || !int.TryParse(Port, out iPort))
                return null;
            TcpProxy proxy = new TcpProxy(Host, iPort , pType);
            if (User != "")
            {
                proxy.Credentials = new System.Net.NetworkCredential(User, Pass);
            }
            return proxy;
        }



        public void LoadProxiesFromText(string Text, Fenryr.ProxyTypes pType)
        {
            lock (m_Proxies)
            {
                m_Keys.Clear();

                // maybe it should be detected
                if (pType == ProxyTypes.None)
                {
                  //  m_Proxies.Clear();
                  //  return;
                }
              
                string[] lines = Text.Split(new char[] { '\r', '\n'},
                     StringSplitOptions.RemoveEmptyEntries);
                foreach (string line in lines)
                {
                    string proxyLine = line.Trim();
                    TcpProxy proxy = StrToProxy(proxyLine, pType);
                    if (proxy == null)
                        continue;
                    string Key = proxy.Host + ":" + proxy.Port.ToString();
                    m_Proxies[Key] = new ProxyItem(proxy);
                   // m_Keys.Add(Key);
                }

                object[] keys = new object[m_Proxies.Count];
                m_Proxies.Keys.CopyTo(keys , 0);
                m_Keys = new List<object>(keys);
     
                ProxiesChangedNotify();
            } // lock
        }


        public void Clear()
        {
            m_Keys.Clear();
            m_Proxies.Clear();
            cursor = 0;
        }


      public TcpProxy NextProxy()
       {
           lock (m_Proxies)
           {
               if (m_Proxies.Count == 0)
                   return null;
               if (cursor >= m_Keys.Count)
                   cursor = 0;
               return ((ProxyItem)m_Proxies[m_Keys[cursor++]]).Proxy;
           }
       }

   }
}
