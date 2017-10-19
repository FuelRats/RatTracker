using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using RatTracker_WPF.Api;
using RatTracker_WPF.Caches;
using RatTracker_WPF.Infrastructure;
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
    private RatState self;
    private RatState rat1;
    private RatState rat2;
    private RatState rat3;

    public AssignedRescueViewModel(ApiWorker apiWorker, Cache cache)
    {
      this.apiWorker = apiWorker;
      this.cache = cache;
      cache.RescueUpdated += RescueUpdated;
      ClientName = "TestClient";
      SystemName= "TestSystem";
      Rat1 = new RatState{Rat = new Rat{Name = "Rat1"}, InSystem = true};
      Rat2 = new RatState{Rat = new Rat{Name = "Rat2"}, WingRequest = RequestState.Recieved};
      Rat3 = new RatState{Rat = new Rat{Name = "Rat3 -  longer name"}, Beacon = true};
    }

    private void RescueUpdated(object sender, Rescue rescue)
    {
      if (rescue.Rats.Any(x => x.Id == cache.PlayerInfo.GetDisplayRat().Id))
      {
        assignedRescue = rescue;
      }
    }
    
    public string ClientName
    {
      get => clientName;
      set
      {
        clientName = value;
        NotifyPropertyChanged();
      }
    }

    public string SystemName
    {
      get => systemName;
      set
      {
        systemName = value;
        NotifyPropertyChanged();
      }
    }

    public RatState Rat1
    {
      get => rat1;
      set
      {
        rat1 = value;
        NotifyPropertyChanged();
      }
    }

    public RatState Rat2
    {
      get => rat2;
      set
      {
        rat2 = value;
        NotifyPropertyChanged();
      }
    }

    public RatState Rat3
    {
      get => rat3;
      set
      {
        rat3 = value;
        NotifyPropertyChanged();
      }
    }

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
      SendTpaMessage("InstanceSuccessful","update", ratState.InInstance);
    }

    private void SendTpaMessage(string controller, string action, bool value)
    {
      var frmsg = new TpaMessage(controller, action)
      {
        Data = new JObject()
      };
      frmsg.Data.Add("RatID", self?.Rat.Id);
      frmsg.Data.Add("RescueID", assignedRescue?.Id);
      frmsg.Data.Add(controller, value.ToApiName());

      //apiWorker.SendTpaMessage(frmsg);
    }
  }
}