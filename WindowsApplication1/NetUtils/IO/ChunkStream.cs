using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Fenryr.IO
{
    class ChunkStream : ReadonlyStream
    {
        byte[] dataBuffer = new byte[0];
        const int DEFAULT_BUFF_SIZE = 1024;

        int dataOffset = 0;
        bool areAchunks = true;

         public ChunkStream(Stream stream, bool ownsStream)
            : base(stream, ownsStream)
        {
           
        }

        

        public override int Read(byte[] buffer, int offset, int size)
        {
            int bytesRead = 0;
            if (!areAchunks)
                return 0;
            bool bCanRead = false;

            while (bytesRead != size)
            {
                if (dataOffset >= dataBuffer.Length)
                {
                    dataOffset = 0;
                  
                    string line = StreamUtils.ReadLine(InnerStream);
                    string prevLine = line;
                    bCanRead = InnerStream.CanRead;
                    int len = 0;
                    bool bOk = false;

                    if (String.IsNullOrEmpty(line))
                    {
                        Array.Resize(ref dataBuffer, 0);
                        continue;
                    }                
                    else if (Int32.TryParse(line, System.Globalization.NumberStyles.HexNumber, null, out len))
                    {
                        Array.Resize(ref dataBuffer, len);
                        bOk =  true;
                    }
                    else 
                    {
                        int pos = line.IndexOf(';');
                        if (pos == -1 ||  !Int32.TryParse(line.Substring(0, pos), System.Globalization.NumberStyles.HexNumber, null, out len))
                          Array.Resize(ref dataBuffer, 0); 
                        else {
                            Array.Resize(ref dataBuffer, len);
                            bOk =  true;
                        }
                    }

                    if (bOk)
                    {
                        if (len == 0)
                        {
                            line = StreamUtils.ReadLine(InnerStream);
                            bCanRead = InnerStream.CanRead;
                            areAchunks = false;
                        }
                        else
                        {
                            int read = 0;
                            while (read != dataBuffer.Length)
                            {
                                read += InnerStream.Read(dataBuffer, read, dataBuffer.Length - read);
                                bCanRead = InnerStream.CanRead;
                            }
                        }
                    }                   
                }
                if (dataBuffer.Length == 0)
                    return bytesRead;
                int bytesToCopy = Math.Min(size - bytesRead -offset, dataBuffer.Length - dataOffset);
                Array.Copy(dataBuffer, dataOffset, buffer, offset + bytesRead, bytesToCopy);
                bytesRead += bytesToCopy;
                dataOffset += bytesToCopy;
            }
            return bytesRead;
        }
    }
}
