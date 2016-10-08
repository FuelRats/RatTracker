using System;

namespace RatTracker_WPF.Models.CmdrLog {
    public class WingJoinLog : ICmdrLogEntry {
        public DateTime Timestamp { get; set; }
        public string Event { get; set; }
        public string[] Others { get; set; }
    }
}