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
using ILog = log4net.ILog;

namespace RatTracker.ViewModels
{
  public class AssignedRescueViewModel : Screen
  {
    private readonly ILog log;
    private readonly EventBus eventBus;
    private readonly Cache cache;
    private Rescue assignedRescue;
    private RatState self;

    public AssignedRescueViewModel(ILog log, EventBus eventBus, Cache cache)
    {
      this.log = log;
      this.eventBus = eventBus;
      this.cache = cache;
      Rats = new ObservableCollection<RatState>();
      eventBus.RescueUpdated += EventBusOnRescueUpdated;
      eventBus.RescuesReloaded += EventBusOnRescuesReloaded;
      eventBus.RescueClosed += EventBusOnRescueUpdated;
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

      SendTpaMessage("FriendRequest", ratState.FriendRequest == RequestState.Accepted);
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

      SendTpaMessage("WingRequest", ratState.WingRequest == RequestState.Accepted);
    }

    public void ToggleInSystem(RatState ratState)
    {
      ratState.InSystem = !ratState.InSystem;
      SendTpaMessage("SysArrived", ratState.InSystem);
    }

    public void ToggleBeaconVisible(RatState ratState)
    {
      ratState.Beacon = !ratState.Beacon;
      SendTpaMessage("BeaconSpotted", ratState.Beacon);
    }

    public void ToggleInInstance(RatState ratState)
    {
      ratState.InInstance = !ratState.InInstance;
      SendTpaMessage("InstanceSuccessful", ratState.InInstance);
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
          Rats.AddAll(rescue.Rats.Select(rat => new RatState {Rat = rat}));
        }

        Self = Rats.Single(x => x.Rat == displayRat);
        AssignedRescue = rescue;
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
      tpaMessage.AddData("RatID", Self?.Rat.Id);
      tpaMessage.AddData("RescueID", assignedRescue?.Id);
      tpaMessage.AddData(eventName, value.ToApiName());
      eventBus.PostWebsocketMessage(tpaMessage);
    }
  }
}