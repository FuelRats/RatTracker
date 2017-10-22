using System;
using System.Collections.ObjectModel;
using Caliburn.Micro;
using Newtonsoft.Json.Linq;
using RatTracker.Infrastructure.Extensions;
using RatTracker.Models.Api;
using RatTracker.Models.Api.Rescues;
using RatTracker.Models.Api.TPA;
using RatTracker.Models.App;

namespace RatTracker.ViewModels
{
  public class AssignedRescueViewModel : Screen
  {
    private Rescue assignedRescue;

    public AssignedRescueViewModel()
    {
      //  this.cache = cache;
      //  cache.RescueUpdated += RescueUpdated;
      Rats = new ObservableCollection<RatState>();
      Rats.Add(new RatState {Rat = new Rat {Name = "Rat1"}, FriendRequest = RequestState.Recieved});
      Rats.Add(new RatState {Rat = new Rat {Name = "Rat2"}, WingRequest = RequestState.Accepted});
      Rats.Add(new RatState {Rat = new Rat {Name = "Rat3 - longer name"}, InSystem = true});
      Self = Rats[0];
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
        NotifyOfPropertyChange();
      }
    }

    public RatState Self { get; }

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
      //var displayRat = cache.PlayerInfo.GetDisplayRat();
      //if (rescue.Rats.Contains(displayRat))
      //{
      //  if (assignedRescue != null && assignedRescue.Id != rescue.Id && assignedRescue.Status != RescueState.Closed)
      //  {
      //    DialogHelper.ShowWarning("There is already an open rescue assigned to you. Keeping current rescue.");
      //    return;
      //  }

      //  if (assignedRescue?.Id == rescue.Id)
      //  {
      //  }

      //  AssignedRescue = rescue;
      //  Rats.AddAll(rescue.Rats.Select(rat => new RatState {Rat = rat}));
      //  Self = Rats.Single(x => x.Rat == displayRat);
      //}
      //else
      //{
      //  // if id = assignedrescue.id
      //  if (rescue.Id == assignedRescue?.Id)
      //  {
      //    // -> I was unassigned
      //    AssignedRescue = null;
      //    Rats.Clear();
      //  }
      //}
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