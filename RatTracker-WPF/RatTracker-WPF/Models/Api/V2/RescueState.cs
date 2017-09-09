using System.Runtime.Serialization;

namespace RatTracker_WPF.Models.Api.V2
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