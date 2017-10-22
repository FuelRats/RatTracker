using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Caliburn.Micro;
using RatTracker.Api;
using RatTracker.Infrastructure.Extensions;
using RatTracker.Models.Api.Rescues;

namespace RatTracker.ViewModels
{
  public class RescuesViewModel : Screen
  {
    private Rescue selectedRescue;
    private double distance;
    private int jumps;
    private readonly ObservableCollection<Rescue> rescues = new ObservableCollection<Rescue>();

    public RescuesViewModel(EventBus eventBus)
    {
      eventBus.RescueCreated += EventBusOnRescueCreated;
      eventBus.RescueUpdated += EventBusOnRescueUpdated;
      eventBus.RescuesReloaded += EventBusOnRescuesReloaded;
    }

    private void EventBusOnRescuesReloaded(object sender, IEnumerable<Rescue> rescues)
    {
      Rescues.Clear();
      Rescues.AddAll(rescues);
    }

    public ObservableCollection<Rescue> Rescues
    {
      get { return rescues; }
    }

    public Rescue SelectedRescue
    {
      get => selectedRescue;
      set
      {
        selectedRescue = value;
        NotifyOfPropertyChange();
      }
    }

    public double Distance
    {
      get => distance;
      set
      {
        distance = value;
        NotifyOfPropertyChange();
      }
    }

    public int Jumps
    {
      get => jumps;
      set
      {
        jumps = value;
        NotifyOfPropertyChange();
      }
    }

    public void FindNearestStation()
    {
    }

    private void EventBusOnRescueUpdated(object sender, Rescue rescue)
    {
      var oldRescue = Rescues.SingleOrDefault(x => x.Id == rescue.Id);
      if (oldRescue != null)
      {
        var index = Rescues.IndexOf(oldRescue);
        Rescues[index] = rescue;
      }
      else
      {
        Rescues.Add(rescue);
      }
    }

    private void EventBusOnRescueCreated(object sender, Rescue rescue)
    {
      Rescues.Add(rescue);
    }
  }
}