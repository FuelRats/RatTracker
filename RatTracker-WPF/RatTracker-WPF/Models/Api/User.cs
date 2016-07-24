using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RatTracker_WPF.Models.Api
{
	class User
	{
		public string UserName { get; set; }
		public string Password { get; set; }
		public IList<string> NickNames { get; set; }
		public DrillStatus Drilled { get; set; }


	}
}
