using System;

namespace RatTracker_WPF.Models.CmdrLog
{
    public class WingLeaveLog : ICmdrLogEntry
    {
        public DateTime Timestamp { get; set; }
        public string Event { get; set; }
    }
}