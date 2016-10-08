using System;

namespace RatTracker_WPF.Models.CmdrLog
{
    public class FuelScoopLog : ICmdrLogEntry
    {
        public double Scooped { get; set; }
        public double Total { get; set; }
        public DateTime Timestamp { get; set; }
        public string Event { get; set; }
    }
}