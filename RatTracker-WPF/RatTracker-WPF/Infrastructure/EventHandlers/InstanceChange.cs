using System;
using System.Collections.Generic;

namespace RatTracker_WPF.Infrastructure.EventHandlers
{
  public class InstanceChangeArgs : EventArgs
  {
    public string IslandName { get; set; }
    public List<string> IslandMembers { get; set; }
    public string SystemName { get; set; }
  }
}