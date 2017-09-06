using System;
using Newtonsoft.Json;

namespace RatTracker_WPF.Models.CmdrJournal
{
  [JsonObject]
  public class SuperCruiseExitLog : ICmdrJournalEntry
  {
    /// <summary>
    ///   The name of the closest stellar body.
    /// </summary>
    [JsonProperty]
    public string Body { get; set; }

    /// <summary>
    ///   type of the closest stellar body.
    /// </summary>
    [JsonProperty]
    public string BodyType { get; set; }

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