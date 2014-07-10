using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;


namespace Fenryr.Classes
{
    
    public delegate void VoidDelegate ();

    public class ThreadManager
    {
        List<BackgroundWorker> m_Workers = new List<BackgroundWorker>();
        List<Object> m_Subscribers = new List<Object>();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern UInt32 WaitForSingleObject(IntPtr hHandle, UInt32 dwMilliseconds);

       public BackgroundWorker[] Workers
        {
            get
            {
                lock (m_Workers)
                {
                    return m_Workers.ToArray();
                }
            }
        }

        bool m_Terminated = false;

        public  event EventHandler AllThreadsFinished;

        ManualResetEvent m_Event = new ManualResetEvent(false);
        ManualResetEvent m_ThreadsFinished = new ManualResetEvent(false);

        public void RunThreads()
        {
            m_Terminated = false;
            lock (m_Workers)
            {
                foreach (BackgroundWorker worker in m_Workers)
                {
                    worker.Start();
                }
            }
        }

        public void TerminateThreads()
        {
            m_Terminated = true;
            lock (m_Workers)
            {
                foreach (BackgroundWorker worker in m_Workers)
                {
                    worker.Terminate();
                }
                m_Workers.Clear();
            }
        }


        public void AbortThreads()
        {
            m_Terminated = true;
            lock (m_Workers)
            {
                foreach (BackgroundWorker worker in m_Workers)
                {
                    try { worker.Abort(); }
                    catch
                    {

                    }
                }
                m_Workers.Clear();
            }
        }



      public  void InvokeDelegateOnceAndForAll (VoidDelegate del, ref bool WasInvoked)
        {
            lock (this) {
                if (WasInvoked) return;
                try
                {
                    if (del != null)
                        del();
                }
                finally
                {
                    WasInvoked = true;
                }
            }
        }



        public void SubscribeForATask(BackgroundWorker subscriber)
        {
            lock (m_Subscribers)
            {
                m_Subscribers.Add(subscriber);
                if (m_Subscribers.Count == 1)
                    m_Event.Reset();
            }
        }

        public void UnsubscribeFromTask(BackgroundWorker subscriber)
        {
            lock (m_Subscribers)
            {
                m_Subscribers.Remove(subscriber);
                if (m_Subscribers.Count == 0)
                    m_Event.Set();
            }
        }


        public void WaitTaskCompleted()
        {
            while (!m_Terminated)
            {
                if (WaitForSingleObject(m_Event.Handle , 100) == 0)
                    return;
            }
        }


        void RaiseAllThreadsFinished()
        {
            if (AllThreadsFinished != null)
            {
                AllThreadsFinished(this, EventArgs.Empty);
            }
        }


       public void Init()
        {
            lock (m_Workers)
            {
                foreach (BackgroundWorker w in m_Workers)
                    w.Terminate();
                m_Workers.Clear();
            }
            m_ThreadsFinished.Reset();
        }


        void worker_Finish(object sender, EventArgs e)
        {
            if (!m_Terminated)
            {
                lock (m_Workers)
                {
                    m_Workers.Remove((BackgroundWorker)sender);
                    if (m_Workers.Count == 0)
                    {
                        RaiseAllThreadsFinished();
                        m_ThreadsFinished.Set();
                    }
                }
            }
        }


        public void AddWorker(BackgroundWorker worker)
        {
             worker.Finish +=new EventHandler(worker_Finish);
             worker.ThreadManager = this;
             lock (m_Workers)
             {
                 m_Workers.Add(worker);
             }
        }


        public void WaitAllThreads()
        {
            if (m_Workers.Count == 0) return;
            WaitForSingleObject(m_ThreadsFinished.Handle, 0xffffffff);
        }

        public bool WaitAllThreads(uint timeout)
        {
            if (m_Workers.Count == 0) return true;
            return (WaitForSingleObject(m_ThreadsFinished.Handle, timeout) == 0);
        }

    }

}
