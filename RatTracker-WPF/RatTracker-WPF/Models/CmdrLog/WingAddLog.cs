using System;

namespace RatTracker_WPF.Models.CmdrLog
{
    public class WingAddLog : ICmdrLogEntry
    {
        public string Name { get; set; }

        public DateTime Timestamp { get; set; }
        public string Event { get; set; }
    }
}