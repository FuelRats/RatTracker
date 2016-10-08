using System;

namespace RatTracker_WPF.Models.CmdrLog {
    public class SuperCruiseExitLog : ICmdrLogEntry {
        public DateTime Timestamp { get; set; }
        public string Event { get; set; }
        public string Body { get; set; }
        public string BodyType { get; set; }
    }
}