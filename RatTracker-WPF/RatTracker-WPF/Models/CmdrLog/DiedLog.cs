using System;

namespace RatTracker_WPF.Models.CmdrLog
{
    public class DiedLog : ICmdrLogEntry
    {
        public string KillerName { get; set; }
        public string KillerShip { get; set; }
        public string KillerRank { get; set; }
        public Killer[] Killers { get; set; }

        public Killer[] KillersList
            => Killers ?? new[] {new Killer {Name = KillerName, Ship = KillerShip, Rank = KillerRank}};

        public DateTime Timestamp { get; set; }
        public string Event { get; set; }
    }

    public class Killer
    {
        public string Name { get; set; }
        public string Ship { get; set; }
        public string Rank { get; set; }
    }
}