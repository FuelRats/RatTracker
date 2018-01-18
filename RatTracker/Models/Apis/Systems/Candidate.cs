using System.Collections.Generic;
using Newtonsoft.Json;

namespace RatTracker.Models.Apis.Systems
{
  public class Candidate
  {
    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("distance")]
    public double Distance { get; set; }

    [JsonProperty("id")]
    public long Id { get; set; }

    [JsonIgnore]
    public IList<Body> Bodies { get; } = new List<Body>();

    [JsonIgnore]
    public IList<Station> Stations { get; } = new List<Station>();

    public override string ToString()
    {
      return Name;
    }
  }
}