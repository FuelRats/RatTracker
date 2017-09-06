using System.Collections.Generic;

namespace RatTracker_WPF.Models.Api
{
  public class APIQuery
  {
    public string[] action { get; set; }
    public IDictionary<string, string> data { get; set; }
  }
}