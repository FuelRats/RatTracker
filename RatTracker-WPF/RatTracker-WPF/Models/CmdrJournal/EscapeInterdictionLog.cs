using System;
using Newtonsoft.Json;

namespace RatTracker_WPF.Models.CmdrJournal
{
  [JsonObject]
  public class EscapeInterdictionLog : ICmdrJournalEntry
  {
    /// <summary>
    ///   Name of the interdictor
    /// </summary>
    [JsonProperty]
    public string Interdictor { get; set; }

    /// <summary>
    ///   Whether the interdictor is a player.
    /// </summary>
    [JsonProperty]
    public bool IsPlayer { get; set; }

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