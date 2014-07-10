
#define _DEBUG_
#undef _DEBUG_

using System;
using System.Collections.Generic;
using System.Text;

namespace Fenryr
{
     static class Tracer
    {
         static object locker = new object();
		 
		 public static void Init()
         {
         #if _DEBUG_
             using (System.IO.StreamWriter wr = new System.IO.StreamWriter("log.txt"))
             {

             }
          #endif
         }
         
         public static void Write(string data)
         {
           #if _DEBUG_
		    lock (locker) {
             System.IO.File.AppendAllText("log.txt", String.Format("[{0}]  ", DateTime.Now)+data+ "\n");
			 }
          #endif
         }

        
    }
}
