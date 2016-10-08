using System;

namespace RatTracker_WPF.Models.CmdrLog
{
    public class InterdictedLog : ICmdrLogEntry
    {
        public bool Submitted { get; set; }
        public string Interdictor { get; set; }
        public bool IsPlayer { get; set; }
        public int CombatRank { get; set; }
        public DateTime Timestamp { get; set; }
        public string Event { get; set; }
    }
}