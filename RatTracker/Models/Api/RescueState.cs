using System.Runtime.Serialization;

namespace RatTracker.Models.Api
{
  public enum RescueState
  {
    [EnumMember(Value = "open")]
    Open,

    [EnumMember(Value = "inactive")]
    Inactive,

    [EnumMember(Value = "closed")]
    Closed
  }
}