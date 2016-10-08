using System;

namespace RatTracker_WPF.Models.CmdrLog
{
    public class FsdJumpLog : ICmdrLogEntry
    {
        public bool BoostUsed { get; set; }
        public double FuelLevel { get; set; }
        public double FuelUsed { get; set; }
        public double JumpDist { get; set; }
        public double[] StarPos { get; set; }
        public string Allegiance { get; set; }
        public string Body { get; set; }
        public string Economy { get; set; }
        public string Faction { get; set; }
        public string FactionState { get; set; }
        public string Government { get; set; }
        public string Security { get; set; }
        public string StarSystem { get; set; }
        public DateTime Timestamp { get; set; }
        public string Event { get; set; }
    }
}