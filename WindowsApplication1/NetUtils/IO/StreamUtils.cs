using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.IO.Compression;

namespace Fenryr.IO
{

public enum WorkMode { Read, Write }

    public class DownloadEventArgs : EventArgs
    {
        public readonly long bytesRead, bytesAll;
        public readonly string what;
        public bool IsInterrupted = false;
        public readonly WorkMode mode = WorkMode.Read;

        public DownloadEventArgs(long br, long ba, WorkMode wm , string what)
        {
            bytesRead = br;
            bytesAll = ba;
            this.what = what;
            mode = wm;
        }
    }


    public delegate void StreamProgressEventHandler(object sender, DownloadEventArgs e); 
    public delegate void StreamProgressHandler (long bytesCurr, long bytesAll ,WorkMode mode ,  ref bool Interrupt,  string info);
    
    public delegate byte []  BytesConvertor ( byte [] srcBytes, int Len);

    public static class BytesConvertors
    {
        public static byte[] SimpleConvertor(byte[] srcBytes, int Len)
        {
            byte[] result = new byte[Len];
            Array.Copy(srcBytes , result , Len);
            return result;
        }

        public static byte[] GZipConvertor(byte[] srcBytes, int Len)
        {
            MemoryStream ms = new MemoryStream();
            ms.Write(srcBytes, 0, Len);
            ms.Seek(0, SeekOrigin.Begin);
            GZipStream gzs = new GZipStream(ms, CompressionMode.Decompress);
            byte[] data = new byte[Len];
           
            int offset = 0;
            int count = Len;
            while (count > 0)
            {
                int delta = gzs.Read(data, offset, count);
                offset += delta;
                count -= delta;
            }
            gzs.Close();
            return data;
        }


        public static byte[] DeflateConvertor(byte[] srcBytes, int Len)
        {
            MemoryStream ms = new MemoryStream();
            ms.Write(srcBytes, 0, Len);
            ms.Seek(0, SeekOrigin.Begin);
            DeflateStream dfs = new DeflateStream(ms, CompressionMode.Decompress);
            byte [] data = new byte [Len];
            int offset = 0;
            int count = Len;
            while (count > 0)
           {
                int delta = dfs.Read(data, offset, count);
                offset += delta;
                count -= delta;
            }
            dfs.Close();
            return data;
        }

    }
	
    class StreamUtils
    {
        public static string ReadLine(System.IO.Stream stream)
        {
            return ReadLineValidating(stream , "",null);
        }

        public static string ReadLineValidating(System.IO.Stream stream, string StartsWith , Exception toThrow)
        {
            byte[] buffer = new byte[1024];
            byte[] buff = new byte[2];
            int count = 0;
            while (true)
            {

                bool bCanRead = stream.CanRead;

                if (count >= buffer.Length)
                    Array.Resize(ref buffer, 2 * buffer.Length);

                int bytes = stream.Read(buff, 0, 1);
                if (bytes == 1 && buff[0] != '\n' && buff[0] != '\r')
                {
                    if (count < StartsWith.Length && StartsWith[count] != (char)buff[0])
                        throw toThrow;
                    buffer[count++] = buff[0];
                }
                else if (buff[0] == '\n' || bytes == 0)
                {
                    break;
                }
            }
            if (buffer.Length == 0)
            {
                return "";
            }
            return Encoding.Default.GetString(buffer, 0, count);
        }


        public static void WriteLine(System.IO.Stream stream, string line)
        {
            byte[] data = Encoding.Default.GetBytes(line+"\r\n");
            stream.Write(data, 0, data.Length);
        }
		
		public static void CopyStream(Stream srcStream, Stream destStream)
        {          
            byte[] buffer = new byte[1024];
            bool bCanRead = srcStream.CanRead;
            int read = srcStream.Read(buffer, 0, 1024);
            while (read > 0)
            {
                bCanRead = srcStream.CanRead;
                destStream.Write(buffer, 0, read);
                read = srcStream.Read(buffer, 0, 1024);
            }
        }
		
		
		public static void StreamToStream(Stream srcStream, Stream destStream, long len, StreamProgressHandler OnProgress, WorkMode mode, out bool Aborted, int DownloadStep)
        {
            byte[] buffer = new byte[DownloadStep];
            int allRead = 0;
            while (allRead < len)
            {             
                int read = srcStream.Read(buffer, 0, (int) Math.Min(buffer.Length, len - allRead));
                if (read == 0 ) break;

                allRead += read;
                destStream.Write(buffer, 0, read);
                bool b = destStream.CanRead;
                bool Interrupt = false;
                if (OnProgress != null)
                    OnProgress((long)allRead, len, mode , ref Interrupt,String.Empty);
                System.Threading.Thread.Sleep(50);
                if (Interrupt == true)
                {
                    destStream.Close();
                    Aborted = true;
                    return;
                }

            }
            Aborted = false;
        }



        public static void StreamToStreamWhenLengthUnknown(Stream srcStream, Stream destStream, StreamProgressHandler OnProgress, WorkMode mode, out bool Aborted, int DownloadStep, bool bChunked)
        {
            byte[] buffer = new byte[DownloadStep];
            int allRead = 0;
            int read = srcStream.Read(buffer, 0, buffer.Length);
            while (read > 0)
            {
                allRead += read;
                destStream.Write(buffer, 0, read);
                bool Interrupt = false;
                
                if (OnProgress != null)
                    OnProgress((long)allRead, (long)allRead, mode, ref Interrupt, string.Empty);

                if (Interrupt == true)
                {
                    destStream.Close();
                    Aborted = true;
                    return;
                }
                System.Threading.Thread.Sleep(50);
                read = srcStream.Read(buffer, 0, buffer.Length);
            }
            Aborted = false;
        }


    }
}
