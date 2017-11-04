using System.Runtime.Serialization;

namespace RatTracker.Models.Apis.FuelRats.Rescues
{
  public enum RescueState
  {
    [EnumMember(Value = "open")] Open,

    [EnumMember(Value = "inactive")] Inactive,

    [EnumMember(Value = "closed")] Closed
  }
}