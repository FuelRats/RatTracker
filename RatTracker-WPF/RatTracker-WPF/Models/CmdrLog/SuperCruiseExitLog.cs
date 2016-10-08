using System;

namespace RatTracker_WPF.Models.CmdrLog
{
    public class SuperCruiseExitLog : ICmdrLogEntry
    {
        public string Body { get; set; }
        public string BodyType { get; set; }
        public DateTime Timestamp { get; set; }
        public string Event { get; set; }
    }
}