using System;
using System.Linq;
using RatTracker_WPF.Models.Edsm;

namespace RatTracker_WPF.Models.NetLog
{
    public class TravelLog
    {
        public EdsmSystem system { get; set; }
        public DateTime lastvisited { get; set; }
    }
}
