using System.Collections.Generic;
using Newtonsoft.Json;

namespace RatTracker.Models.Apis.Systems
{
  public class Response
  {
    [JsonProperty("data")]
    public IList<Candidate> Candidates { get; set; }
  }
}