using System;
using RatTracker_WPF.Models.Edsm;

namespace RatTracker_WPF.EventHandlers
{
  public class SystemChangeArgs : EventArgs
  {
    public string SystemName { get; set; }
    public Coordinates Coords { get; set; }
  }
}