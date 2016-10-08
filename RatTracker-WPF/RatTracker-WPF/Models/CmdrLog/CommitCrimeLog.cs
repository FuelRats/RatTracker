using System;

namespace RatTracker_WPF.Models.CmdrLog
{
    public class CommitCrimeLog : ICmdrLogEntry
    {
        public string CrimeType { get; set; }
        public string Faction { get; set; }
        public string Victim { get; set; }
        public int Fine { get; set; }
        public int Bounty { get; set; }

        public DateTime Timestamp { get; set; }
        public string Event { get; set; }
    }
}