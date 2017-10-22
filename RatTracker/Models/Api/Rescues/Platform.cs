using System.Runtime.Serialization;

namespace RatTracker.Models.Api.Rescues
{
  public enum Platform
  {
    [EnumMember(Value="unknown")]
    Unknown,

    [EnumMember(Value = "pc")]
    Pc,

    [EnumMember(Value = "xb")]
    Xb,

    [EnumMember(Value = "ps")]
    Ps
  }
}