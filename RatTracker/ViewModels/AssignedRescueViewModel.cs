using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Caliburn.Micro;
using RatTracker.Api;
using RatTracker.Api.Fuelrats;
using RatTracker.Infrastructure.Events;
using RatTracker.Infrastructure.Extensions;
using RatTracker.Models.Apis.FuelRats.Rescues;
using RatTracker.Models.App.Rescues;
using RatTracker.Models.Journal;
using ILog = log4net.ILog;

namespace RatTracker.ViewModels
{
  public class AssignedRescueViewModel : Screen
  {
    private readonly ILog log;

    //private readonly EventBus eventBus;
    private readonly Cache cache;
    private readonly IList<string> friendsList;
    private Rescue assignedRescue;
    private RatState self;

    public AssignedRescueViewModel(ILog log, EventBus eventBus, Cache cache)
    {
      this.log = log;
      //this.eventBus = eventBus;
      this.cache = cache;
      friendsList = new List<string>();
      Rats = new ObservableCollection<RatState>();
      eventBus.RescueUpdated += EventBusOnRescueUpdated;
      eventBus.RescuesReloaded += EventBusOnRescuesReloaded;
      eventBus.RescueClosed += EventBusOnRescueUpdated;
      eventBus.Journal.Friends += JournalOnFriends;
      eventBus.Journal.WingInvite += JournalOnWingInvite;
      eventBus.Journal.WingJoin += JournalOnWingJoin;
      eventBus.Journal.WingAdd += JournalOnWingAdd;
      eventBus.Journal.WingLeave += JournalOnWingLeave;
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
        NotifyOfPropertyChange(nameof(ClientName));
        NotifyOfPropertyChange(nameof(SystemName));
      }
    }

