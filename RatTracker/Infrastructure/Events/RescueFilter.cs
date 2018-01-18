using System;

namespace RatTracker.Infrastructure.Events
{
  [Flags]
  public enum RescueFilter
  {
    None = 0b00,
    PC = 0b01,
    Active = 0b10
  }
}