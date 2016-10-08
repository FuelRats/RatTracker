using System;

namespace RatTracker_WPF.Models.CmdrLog
{
    public class EscapeInterdictionLog : ICmdrLogEntry
    {
        public string Interdictor { get; set; }
        public bool IsPlayer { get; set; }

        public DateTime Timestamp { get; set; }
        public string Event { get; set; }
    }
}