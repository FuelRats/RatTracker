using Newtonsoft.Json;
using RatTracker.Infrastructure.Json;

namespace RatTracker.Models.App.StarSystems
{
  public class EddbStation
  {
    public EddbStation(long id)
    {
      Id = id;
    }

    [JsonProperty("id")]
    public long Id { get; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("system_id")]
    public long SystemId { get; set; }

    [JsonProperty("max_landing_pad_size")]
    [JsonConverter(typeof(LandingPadSizeConverter))]
    public char MaxLandingPadSize { get; set; }

    [JsonProperty("distance_to_star")]
    public long? DistanceToStar { get; set; }

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

    [JsonProperty("")]
    public long UpdatedAt { get; set; }

    public string UpperCaseName => Name?.ToUpper() ?? string.Empty;

    public override bool Equals(object obj)
    {
      if (ReferenceEquals(null, obj)) { return false; }
      if (ReferenceEquals(this, obj)) { return true; }
      return obj.GetType() == GetType() && Equals((EddbStation) obj);
    }

    public override int GetHashCode()
    {
      return Id.GetHashCode();
    }

    private bool Equals(EddbStation other)
    {
      return Id == other.Id;
    }
  }
}