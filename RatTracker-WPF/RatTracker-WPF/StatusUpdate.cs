using System;

namespace RatTracker_WPF.EventHandlers
{
    public class StatusUpdateArgs : EventArgs
    {
        public string StatusMessage
        {
            get;
            set;
        }
    }

}
