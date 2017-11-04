using System;
using RatTracker.Models.App.Rescues;

namespace RatTracker.Infrastructure.Events
{
  public class RescueEvents
  {
    public event EventHandler<RescueModel> SelectedRescueChanged;
    public event EventHandler<RescueFilter> FilterChanged;

    public void PostSelectedRescueChanged(object sender, RescueModel rescue)
    {
      sender = sender ?? this;
      SelectedRescueChanged?.Invoke(sender, rescue);
    }

    public void PostFilterChanged(object sender, RescueFilter rescueFilter)
    {
      sender = sender ?? this;
      FilterChanged?.Invoke(sender, rescueFilter);
    }
  }
}