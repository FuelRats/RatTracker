namespace RatTracker_WPF.Models.Api
{
  internal class WsApiData
  {
    public string type { get; set; }
    public dynamic data { get; set; } // Temporary, until we can unify a set for this.
  }
}