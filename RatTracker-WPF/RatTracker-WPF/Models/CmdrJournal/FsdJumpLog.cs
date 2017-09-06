using System;
using Newtonsoft.Json;

namespace RatTracker_WPF.Models.CmdrJournal
{
  [JsonObject]
  public class FsdJumpLog : ICmdrJournalEntry
  {
    /// <summary>
    ///   Whether a FSD boost was used.
    /// </summary>
    [JsonProperty]
    public bool BoostUsed { get; set; }

    /// <summary>
    ///   Fuel level of the player, post FSD jump.
    /// </summary>
    [JsonProperty]
    public double FuelLevel { get; set; }

    /// <summary>
    ///   Fuel used to make the FSD jump.
    /// </summary>
    [JsonProperty]
    public double FuelUsed { get; set; }

    /// <summary>
    ///   Distance the player jumped.
    /// </summary>
    [JsonProperty]
    public double JumpDist { get; set; }

    /// <summary>
    ///   Array of doubles representing the position of the star on the galactic plane.
    /// </summary>
    /// <remarks>Format: {X, Y, Z}</remarks>
    [JsonProperty]
    public double[] StarPos { get; set; }

    /// <summary>
    ///   System's major faction allegiance.
    /// </summary>
    [JsonProperty]
    public string Allegiance { get; set; }

    /// <summary>
    ///   Star's body name;
    /// </summary>
    [JsonProperty]
    public string Body { get; set; }

    /// <summary>
    ///   System's economy type.
    /// </summary>
    [JsonProperty]
    public string Economy { get; set; }

    /// <summary>
    ///   System's controlling minor faction.
    /// </summary>
    [JsonProperty]
    public string Faction { get; set; }

    /// <summary>
    ///   System's current economic or political state.
    /// </summary>
    [JsonProperty]
    public string FactionState { get; set; }

    /// <summary>
    ///   Controlling government's type.
    /// </summary>
    [JsonProperty]
    public string Government { get; set; }

    /// <summary>
    ///   If player is pledged to a power, the system's controlling power.
    /// </summary>
    /// <remarks>Use <see cref="PowersList" /> for consistant single result.</remarks>
    [JsonProperty]
    public string Power { get; set; }

    /// <summary>
    ///   If player is pledged to a power, the system's contesting powers.
    /// </summary>
    /// <remarks>Use <see cref="PowersList" /> for consistant single result.</remarks>
    [JsonProperty]
    public string[] Powers { get; set; }

    /// <summary>
    ///   if player is pledged to a power, the system's major powers.
    /// </summary>
    [JsonIgnore]
    public string[] PowersList => Powers ?? new[] {Power};

    /// <summary>
    ///   Security level of the system.
    /// </summary>
    [JsonProperty]
    public string Security { get; set; }

    /// <summary>
    ///   Name of the star system.
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