using System;

namespace RatTracker_WPF.Models.CmdrLog {
    public class SupercruiseEntryLog : ICmdrLogEntry {

        public DateTime Timestamp { get; set; }
        public string Event { get; set; }
        public string StarSystem { get; set; }
    }
}