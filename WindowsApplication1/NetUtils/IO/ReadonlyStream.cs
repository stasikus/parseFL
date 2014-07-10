using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Fenryr.IO
{
    class ReadonlyStream : System.IO.Stream
    {

         Stream _stream;
        bool OwnStream;

        protected Stream InnerStream { get { return _stream; } }

        public ReadonlyStream(Stream InnerStream, bool ownsStream)
        {
            _stream = InnerStream;
            this.OwnStream = ownsStream; 
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new InvalidOperationException("ReadonlyStream.Write");
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int r =  _stream.Read(buffer, offset, count);
            bool bCanRead = _stream.CanRead;
            return r;
        }

        public override void Close()
        {
            if (OwnStream)
                _stream.Close();
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return _stream.BeginRead(buffer, offset, count, callback, state);
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            throw new InvalidOperationException("ReadonlyStream.BeginWrite");
        }

        public override bool CanSeek
        {
            get { return false; }
        }
        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanWrite
        {
            get { return true; }
        }

        public override long Length
        {
           get
           {
               return _stream.Length;
           }
        }


        public override long Position
        {
            set
            {
                _stream.Position = value;
            }
            get
            {
                return _stream.Position;
            }
        }

        public override void Flush()
        {
           
        }

        public override void SetLength(long value)
        {
            
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _stream.Seek(offset, origin);
        }

    }
}
