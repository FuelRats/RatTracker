using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RatTracker_WPF.Models.Api
{
	class TokenResponse
	{
		public AccessToken access_token { get; set; }
		public string token_type { get; set; }
	}
}
