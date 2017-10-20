using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Threading;
using Newtonsoft.Json.Linq;
using RatTracker_WPF.Api;
using RatTracker_WPF.Caches;
using RatTracker_WPF.Infrastructure;
using RatTracker_WPF.Infrastructure.Extensions;
using RatTracker_WPF.Models.Api.V2;
using RatTracker_WPF.Models.Api.V2.TPA;
using RatTracker_WPF.Models.App;

namespace RatTracker_WPF.ViewModels
{
  public class AssignedRescueViewModel : PropertyChangedBase
  {
    private readonly ApiWorker apiWorker;
    private readonly Cache cache;
    private string clientName;
    private string systemName;
    private Rescue assignedRescue;

    public AssignedRescueViewModel(ApiWorker apiWorker, Cache cache)
    {
      this.apiWorker = apiWorker;
      this.cache = cache;
      cache.RescueUpdated += RescueUpdated;
      Rats = new ObservableCollection<RatState>();
    }

    public ObservableCollection<RatState> Rats { get; }

    public string ClientName
    {
      get => AssignedRescue?.Client;
      set => AssignedRescue.Client = value;
    }

    public string SystemName
    {
      get => AssignedRescue?.System;
      set => AssignedRescue.System = value;
    }

    public Rescue AssignedRescue
    {
      get => assignedRescue;
      private set
      {
        assignedRescue = value;
        NotifyPropertyChanged();
      }
    }

    public RatState Self { get; private set; }

    public void SetClientName()
    {
    }

    public void SetSystemName()
    {
    }

    public void ToggleFriendRequest(RatState ratState)
    {
      switch (ratState.FriendRequest)
      {
        case RequestState.NotRecieved:
          ratState.FriendRequest = RequestState.Recieved;
          break;
        case RequestState.Recieved:
          ratState.FriendRequest = RequestState.Accepted;
          break;
        case RequestState.Accepted:
          ratState.FriendRequest = RequestState.NotRecieved;
          break;
        default:
          throw new ArgumentOutOfRangeException();
      }

      SendTpaMessage("FriendRequest", "update", ratState.FriendRequest == RequestState.Accepted);
    }

    public void ToggleWingRequest(RatState ratState)
    {
      switch (ratState.WingRequest)
      {
        case RequestState.NotRecieved:
          ratState.WingRequest = RequestState.Recieved;
          break;
        case RequestState.Recieved:
          ratState.WingRequest = RequestState.Accepted;
          break;
        case RequestState.Accepted:
          ratState.WingRequest = RequestState.NotRecieved;
          break;
        default:
          throw new ArgumentOutOfRangeException();
      }

      SendTpaMessage("WingRequest", "update", ratState.WingRequest == RequestState.Accepted);
    }

    public void ToggleInSystem(RatState ratState)
    {
      ratState.InSystem = !ratState.InSystem;
      SendTpaMessage("SysArrived", "update", ratState.InSystem);
    }

    public void ToggleBeaconVisible(RatState ratState)
    {
      ratState.Beacon = !ratState.Beacon;
      SendTpaMessage("BeaconSpotted", "update", ratState.Beacon);
    }

    public void ToggleInInstance(RatState ratState)
    {
      ratState.InInstance = !ratState.InInstance;
      SendTpaMessage("InstanceSuccessful", "update", ratState.InInstance);
    }

    private void RescueUpdated(object sender, Rescue rescue)
    {
      // if my rat assigned
      var displayRat = cache.PlayerInfo.GetDisplayRat();
      if (rescue.Rats.Contains(displayRat))
      {
        if (assignedRescue != null && assignedRescue.Id != rescue.Id && assignedRescue.Status != RescueState.Closed)
        {
          DialogHelper.ShowWarning("There is already an open rescue assigned to you. Keeping current rescue.");
          return;
        }

        if (assignedRescue?.Id == rescue.Id)
        {
        }

        AssignedRescue = rescue;
        Rats.AddAll(rescue.Rats.Select(rat => new RatState {Rat = rat}));
        Self = Rats.Single(x => x.Rat == displayRat);
      }
      else
      {
        // if id = assignedrescue.id
        if (rescue.Id == assignedRescue?.Id)
        {
          // -> I was unassigned
          AssignedRescue = null;
          Rats.Clear();
        }
      }
    }
    
    private void SendTpaMessage(string controller, string action, bool value)
    {
      var frmsg = new TpaMessage(controller, action)
      {
        Data = new JObject()
      };
      frmsg.Data.Add("RatID", Self?.Rat.Id);
      frmsg.Data.Add("RescueID", assignedRescue?.Id);
      frmsg.Data.Add(controller, value.ToApiName());

      //apiWorker.SendTpaMessage(frmsg);
    }
  }
}