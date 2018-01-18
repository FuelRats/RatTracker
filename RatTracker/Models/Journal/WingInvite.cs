using Newtonsoft.Json;

namespace RatTracker.Models.Journal
{
  public class WingInvite : JournalEntryBase
  {
    [JsonProperty("Name")]
    public string Name { get; set; }
  }
}