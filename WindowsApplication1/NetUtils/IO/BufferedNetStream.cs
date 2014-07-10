using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;
using Fenryr.Net.Sockets;
using System.Net.Sockets;

namespace Fenryr.IO
{

    

    class BufferedNetStream : Stream
    {
        byte[] dataBuffer  = new byte[0];
        const int DEFAULT_BUFF_SIZE = 1024;

        int dataOffset = 0;

        TcpSocket m_Socket;
        bool m_ownsSocket = false;

        public BufferedNetStream(TcpSocket sock)
            : this (sock, true)
        {
        }

        public BufferedNetStream(TcpSocket sock, bool ownsSocket)
        {
            m_Socket = sock;
            m_ownsSocket = ownsSocket;
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return base.BeginRead(buffer, offset, count, callback, state);
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return base.BeginWrite(buffer, offset, count, callback, state);
        }

        public override bool CanRead
        {
            get { 
                if (lastExcept == null) return true;
                throw lastExcept;
            }
        }

        public override bool CanWrite
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override void Flush()
        {
          
        }

        public override long Length
        {
            get { return dataBuffer.LongLength; }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return 0;
        }

        public override void SetLength(long value)
        {
            
        }

        public override void Close()
        {
            if (m_ownsSocket)
                m_Socket.Close();
        }
        public override long Position
        {
            get
            {
                return dataOffset;
            }
            set
            {
                
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            try
            {
                m_Socket.Send(buffer, offset, count);
            }
            catch (Exception ex)
            {
                lastExcept = ex;
            }
        }

        Exception lastExcept = null;

       public Exception LastException
        {
            get
            {
                return lastExcept;
            }
        }

        public override int Read(byte[] buffer, int offset, int size)
        {
            lastExcept = null;
            if (dataBuffer == null) 
                return 0;
            int copied = Math.Min(size, dataBuffer.Length - dataOffset);
                Array.Copy(dataBuffer, dataOffset, buffer, offset, copied);
                offset += copied;
                dataOffset += copied;

                while (copied < size)
                {
                    dataBuffer = m_Socket.ReceivePacket();
                    
                    if (m_Socket.LastException != null)
                    {
                        string str = m_Socket.LastException.StackTrace;
                        lastExcept = m_Socket.LastException;
                        return copied;
                    }

                    if (dataBuffer == null)
                        return copied;

                    dataOffset = 0;
                    if (dataBuffer.Length == 0) break;                  
                    int nCopied = Math.Min(size - copied , dataBuffer.Length - dataOffset);
                    Array.Copy(dataBuffer, 0, buffer, offset , nCopied);
                    copied += nCopied;
                    offset += nCopied;
                    dataOffset += nCopied;
                }
                return copied;  
        }
    }
}
