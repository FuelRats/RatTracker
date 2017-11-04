using System.Collections.Generic;
using Newtonsoft.Json;

namespace RatTracker.Models.Apis.Systems
{
  public class StarSystem
  {
    [JsonProperty("id")]
    public long Id { get; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("x")]
    public double X { get; set; }

    [JsonProperty("y")]
    public double Y { get; set; }

    [JsonProperty("z")]
    public double Z { get; set; }

    [JsonProperty("population")]
    public long? Population { get; set; }

    [JsonProperty("needs_permit")]
    public bool NeedsPermit { get; set; }

    [JsonProperty("updated_at")]
    public long UpdatedAt { get; set; }

    [JsonProperty("bodies")]
    public IList<Body> Bodies { get; set; }

    [JsonIgnore]
    public double? Distance { get; set; }

    [JsonIgnore]
    public string DistanceText => $"{Distance:N} ly";

    public override string ToString()
    {
      return Distance.HasValue ? $"{Name} ({DistanceText})" : Name;
    }
  }
}