using Newtonsoft.Json;
using RatTracker.Infrastructure.Json;

namespace RatTracker.Models.Apis.Systems
{
  public class Station
  {
    [JsonProperty("id")]
    public long Id { get; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("system_id")]
    public long SystemId { get; set; }

    [JsonProperty("body_id")]
    public long? BodyId { get; set; }

    [JsonProperty("max_landing_pad_size")]
    [JsonConverter(typeof(LandingPadSizeConverter))]
    public char MaxLandingPadSize { get; set; }

    [JsonProperty("distance_to_star")]
    public long? DistanceToStar { get; set; }

    [JsonProperty("has_docking")]
    public bool HasDocking { get; set; }

    [JsonProperty("has_refuel")]
    public bool HasRefuel { get; set; }

    [JsonProperty("has_repair")]
    public bool HasRepair { get; set; }

    [JsonProperty("has_rearm")]
    public bool HasRearm { get; set; }

    [JsonProperty("has_outfitting")]
    public bool HasOutfitting { get; set; }

    [JsonProperty("has_shipyard")]
    public bool HasShipyard { get; set; }

    [JsonProperty("is_planetary")]
    public bool IsPlanetary { get; set; }

    [JsonIgnore]
    public string DistanceToStarText => $"{DistanceToStar} ls";

    public override string ToString()
    {
      return DistanceToStar.HasValue ? $"{Name} ({DistanceToStarText})" : Name;
    }
  }
}