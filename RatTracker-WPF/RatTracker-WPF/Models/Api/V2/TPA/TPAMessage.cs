using Newtonsoft.Json.Linq;

namespace RatTracker_WPF.Models.Api.V2.TPA
{
  public class TpaMessage
  {
    public string Action { get; set; }

    public string ApplicationId { get; set; }

    public JObject Data { get; set; }
  }
}