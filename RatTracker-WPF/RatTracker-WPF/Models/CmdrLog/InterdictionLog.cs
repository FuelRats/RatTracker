using System;

namespace RatTracker_WPF.Models.CmdrLog {
    public class InterdictionLog : ICmdrLogEntry {
        public DateTime Timestamp { get; set; }
        public string Event { get; set; }
        public bool Success { get; set; }
        public string Interdicted { get; set; }
        public bool IsPlayer { get; set; }
        public int CombatRank { get; set; }
        public string Faction { get; set; }
        public string Power { get; set; }
    }
}