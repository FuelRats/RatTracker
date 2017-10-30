using System.Runtime.Serialization;
using Newtonsoft.Json;
using RatTracker.Infrastructure.Json;

namespace RatTracker.Models.Api.Rescues
{
  [JsonConverter(typeof(PlatformConverter))]
  public enum Platform
  {
    Unknown,

    [EnumMember(Value = "pc")]
    Pc,

    [EnumMember(Value = "xb")]
    Xb,

    [EnumMember(Value = "ps")]
    Ps
  }
}