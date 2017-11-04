using System.Collections.Generic;
using Newtonsoft.Json;

namespace RatTracker.Models.Apis.Systems
{
  public class Body
  {
    [JsonProperty("is_landable")]
    public bool IsLandable { get; set; }

    [JsonProperty("type_name")]
    public string TypeName { get; set; }

    [JsonProperty("is_main_star")]
    public bool? IsMainStar { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("id")]
    public long Id { get; set; }

    [JsonProperty("system_id")]
    public long SystemId { get; set; }

    [JsonProperty("distance_to_arrival")]
    public long? DistanceToArrival { get; set; }

    [JsonProperty("spectral_class")]
    public string SpectralClass { get; set; }

    [JsonProperty("group_name")]
    public string GroupName { get; set; }

    [JsonProperty("stations")]
    public IList<Station> Stations { get; set; }

    public override string ToString()
    {
      return $"{Name} ({GroupName})";
    }
  }
}