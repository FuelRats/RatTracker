using System;
using RatTracker_WPF.Models.Edsm;

namespace RatTracker_WPF.Models.NetLog
{
  public class TravelLog
  {
    public EdsmSystem System { get; set; }
    public DateTime LastVisited { get; set; }
  }
}