using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RatTracker_WPF.Models.Api
{
    public class APIQuery
    {
        public string action { get; set; }
        public IDictionary<string, string> data { get; set; }
    }
}
