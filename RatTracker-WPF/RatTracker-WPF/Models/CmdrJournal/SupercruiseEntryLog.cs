using System;
using Newtonsoft.Json;

namespace RatTracker_WPF.Models.CmdrJournal
{
  [JsonObject]
  public class SupercruiseEntryLog : ICmdrJournalEntry
  {
    /// <summary>
    ///   Name of the starsystem the player is in.
    /// </summary>
    [JsonProperty]
    public string StarSystem { get; set; }

    /// <summary>
    ///   Time the event occured.
    /// </summary>
    [JsonProperty("timestamp")]
    public DateTime Timestamp { get; set; }

    /// <summary>
    ///   Event type name.
    /// </summary>
    [JsonProperty("event")]
    public string Event { get; set; }
  }
}