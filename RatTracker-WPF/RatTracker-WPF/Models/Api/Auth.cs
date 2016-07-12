using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RatTracker_WPF.Models.Api
{
	class Auth
	{
		public string code { get; set; }
		public string redirect_url { get; set; }
		public string grant_type { get; set; }
	}
}
