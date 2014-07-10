using System;
using System.Collections.Generic;
using System.Text;

namespace Fenryr.Classes
{
    public class LogEventArgs : EventArgs
    {
        public readonly string Message;
        public LogEventArgs(string mess)
        {
            Message = mess;
        }
    }

    public class DebugEventArgs : EventArgs
    {
        public readonly string What;
        public readonly string Where;
        public readonly DateTime When;

        public DebugEventArgs(DateTime when, string where, string what)
        {
            When = when;
            Where = where;
            What = what;
        }
    }

    public class ProgressEventArgs : EventArgs
    {
        public readonly int CurrentState;
        public readonly int MaxState;
        public readonly string Description;

        public ProgressEventArgs(int curState, int maxState, string description)
        {
            CurrentState = curState;
            MaxState = maxState;
            Description = description;
        }
    }

    public delegate void ProgressEventHandler(object sender, ProgressEventArgs e);
    public delegate void DebugEventHandler(object sender, DebugEventArgs e);
    public delegate void LogEventHandler(object sender, LogEventArgs e);
        


    public abstract class EventSupportable
    {
        public event ProgressEventHandler Progress;
        protected virtual void OnProgress(object sender, ProgressEventArgs e)
        {
            if (Progress != null)
            {
                Progress(sender, e);
            }

        }

        protected  void do_Progress(int cur, int all, string some)
        {
            OnProgress(this, new ProgressEventArgs(cur, all, some));
        }

        public event LogEventHandler Log;
        protected virtual void OnLog(object sender, LogEventArgs e)
        {
            if (Log != null)
            {
                Log(sender, e);
            }
        }

        protected void do_Log(string what)
        {
            OnLog(this, new LogEventArgs(what));
        }
        
        
        
        
        public event EventHandler Finish;
        protected virtual void OnFinish(object sender, EventArgs e)
        {
            if (Finish != null)
            {
                Finish(this, e);
            }
        }

        protected  void do_Finish()
        {
            OnFinish(this, EventArgs.Empty);
        }

        public event EventHandler Start;
        protected virtual void OnStart(object sender, EventArgs e)
        {
            if (Start != null)
            {
                Start(this, e);
            }
        }

        protected void do_Start()
        {
            OnStart(this, EventArgs.Empty);
        }


        public event DebugEventHandler Debug;
        protected virtual void OnDebug(object sender, DebugEventArgs e)
        {
            if (Debug != null)
            {
                Debug(sender, e);
            }
        }

        protected void do_Debug(string what, string where)
        {
            OnDebug(this, new DebugEventArgs(DateTime.Now, where, what));
        }

    }

}
