using System.Collections.Generic;

namespace RatTracker_WPF.Models
{
    public class RootObject
    {
        public Links Links { get; set; }
        public Meta Meta { get; set; }
        public List<Datum> Data { get; set; }
    }
}