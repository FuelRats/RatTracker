using System;

namespace RatTracker_WPF.Models.CmdrLog {
    public class HullDamageLog : ICmdrLogEntry {
        public DateTime Timestamp { get; set; }
        public string Event { get; set; }
        public double Health { get; set; }
    }
}