using System;

namespace RatTracker_WPF.Models.CmdrLog
{
    public class SupercruiseEntryLog : ICmdrLogEntry
    {
        public string StarSystem { get; set; }

        public DateTime Timestamp { get; set; }
        public string Event { get; set; }
    }
}