using Caliburn.Micro;
using RatTracker.Api;
using RatTracker.Infrastructure.Events;
using RatTracker.Models.App.Rescues;
using RatTracker.Models.Journal;

namespace RatTracker.ViewModels
{
  public class PlayerInformationViewModel : Screen
  {
    private PlayerInfo playerInfo;

    public PlayerInformationViewModel(EventBus eventBus, Cache cache)
    {
      PlayerInfo = cache.PlayerInfo;
      eventBus.Journal.Location += JournalOnLocation;
    }

    public PlayerInfo PlayerInfo
    {
      get => playerInfo;
      set
      {
        playerInfo = value;
        NotifyOfPropertyChange();
      }
    }

    private void JournalOnLocation(object sender, Location location)
    {
      PlayerInfo.CurrentSystem = new SystemInfo { Name = location.SystemName, X = location.Coordinates[0], Y = location.Coordinates[1], Z = location.Coordinates[2] };
    }
  }
}