using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RatTracker_WPF.Models
{
    public class EDSMSystem
    {
        public string name { get; set; }
        public EDSMCoords coords { get; set; }
    }
    public class EDSMCoords
    {
        public double x { get; set; }
        public double y { get; set; }
        public double z { get; set; }
    }
}
