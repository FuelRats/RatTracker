using System;

namespace RatTracker_WPF.Infrastructure.EventHandlers
{
  public class StatusUpdateArgs : EventArgs
  {
    public string StatusMessage { get; set; }
  }
}