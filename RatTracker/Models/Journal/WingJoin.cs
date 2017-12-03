using System.Collections.Generic;
using Newtonsoft.Json;

namespace RatTracker.Models.Journal
{
  public class WingJoin : JournalEntryBase
  {
    [JsonProperty("Others")]
    public IList<string> Others { get; set; }
  }
}