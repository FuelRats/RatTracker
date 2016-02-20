using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RatTracker_WPF.Models.Api
{
    public class TPAMessage
    {
        public string action { get; set; }
        //public Meta meta { get; set; } //Currently broken, API does not take Meta
        public string applicationId { get; set; }
        public Dictionary<string,string> data { get; set; }
    }
}
