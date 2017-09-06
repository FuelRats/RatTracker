using System;
using Newtonsoft.Json;

namespace RatTracker_WPF.Models.CmdrJournal
{
  [JsonObject]
  public class InterdictionLog : ICmdrJournalEntry
  {
    /// <summary>
    ///   Whether the interdiction was succuessful
    /// </summary>
    [JsonProperty]
    public bool Success { get; set; }

    /// <summary>
    ///   Interdicted pilot's name.
    /// </summary>
    [JsonProperty]
    public string Interdicted { get; set; }

    /// <summary>
    ///   Whether the interdicted pilot is a player.
    /// </summary>
    [JsonProperty]
    public bool IsPlayer { get; set; }

    /// <summary>
    ///   If interdicted pilot is a player, Combat rank of the interdicted player.
    /// </summary>
    [JsonProperty]
    public int CombatRank { get; set; }

    /// <summary>
    ///   If interdicted pilot is a NPC, the npc's minor faction affiliation.
    /// </summary>
    [JsonProperty]
    public string Faction { get; set; }

    /// <summary>
    ///   If interdicted pilot is a NPC working for a power, the npc's major faction affiliation.
    /// </summary>
    [JsonProperty]
    public string Power { get; set; }

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