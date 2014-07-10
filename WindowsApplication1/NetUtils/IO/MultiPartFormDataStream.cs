using System;
using System.Collections.Generic;
using System.Text;

namespace Fenryr.IO
{
    class MultiPartFormDataStream : System.IO.Stream
    {
        private string boundary;
        System.IO.MemoryStream _data = new System.IO.MemoryStream();
		
		Encoding m_Encoding = Encoding.UTF8;
		
		public Encoding TextEncoding {
		    get {return  m_Encoding;}
			set { m_Encoding = value;}
		}

        public string Boundary
        {
            get
            {
                return boundary;
            }
        }

        bool readyToSend = false;

        public MultiPartFormDataStream()
        {
            boundary = "--------" +
                        DateTime.Now.Ticks.ToString();
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return _data.BeginRead(buffer, offset, count, callback, state);
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            throw new InvalidOperationException("WhiteNet.Http.MultiPartFormDataStream.BeginWrite");
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return true; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }
        public override long Length
        {
            get { return _data.Length; }
        }
        public override long Position
        {
            get
            {
                return _data.Position;
            }
            set
            {
                _data.Position = value;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int br =  _data.Read(buffer , offset, count);
            return br;
        }

        public override void Flush()
        {
            _data.Flush();
        }

        public override void SetLength(long value)
        {
            _data.SetLength(value);
        }
        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new InvalidOperationException("WhiteNet.Http.MultiPartFormDataStream.Write");
        }

      


        void WriteStr(string s)
        {
            byte[] bytes = TextEncoding.GetBytes(s);
            _data.Write(bytes, 0, bytes.Length);
        }

        public void AddFormField(string key, string value)
        {
            WriteStr("--" + boundary + "\r\n" +
                        "Content-Disposition: form-data; " +
                         "name=\"" + key + "\"\r\n\r\n" + value + "\r\n");

        }

        public void AddFile(string field_name, string path, string type)
        {
            if (!System.IO.File.Exists(path)) throw new System.IO.FileNotFoundException();
            byte[] bytes = System.IO.File.ReadAllBytes(path);
            AddFile(field_name, bytes, System.IO.Path.GetFileName(path), type);
        }

        public void AddFile(string field_name, byte[] buffer, string fname, string type)
        {

            WriteStr("--" + boundary + "\r\n" +
                       "Content-Disposition: form-data; " +
                        "name=\"" + field_name + "\"; filename=\"" +
                        fname + "\"\r\n" +
                        "Content-Type: " + type + "\r\n\r\n");

            _data.Write(buffer, 0, buffer.Length);
            WriteStr("\r\n");
        }

        public override long Seek(long offset, System.IO.SeekOrigin loc)
        {
            if (!readyToSend && loc == System.IO.SeekOrigin.Begin && Length > 0)
            {
                _data.Seek(0, System.IO.SeekOrigin.End);
                SetEndOfData();
                readyToSend = true;
                _data.Seek(0, System.IO.SeekOrigin.Begin);
            }
           long res =  _data.Seek(offset, loc);
           return res;
        }


        public void AddFileStream(System.IO.Stream fileStream, string field_name, string name, string type)
        {
            WriteStr("--" + boundary + "\r\n" +
                       "Content-Disposition: form-data; " +
                        "name=\"" + field_name + "\"; filename=\"" +
                        name + "\"\r\n" +
                        "Content-Type: " + type + "\r\n\r\n");

            List<byte> lb = new List<byte>();
            int b;
            while ((b = fileStream.ReadByte()) != -1)
            {
                lb.Add((byte)b);
            }
            byte[] fileBytes = lb.ToArray();
            _data.Write(fileBytes, 0, fileBytes.Length);
            WriteStr("\r\n");
        }


        void SetEndOfData()
        {
            WriteStr("--" + boundary + "--\r\n\r\n");
        }

        public override string ToString()
        {
            string res = string.Empty;
            _data.Seek(0, System.IO.SeekOrigin.Begin);
            byte[] bytes = new byte[1024];
            int read = _data.Read(bytes, 0, bytes.Length);
            while (read > 0)
            {
                res += Encoding.Default.GetString(bytes, 0, read);
                read = _data.Read(bytes, 0, bytes.Length);
            }
            _data.Seek(0, System.IO.SeekOrigin.Begin);
            return res;

        }


    }
}
