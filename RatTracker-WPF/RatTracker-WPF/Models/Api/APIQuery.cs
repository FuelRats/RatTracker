using System.Collections.Generic;

namespace RatTracker_WPF.Models.Api
{
	public class APIQuery
    {
        public string Action { get; set; }
        public IDictionary<string, string> Data { get; set; }
    }
}
