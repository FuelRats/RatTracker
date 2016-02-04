using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RatTracker_WPF.Models
{
    public class TravelLog
    {
        public EDSMSystem system { get; set; }
        public DateTime lastvisited { get; set; }
    }
}
