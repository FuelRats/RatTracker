using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Caliburn.Micro;
using RatTracker.Api;
using RatTracker.Infrastructure.Extensions;
using RatTracker.Models.Api.Rescues;
using RatTracker.Models.App;

namespace RatTracker.ViewModels
{
  public class RescuesViewModel : Screen
  {
    private RescueModel selectedRescue;
    private double distance;
    private int jumps;

    public RescuesViewModel(EventBus eventBus)
    {
      Rescues = new ObservableCollection<RescueModel>();
      eventBus.RescueCreated += EventBusOnRescueCreated;
      eventBus.RescueUpdated += EventBusOnRescueUpdated;
      eventBus.RescuesReloaded += EventBusOnRescuesReloaded;
    }

    public ObservableCollection<RescueModel> Rescues { get; }

    public RescueModel SelectedRescue
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

    public void CallJumps()
    {
    }

    private void EventBusOnRescuesReloaded(object sender, IEnumerable<Rescue> rescues)
    {
      Rescues.Clear();
      Rescues.AddAll(rescues.Select(x=>new RescueModel(x)));
    }

    private void EventBusOnRescueUpdated(object sender, Rescue rescue)
    {
      var rescueModel = Rescues.SingleOrDefault(x => x.Rescue.Id == rescue.Id);
      if (rescueModel != null)
      {
        rescueModel.Rescue = rescue;
      }
      else
      {
        Rescues.Add(new RescueModel(rescue));
      }
    }

    private void EventBusOnRescueCreated(object sender, Rescue rescue)
    {
      Rescues.Add(new RescueModel(rescue));
    }
  }
}