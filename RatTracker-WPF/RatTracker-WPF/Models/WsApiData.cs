using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RatTracker_WPF.Models
{
    class WsApiData
    {
        public string type { get; set; }
        public dynamic data { get; set; } // Temporary, until we can unify a set for this.
    }
}
