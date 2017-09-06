using System;

namespace RatTracker_WPF.EventHandlers
{
  public class FriendRequestArgs : EventArgs
  {
    public string FriendName { get; set; }
  }
}