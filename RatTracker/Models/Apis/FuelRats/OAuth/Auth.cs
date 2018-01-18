using Newtonsoft.Json;

namespace RatTracker.Models.Apis.FuelRats.OAuth
{
  public class Auth
  {
    [JsonProperty(PropertyName = "Code")]
    public string Code { get; set; }

    [JsonProperty(PropertyName = "redirect_url")]
    public string RedirectUrl { get; set; }

    [JsonProperty(PropertyName = "grant_type")]
    public string GrantType { get; set; }
  }
}