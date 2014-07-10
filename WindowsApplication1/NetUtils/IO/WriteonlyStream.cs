using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Fenryr.IO
{
    class WriteonlyStream : Stream
    {

        Stream _innerStream;
        bool OwnStream;

        public WriteonlyStream(Stream InnerStream, bool ownsStream)
        {
            _innerStream = InnerStream;
            this.OwnStream = ownsStream; 
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _innerStream.Write(buffer , offset, count);
            _innerStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new InvalidOperationException("WriteonlyStream.Read");
        }

        public override void Close()
        {
            if (OwnStream)
                _innerStream.Close();
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            throw new InvalidOperationException("WriteonlyStream.BeginRead");
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return _innerStream.BeginWrite(buffer, offset, count, callback, state);
        }

        public override bool CanSeek
        {
            get { return false; }
        }
        public override bool CanRead
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return true; }
        }

        public override long Length
        {
            get { return _innerStream.Length; }
        }


        public override long Position
        {
            get
            {
                return _innerStream.Position;
            }
            set
            {
                _innerStream.Position = value;
            }
        }

        public override void Flush()
        {
            _innerStream.Flush();
        }

        public override void SetLength(long value)
        {
            
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _innerStream.Seek(offset, origin);
        }

    }
}
