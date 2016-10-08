using System;

namespace RatTracker_WPF.Models.CmdrLog {
    public class EscapeInterdictionLog : ICmdrLogEntry {

        public DateTime Timestamp { get; set; }
        public string Event { get; set; }
        public string Interdictor { get; set; }
        public bool IsPlayer { get; set; }


    }
}