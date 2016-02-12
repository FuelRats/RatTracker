using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RatTracker_WPF.Models.App
{
    class ConnectionInfo
    {
        public string WANAddress { get; set; }
        public NATType NATType { get; set; }
        public string TURNServer { get; set; }
        public string runID { get; set; }
        public int MTU { get; set; }
        public float Jitter { get; set; }
        public float Loss { get; set; }
        public int Srtt { get; set; }
        public float Act1 { get; set; }
        public float Act2 { get; set; }
        public int Flowcontrol { get; set; }
        public bool TURNActive { get; set; }
        public string EDServer { get; set; }
        public float FragmentationRate { get; set; }
    }
}