    public RatState Self
    {
      get => self;
      private set
      {
        self = value;
        NotifyOfPropertyChange();
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
      RequestState friendRequest;
      switch (ratState.FriendRequest)
      {
        case RequestState.NotRecieved:
          friendRequest = RequestState.Recieved;
          break;
        case RequestState.Recieved:
          friendRequest = RequestState.Accepted;
          break;
        case RequestState.Accepted:
          friendRequest = RequestState.NotRecieved;
          break;
        default:
          throw new ArgumentOutOfRangeException();
      }

      SetFriendRequestState(ratState, friendRequest);
    }

    public void ToggleWingRequest(RatState ratState)
    {
      RequestState wingRequest;
      switch (ratState.WingRequest)
      {
        case RequestState.NotRecieved:
          wingRequest = RequestState.Recieved;
          break;
        case RequestState.Recieved:
          wingRequest = RequestState.Accepted;
          break;
        case RequestState.Accepted:
          wingRequest = RequestState.NotRecieved;
          break;
        default:
          throw new ArgumentOutOfRangeException();
      }

      SetWingRequestState(ratState, wingRequest);
    }

    public void ToggleInSystem(RatState ratState, bool? inSystem = null)
    {
      ratState.InSystem = inSystem ?? !ratState.InSystem;
      SendTpaMessage(TpaEventNames.InSystem, ratState.InSystem);
    }

    public void ToggleBeaconVisible(RatState ratState, bool? beaconVisible = null)
    {
      ratState.Beacon = beaconVisible ?? !ratState.Beacon;
      SendTpaMessage(TpaEventNames.BeaconVisible, ratState.Beacon);
    }

    public void ToggleInInstance(RatState ratState, bool? inInstance = null)
    {
      ratState.InInstance = inInstance ?? !ratState.InInstance;
      SendTpaMessage(TpaEventNames.InInstance, ratState.InInstance);
    }

    private void SetFriendRequestState(RatState ratState, RequestState friendRequest)
    {
      ratState.FriendRequest = friendRequest;
      SendTpaMessage(TpaEventNames.FriendRequest, friendRequest == RequestState.Accepted);
    }

    private void SetWingRequestState(RatState ratState, RequestState wingRequest)
    {
      ratState.WingRequest = wingRequest;
      SendTpaMessage(TpaEventNames.WingRequest, wingRequest == RequestState.Accepted);
    }

    private void JournalOnFriends(object sender, Friends friends)
    {
      var isFriend = friends.Status == "Online" || friends.Status == "Added";
      if (isFriend)
      {
        friendsList.Add(friends.Name);
      }

      if (friends.Name == AssignedRescue?.Client)
      {
        SetFriendRequestState(Self, isFriend ? RequestState.Accepted : RequestState.NotRecieved);
      }
    }

    private void JournalOnWingInvite(object sender, WingInvite wingInvite)
    {
      if (wingInvite.Name == assignedRescue?.Client)
      {
        SetWingRequestState(self, RequestState.Recieved);
      }
    }

    private void JournalOnWingJoin(object sender, WingJoin wingJoin)
    {
      // TODO do we want to actively track status of other rats (to display their state when they don't sue RT)?
      if (wingJoin.Others.Contains(assignedRescue?.Client))
      {
        SetWingRequestState(self, RequestState.Accepted);
      }
    }

    private void JournalOnWingAdd(object sender, WingAdd wingAdd)
    {
      // TODO do we want to actively track status of other rats (to display their state when they don't sue RT)?
      if (wingAdd.Name == assignedRescue?.Client)
      {
        SetWingRequestState(self, RequestState.Accepted);
      }
    }

    private void JournalOnWingLeave(object sender, WingLeave wingLeave)
    {
      if (AssignedRescue == null) { return; }

      SetWingRequestState(self, RequestState.NotRecieved);
    }

    private void EventBusOnRescuesReloaded(object sender, IEnumerable<Rescue> rescues)
    {
      var displayRat = cache.GetDisplayRatForUser();
      var assignedRescues = rescues.Where(x => x.Rats.Contains(displayRat));
      foreach (var rescue in assignedRescues)
      {
        EventBusOnRescueUpdated(sender, rescue);
      }
    }

    private void EventBusOnRescueUpdated(object sender, Rescue rescue)
    {
      var displayRat = cache.GetDisplayRatForUser();
      if (displayRat == null) { return; }

      if (rescue.Rats.Contains(displayRat))
      {
        if (assignedRescue != null && assignedRescue.Id != rescue.Id && assignedRescue.Status != RescueState.Closed)
        {
          log.Debug("There is already an open rescue assigned to you. Keeping current rescue.");
          return;
        }

        if (assignedRescue?.Id == rescue.Id)
        {
          var ratsRemovedFromRescue = Rats.Select(x => x.Rat).Except(rescue.Rats).ToList();
          var ratStatesToRemove = Rats.Where(x => ratsRemovedFromRescue.Contains(x.Rat));
          Rats.RemoveAll(ratStatesToRemove);

          foreach (var rat in rescue.Rats)
          {
            var ratState = Rats.SingleOrDefault(x => x.Rat == rat) ?? new RatState();
            ratState.Rat = rat;
          }
        }
        else
        {
          Rats.Clear();
          Rats.AddAll(rescue.Rats.Select(rat => new RatState { Rat = rat }));
        }

        Self = Rats.Single(x => x.Rat == displayRat);
        AssignedRescue = rescue;
        if (friendsList.Contains(AssignedRescue.Client))
        {
          SetFriendRequestState(Self, RequestState.Accepted);
        }
      }
      else
      {
        if (rescue.Id == assignedRescue?.Id)
        {
          AssignedRescue = null;
          Rats.Clear();
          Self = null;
        }
      }
    }

    private void SendTpaMessage(string eventName, bool value)
    {
      var tpaMessage = WebsocketMessage.CreateTpaMessage(eventName);
      tpaMessage.AddData("RatID", Self.Rat.Id);
      tpaMessage.AddData("RescueID", assignedRescue.Id);
      tpaMessage.AddData(eventName, value.ToApiName());
      // TODO check Mecha interactions eventBus.PostWebsocketMessage(tpaMessage);
    }
  }
}