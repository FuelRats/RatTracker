using System;
using Newtonsoft.Json;

namespace RatTracker.Models.Journal
{
  public abstract class JournalEntryBase
  {
    [JsonProperty("timestamp")]
    public DateTime Timestamp { get; set; }

    [JsonProperty("event")]
    public string Event { get; set; }
  }
}