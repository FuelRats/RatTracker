using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RatTracker_WPF.EventHandlers
{
    class ConnInfo
    {
        public float Srtt { get; set; }
        public float Loss { get; set; }
        public float Jitter { get; set; }
        public float Act1 { get; set; }
        public float Act2 { get; set; }
    }
}
