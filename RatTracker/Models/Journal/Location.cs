using Newtonsoft.Json;

namespace RatTracker.Models.Journal
{
  public class Location : JournalEntryBase
  {
    [JsonProperty("StarPos")]
    public double[] Coordinates { get; set; }

    [JsonProperty("SystemName")]
    public string SystemName { get; set; }

    [JsonProperty("Population")]
    public long Population { get; set; }
  }
}