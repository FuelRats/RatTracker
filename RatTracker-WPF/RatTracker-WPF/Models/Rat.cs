using System;
using System.Collections.Generic;

namespace RatTracker_WPF.Models
{
    public class Rat
    {
        public bool Archive { get; set; }

        public string CmdrName { get; set; }

        public DateTime CreatedAt { get; set; }

        public DrillStatus Drilled { get; set; }

        public DateTime LastModified { get; set; }

        public DateTime Joined { get; set; }

        public IList<string> NickNames { get; set; }

        public string Platform { get; set; }

        // ReSharper disable once InconsistentNaming
        public string _Id { get; set; }

        public double Score { get; set; }
    }
}