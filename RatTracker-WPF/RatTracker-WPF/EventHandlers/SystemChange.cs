using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RatTracker_WPF.Models.Edsm;

namespace RatTracker_WPF.EventHandlers
{
    public class SystemChangeArgs : EventArgs
    {
        public string SystemName { get; set; }
        public EdsmCoords Coords { get; set; }
    }
}
