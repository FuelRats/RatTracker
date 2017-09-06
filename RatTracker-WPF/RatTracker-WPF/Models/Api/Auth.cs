namespace RatTracker_WPF.Models.Api
{
  internal class Auth
  {
    public string code { get; set; }
    public string redirect_url { get; set; }
    public string grant_type { get; set; }
  }
}