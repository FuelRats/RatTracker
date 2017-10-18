using System;
using Newtonsoft.Json;

namespace RatTracker_WPF.Models.CmdrJournal
{
  [JsonObject]
  public class FuelScoopLog : ICmdrJournalEntry
  {
    /// <summary>
    ///   Amount of fuel scooped.
    /// </summary>
    [JsonProperty]
    public double Scooped { get; set; }

    /// <summary>
    ///   Total fuel level after scooping.
    /// </summary>
    [JsonProperty]
    public double Total { get; set; }

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