using System.Collections.Generic;

namespace RatTracker_WPF.Models.Api.V1
{
  internal class User
  {
    public string UserName { get; set; }
    public string Password { get; set; }
    public IList<string> NickNames { get; set; }
    public DrillStatus Drilled { get; set; }
  }
}