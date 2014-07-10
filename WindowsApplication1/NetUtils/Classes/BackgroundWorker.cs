using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Fenryr.Classes
{
   public abstract class BackgroundWorker : EventSupportable
    {

       protected ThreadManager m_Manager;

       public ThreadManager ThreadManager
       {
           get { return m_Manager; }
           set { m_Manager = value; }
       }

       
       Thread _thread = null;
       bool _terminated = false;

       protected bool Terminated
       {
           get { return _terminated;  }
       }

       public void Start()
       {
           if (_thread != null && ((_thread.ThreadState & ThreadState.Suspended) != 0))
           {
               _thread.Resume();
               while ((_thread.ThreadState & ThreadState.Suspended) != 0) ;
           }
           
           if (_thread != null)
           {
               Terminate();
               if (!_thread.Join(2000))
               {
                   Abort();
               }
           }

           _thread = new Thread(new ThreadStart (Execute) );
           _thread.IsBackground = true;
           _thread.Start();
       }

       protected void Sleep(int msec)
       {
           for (int i = 0; i < (int)msec / 1000; i++)
           {
               if (Terminated) return;
               System.Threading.Thread.Sleep( 1000);
           }
           if (Terminated) return;
           System.Threading.Thread.Sleep(msec % 1000);
       }

       public void Wait()
       {
           _thread.Join();
       }



       protected abstract void Execute();



       public void Terminate()
       {
           _terminated = true;
       }

       public void Suspend()
       {
           if (_thread != null && _thread.ThreadState == ThreadState.Running)
           {
               _thread.Suspend();
           }
       }

       public void Resume()
       {
           if (_thread != null && _thread.ThreadState == ThreadState.Suspended)
           {
               _thread.Resume();
           }
       }

       public void Abort()
       {
           try
           {
               try
               {
                   _thread.Abort();
               }
               catch (ThreadAbortException)
               {
               }
           }
           catch { }
           _thread = null;
       }

    }
}
