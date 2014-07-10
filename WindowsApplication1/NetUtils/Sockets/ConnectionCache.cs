using System;
using System.Collections.Generic;
using System.Text;

namespace Fenryr.Net.Sockets
{

    class ConnectionCache
    {      
        public static int MaxConnections = 10;
        public static int MaxAlivePeriod = 15;

        Dictionary<TcpConnection, long> m_Connections = new Dictionary<TcpConnection, long>();

        
        public void Clear()
        {
            lock (m_Connections)
            {
                foreach (KeyValuePair<TcpConnection, long> pair in m_Connections)
                    pair.Key.Close();
                m_Connections.Clear();
            }
        }

        void ClearExpired()
        {
            TcpConnection [] connections = new TcpConnection[m_Connections.Keys.Count];
            m_Connections.Keys.CopyTo(connections ,0);

            long mSecs = Utils.GetTimeStamp();

            foreach ( TcpConnection key in connections)
            {
                if ((mSecs - m_Connections[key] > MaxAlivePeriod * 1000) || !key.Alive)
                {
                    m_Connections.Remove(key);
                }
            }
        }

        public void AddConnection(TcpConnection connection)
        {
            if (MaxConnections == 0)
                return;

            lock (m_Connections)
            {
                ClearExpired();
                if (m_Connections.Count > MaxConnections)
                {
                    TcpConnection [] connections = new TcpConnection[m_Connections.Keys.Count];
                    m_Connections.Keys.CopyTo(connections ,0);
                    m_Connections.Remove(connections[0]);
                }

                m_Connections[connection] = Utils.GetTimeStamp();
            }
        }

        public TcpConnection GetConnection(string Host , int Port , TcpProxy proxy)
        {
            lock (m_Connections)
            {
                ClearExpired();
                TcpConnection toReturn = null;

                    foreach (KeyValuePair<TcpConnection, long> pair in m_Connections)
                    {
                        if (pair.Key.IsMatch(Host, Port, proxy))
                        {
                            toReturn = pair.Key;
                            break;
                        }                    
                    }

                    if (toReturn != null)
                    {
                        m_Connections.Remove(toReturn);
                    }
                return toReturn;
            }
        }


    }
}
