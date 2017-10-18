using System.Runtime.Serialization;

namespace RatTracker_WPF.Models.Api.V2
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