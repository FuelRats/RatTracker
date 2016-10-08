using System;

namespace RatTracker_WPF.Models.CmdrLog {
    public class WingAddLog: ICmdrLogEntry {

        public DateTime Timestamp { get; set; }
        public string Event { get; set; }
        public string Name { get; set; }
    }
}