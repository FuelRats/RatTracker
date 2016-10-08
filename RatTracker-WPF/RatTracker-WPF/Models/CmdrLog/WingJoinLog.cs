using System;

namespace RatTracker_WPF.Models.CmdrLog
{
    public class WingJoinLog : ICmdrLogEntry
    {
        public string[] Others { get; set; }
        public DateTime Timestamp { get; set; }
        public string Event { get; set; }
    }
}