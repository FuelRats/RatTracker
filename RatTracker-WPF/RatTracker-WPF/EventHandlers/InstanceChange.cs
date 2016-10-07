using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RatTracker_WPF.EventHandlers
{
    public class InstanceChangeArgs : EventArgs
    {
        public string IslandName { get; set; }
        public List<string> IslandMembers { get; set; }
        public string SystemName { get; set; }

    }
}
