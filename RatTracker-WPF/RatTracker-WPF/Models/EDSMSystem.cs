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
        public float x { get; set; }
        public float y { get; set; }
        public float z { get; set; }
    }
}
