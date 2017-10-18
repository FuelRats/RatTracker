using System;

namespace RatTracker_WPF.Infrastructure.EventHandlers
{
  public class FriendRequestArgs : EventArgs
  {
    public string FriendName { get; set; }
  }
}