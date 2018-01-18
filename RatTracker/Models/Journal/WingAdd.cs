using Newtonsoft.Json;

namespace RatTracker.Models.Journal
{
  public class WingAdd : JournalEntryBase
  {
    [JsonProperty("Name")]
    public string Name { get; set; }
  }
}