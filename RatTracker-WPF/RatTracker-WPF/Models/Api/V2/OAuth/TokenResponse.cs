using System.Diagnostics.CodeAnalysis;

namespace RatTracker_WPF.Models.Api.V2.OAuth
{
  [SuppressMessage("ReSharper", "InconsistentNaming")]
  public class TokenResponse
  {
    public string access_token { get; set; }
    public string token_type { get; set; }
  }
}