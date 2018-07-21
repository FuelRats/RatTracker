using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using log4net;
using RatTracker.Api.Fuelrats;
using RatTracker.Infrastructure.Events;
using RatTracker.Infrastructure.Extensions;
using RatTracker.Models.Apis.FuelRats;
using RatTracker.Models.Apis.FuelRats.Rescues;
using RatTracker.Models.App.Rescues;

namespace RatTracker.Api
{
  public class Cache
  {
    private readonly EventBus eventBus;
    private readonly ILog log;
    private readonly ConcurrentDictionary<Guid, Rescue> rescues = new ConcurrentDictionary<Guid, Rescue>();

    public Cache(EventBus eventBus, ILog log)
    {
      this.eventBus = eventBus;
      this.log = log;
      eventBus.ConnectionEstablished += EventBusOnConnectionEstablished;
      eventBus.ProfileLoaded += EventBusOnProfileLoaded;
      eventBus.RescueCreated += EventBusOnRescueCreated;
      eventBus.RescueUpdated += EventBusOnRescueUpdated;
      eventBus.RescueClosed += EventBusOnRescueClosed;
      eventBus.RescuesReloaded += EventBusOnRescuesReloaded;
    }

    public PlayerInfo PlayerInfo { get; } = new PlayerInfo();

    public Rat GetDisplayRatForUser()
    {
      return PlayerInfo?.GetDisplayRat();
    }

    private void EventBusOnConnectionEstablished(object sender, Version version)
    {
      log.Info($"Connected to FuelRats API version '{version}'");

      var profileRequest = WebsocketMessage.Request("users", "profile", ApiEventNames.UserProfile);
      eventBus.PostWebsocketMessage(profileRequest);
    }

    private void EventBusOnProfileLoaded(object sender, User receivedUser)
    {
      PlayerInfo.User = receivedUser;

      var rescuesRequest = WebsocketMessage.Request("rescues", "read", ApiEventNames.RescueRead);
      rescuesRequest.AddData(nameof(Rescue.Status).ToApiName(), WebsocketMessage.Data("$not", RescueState.Closed.ToApiName()));
      eventBus.PostWebsocketMessage(rescuesRequest);
    }

    private void EventBusOnRescuesReloaded(object sender, IEnumerable<Rescue> receivedRescues)
    {
      rescues.Clear();
      foreach (var rescue in receivedRescues)
      {
        AddRescue(rescue);
      }
    }

    private void EventBusOnRescueCreated(object sender, Rescue rescue)
    {
      AddRescue(rescue);
    }

    private void EventBusOnRescueUpdated(object sender, Rescue rescue)
    {
      AddRescue(rescue);
    }

    private void EventBusOnRescueClosed(object sender, Rescue rescue)
    {
      RemoveRescue(rescue);
    }

    private void AddRescue(Rescue rescue)
    {
      rescues.AddOrUpdate(rescue.Id, rescue, (guid, oldValue) => rescue);
    }

    private void RemoveRescue(Rescue rescue)
    {
      RemoveRescue(rescue.Id);
    }

    private void RemoveRescue(Guid rescueId)
    {
      rescues.TryRemove(rescueId, out var unused);
    }
  }
}