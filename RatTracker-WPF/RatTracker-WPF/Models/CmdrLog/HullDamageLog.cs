using System;

namespace RatTracker_WPF.Models.CmdrLog
{
    public class HullDamageLog : ICmdrLogEntry
    {
        public double Health { get; set; }
        public DateTime Timestamp { get; set; }
        public string Event { get; set; }
    }
}