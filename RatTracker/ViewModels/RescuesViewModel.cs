using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Caliburn.Micro;
using RatTracker.Api.StarSystems;
using RatTracker.Infrastructure.Events;
using RatTracker.Infrastructure.Extensions;
using RatTracker.Models.Apis.FuelRats.Rescues;
using RatTracker.Models.App.Rescues;

namespace RatTracker.ViewModels
{
  public class RescuesViewModel : Screen
  {
    private readonly EventBus eventBus;
    private readonly SystemApi systemApi;
    private readonly IList<RescueModel> allRescues;
    private RescueModel selectedRescue;
    private RescueFilter rescueFilter;

    public RescuesViewModel(EventBus eventBus, SystemApi systemApi)
    {
      this.eventBus = eventBus;
      this.systemApi = systemApi;
      allRescues = new List<RescueModel>();
      Rescues = new ObservableCollection<RescueModel>();
      eventBus.RescueCreated += EventBusOnRescueCreated;
      eventBus.RescueUpdated += EventBusOnRescueUpdated;
      eventBus.RescuesReloaded += EventBusOnRescuesReloaded;
      eventBus.RescueClosed += EventBusOnRescueClosed;
      eventBus.Rescues.FilterChanged += RescuesOnFilterChanged;
    }

    public ObservableCollection<RescueModel> Rescues { get; }

    public RescueModel SelectedRescue
    {
      get => selectedRescue;
      set
      {
        selectedRescue = value;
        NotifyOfPropertyChange();
        eventBus.Rescues.PostSelectedRescueChanged(this, value);
      }
    }

    private void RescuesOnFilterChanged(object sender, RescueFilter filter)
    {
      rescueFilter = filter;
      FilterRescues();
    }

    private async void EventBusOnRescuesReloaded(object sender, IEnumerable<Rescue> rescues)
    {
      allRescues.Clear();
      var rescueModels = rescues.Select(x => new RescueModel(x)).ToList();
      allRescues.AddAll(rescueModels);
      FilterRescues();
      await FetchSystems(rescueModels);
    }

    private async void EventBusOnRescueUpdated(object sender, Rescue rescue)
    {
      var update = true;
      var rescueModel = allRescues.SingleOrDefault(x => x.Rescue.Id == rescue.Id);
      if (rescueModel != null)
      {
        update = rescueModel.Rescue.System != rescue.System;
        rescueModel.Rescue = rescue;
      }
      else
      {
        rescueModel = new RescueModel(rescue);
        allRescues.Add(rescueModel);
      }

      FilterRescues();
      if (update)
      {
        await FetchSystem(rescueModel);
      }
    }

    private async void EventBusOnRescueCreated(object sender, Rescue rescue)
    {
      var rescueModel = new RescueModel(rescue);
      allRescues.Add(rescueModel);
      FilterRescues();
      await FetchSystem(rescueModel);
    }

    private void EventBusOnRescueClosed(object sender, Rescue rescue)
    {
      var rescueModels = Rescues.Where(x => x.Rescue == rescue).ToList();
      allRescues.RemoveAll(rescueModels);
      FilterRescues();
    }

    private void FilterRescues()
    {
      Rescues.Clear();

      var pc = rescueFilter.HasFlag(RescueFilter.PC);
      var active = rescueFilter.HasFlag(RescueFilter.Active);

      var rescues = from rescue in allRescues
                    where (!pc || rescue.Rescue.Platform == Platform.Pc)
                          && (!active || rescue.Rescue.Status == RescueState.Open)
                    select rescue;
      Rescues.AddAll(rescues);
    }

    private async Task FetchSystems(IEnumerable<RescueModel> rescues)
    {
      foreach (var rescue in rescues)
      {
        await FetchSystem(rescue);
      }
    }

    private async Task FetchSystem(RescueModel rescue)
    {
      if (string.IsNullOrWhiteSpace(rescue.Rescue.System)) { return; }
      var starSystem = await systemApi.GetSystemByNameAsync(rescue.Rescue.System);
      rescue.System = starSystem;
    }
  }
}