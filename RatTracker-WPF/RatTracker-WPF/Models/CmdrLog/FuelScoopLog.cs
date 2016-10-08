using System;

namespace RatTracker_WPF.Models.CmdrLog {
    public class FuelScoopLog : ICmdrLogEntry {
        public DateTime Timestamp { get; set; }
        public string Event { get; set; }
        public double Scooped { get; set; }
        public double Total { get; set; }
    }
}