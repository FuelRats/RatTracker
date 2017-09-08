using System.Diagnostics.CodeAnalysis;

namespace RatTracker_WPF.Models.Api.V2.OAuth
{
  [SuppressMessage("ReSharper", "InconsistentNaming")]
  public class Auth
  {
    public string Code { get; set; }
    public string redirect_url { get; set; }
    public string grant_type { get; set; }
  }
}