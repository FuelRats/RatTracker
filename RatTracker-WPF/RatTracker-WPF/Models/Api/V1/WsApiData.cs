namespace RatTracker_WPF.Models.Api.V1
{
  internal class WsApiData
  {
    public string type { get; set; }
    public dynamic data { get; set; } // Temporary, until we can unify a set for this.
  }
}