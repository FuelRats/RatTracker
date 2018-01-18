using Newtonsoft.Json;

namespace RatTracker.Models.Apis.FuelRats.OAuth
{
  public class TokenResponse
  {
    [JsonProperty(PropertyName = "access_token")]
    public string AccessToken { get; set; }

    [JsonProperty(PropertyName = "token_type")]
    public string TokenType { get; set; }
  }
}