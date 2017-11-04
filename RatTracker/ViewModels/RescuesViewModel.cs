using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Caliburn.Micro;
using RatTracker.Api.StarSystems;
using RatTracker.Infrastructure;
using RatTracker.Infrastructure.Events;
using RatTracker.Infrastructure.Extensions;
using RatTracker.Models.Apis.FuelRats.Rescues;
using RatTracker.Models.App.Rescues;

namespace RatTracker.ViewModels
{
  public class RescuesViewModel : Screen
  {
    private readonly SystemApi systemApi;
    private RescueModel selectedRescue;
    private double distance;
    private int jumps;
    private char maxLandingPadSize;

    public RescuesViewModel(EventBus eventBus, SystemApi systemApi)
    {
      this.systemApi = systemApi;
      Rescues = new ObservableCollection<RescueModel>();
      eventBus.RescueCreated += EventBusOnRescueCreated;
      eventBus.RescueUpdated += EventBusOnRescueUpdated;
      eventBus.RescuesReloaded += EventBusOnRescuesReloaded;
      eventBus.RescueClosed += EventBusOnRescueClosed;
      LandingPadSizes = new ObservableCollection<char> {'L', 'M'};
      MaxLandingPadSize = LandingPadSizes.First();
    }

    public ObservableCollection<RescueModel> Rescues { get; }

    public ObservableCollection<char> LandingPadSizes { get; }

    public RescueModel SelectedRescue
    {
      get => selectedRescue;
      set
      {
        selectedRescue = value;
        NotifyOfPropertyChange();
        RecalculateJumps(value.Rescue.System);
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

    public char MaxLandingPadSize
    {
      get => maxLandingPadSize;
      set
      {
        maxLandingPadSize = value;
        NotifyOfPropertyChange();
      }
    }

    public async void FindNearestStation()
    {
      var rescue = SelectedRescue;
      if (rescue?.System == null) { return; }
      if (rescue.System.Population > 0) { rescue.NearestSystem = rescue.System; }

      if (rescue.NearestSystem == null)
      {
        var system = await systemApi.GetNearestPopulatedSystemAsync(rescue.System.X, rescue.System.Y, rescue.System.Z, MaxLandingPadSize);
        rescue.NearestSystem = system;
      }

      var stations = from body in rescue.NearestSystem.Bodies
        from station in body.Stations
        where station.MaxLandingPadSize == MaxLandingPadSize
        orderby body.DistanceToArrival, station.DistanceToStar
        select station;

      rescue.NearestStation = stations.FirstOrDefault();
    }

    public void CallJumps()
    {
      DialogHelper.ShowWarning("This function is not implemented yet.");
    }

    public void CopyClientName()
    {
      if (SelectedRescue == null) { return; }
      Clipboard.SetText(SelectedRescue.Rescue.Client);
    }

    public void CopySystemName()
    {
      if (SelectedRescue == null) { return; }
      Clipboard.SetText(SelectedRescue.Rescue.System);
    }

    public void CopyNearestSystemName()
    {
      if (SelectedRescue?.NearestSystem == null) { return; }
      Clipboard.SetText(SelectedRescue.NearestSystem.Name);
    }

    public void CopyNearestStationName()
    {
      if (SelectedRescue?.NearestStation == null) { return; }
      Clipboard.SetText(SelectedRescue.NearestStation.Name);
    }

    private async void RecalculateJumps(string rescueSystem)
    {
      // TODO implement jump calculation
      var jumpCount = await Task.Run(() => !string.IsNullOrWhiteSpace(rescueSystem) ? new Random().Next(10) : 0);

      if (SelectedRescue?.Rescue.System == rescueSystem)
      {
        Jumps = jumpCount;
      }
    }

    private async void EventBusOnRescuesReloaded(object sender, IEnumerable<Rescue> rescues)
    {
      Rescues.Clear();
      var rescueModels = rescues.Select(x => new RescueModel(x)).ToList();
      Rescues.AddAll(rescueModels);
      await FetchSystems(rescueModels);
    }

    private async void EventBusOnRescueUpdated(object sender, Rescue rescue)
    {
      var update = true;
      var rescueModel = Rescues.SingleOrDefault(x => x.Rescue.Id == rescue.Id);
      if (rescueModel != null)
      {
        update = rescueModel.Rescue.System != rescue.System;
        rescueModel.Rescue = rescue;
      }
      else
      {
        rescueModel = new RescueModel(rescue);
        Rescues.Add(rescueModel);
      }

      if (update)
      {
        await FetchSystem(rescueModel);
      }
    }

    private async void EventBusOnRescueCreated(object sender, Rescue rescue)
    {
      var rescueModel = new RescueModel(rescue);
      Rescues.Add(rescueModel);
      await FetchSystem(rescueModel);
    }

    private void EventBusOnRescueClosed(object sender, Rescue rescue)
    {
      var rescueModels = Rescues.Where(x => x.Rescue == rescue).ToList();
      Rescues.RemoveAll(rescueModels);
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