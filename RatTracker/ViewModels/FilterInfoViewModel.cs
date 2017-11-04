using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using Caliburn.Micro;
using RatTracker.Api.StarSystems;
using RatTracker.Infrastructure.Events;
using RatTracker.Models.App.Rescues;

namespace RatTracker.ViewModels
{
  public class FilterInfoViewModel : Screen
  {
    private readonly SystemApi systemApi;
    private readonly EventBus eventBus;
    private char maxLandingPadSize;
    private RescueModel rescueModel;
    private bool showOnlyPcCases;
    private bool showOnlyActiveCases;

    public FilterInfoViewModel(SystemApi systemApi, EventBus eventBus)
    {
      this.systemApi = systemApi;
      this.eventBus = eventBus;
      LandingPadSizes = new ObservableCollection<char> {'L', 'M'};
      MaxLandingPadSize = LandingPadSizes.First();
      eventBus.Rescues.SelectedRescueChanged += RescuesOnSelectedRescueChanged;
    }

    public ObservableCollection<char> LandingPadSizes { get; }

    public char MaxLandingPadSize
    {
      get => maxLandingPadSize;
      set
      {
        maxLandingPadSize = value;
        NotifyOfPropertyChange();
      }
    }

    public RescueModel RescueModel
    {
      get => rescueModel;
      set
      {
        rescueModel = value;
        NotifyOfPropertyChange();
      }
    }

    public bool ShowOnlyPCCases
    {
      get => showOnlyPcCases;
      set
      {
        showOnlyPcCases = value;
        NotifyOfPropertyChange();
        UpdateFilter();
      }
    }

    public bool ShowOnlyActiveCases
    {
      get => showOnlyActiveCases;
      set
      {
        showOnlyActiveCases = value;
        NotifyOfPropertyChange();
        UpdateFilter();
      }
    }

    public async void FindNearestStation()
    {
      var rescue = RescueModel;
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

    public void CopyNearestSystemName()
    {
      if (RescueModel?.NearestSystem == null) { return; }
      Clipboard.SetText(RescueModel.NearestSystem.Name);
    }

    public void CopyNearestStationName()
    {
      if (RescueModel?.NearestStation == null) { return; }
      Clipboard.SetText(RescueModel.NearestStation.Name);
    }

    private void UpdateFilter()
    {
      var filter = RescueFilter.None;
      if (ShowOnlyPCCases)
      {
        filter |= RescueFilter.PC;
      }

      if (ShowOnlyActiveCases)
      {
        filter |= RescueFilter.Active;
      }

      eventBus.Rescues.PostFilterChanged(this, filter);
    }

    private void RescuesOnSelectedRescueChanged(object sender, RescueModel rescue)
    {
      RescueModel = rescue;
    }
  }
}