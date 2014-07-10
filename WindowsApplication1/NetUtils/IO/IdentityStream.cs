using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Fenryr.IO
{
    class IdentityStream : ReadonlyStream
    {
        long Size;
        long DataPosition = 0;

        public IdentityStream(Stream stream, bool ownsStream, long size)
            : base(stream, ownsStream)
        {
            Size = size;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (DataPosition >= Size) return 0;
            int toRead = (int)Math.Min(count, Size - DataPosition);
            int read = InnerStream.Read(buffer , offset , toRead);
            if (read == 0)
                throw new Fenryr.Net.Sockets.TcpException("Data receive timeout", new System.Net.Sockets.SocketException((int)System.Net.Sockets.SocketError.TimedOut));

            bool bCanRead = InnerStream.CanRead;
            DataPosition += read;
            return read;
        }
    }
}
