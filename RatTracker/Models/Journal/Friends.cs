using Newtonsoft.Json;

namespace RatTracker.Models.Journal
{
  public class Friends : JournalEntryBase
  {
    [JsonProperty("Status")]
    public string Status { get; set; }

    [JsonProperty("Name")]
    public string Name { get; set; }
  }
}