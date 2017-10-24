using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using RatTracker.Infrastructure.Extensions;
using RatTracker.Models.Api;
using RatTracker.Models.Api.Rescues;
using RatTracker.Models.App.Rescues;

namespace RatTracker.Api
{
  public class Cache
  {
    private readonly EventBus eventBus;
    private readonly ConcurrentDictionary<Guid, Rescue> rescues = new ConcurrentDictionary<Guid, Rescue>();
    private readonly PlayerInfo playerInfo = new PlayerInfo();

    public Cache(EventBus eventBus)
    {
      this.eventBus = eventBus;
      eventBus.ConnectionEstablished += EventBusOnConnectionEstablished;
      eventBus.ProfileLoaded += EventBusOnProfileLoaded;
      eventBus.RescueCreated += EventBusOnRescueCreated;
      eventBus.RescueUpdated += EventBusOnRescueUpdated;
      eventBus.RescueClosed += EventBusOnRescueClosed;
      eventBus.RescuesReloaded += EventBusOnRescuesReloaded;
    }

    public IEnumerable<Rescue> GetRescues()
    {
      return rescues.Values.ToList().OrderBy(x => x.Data.BoardIndex);
    }

    public Rat GetDisplayRatForUser()
    {
      return playerInfo?.GetDisplayRat();
    }

    private void EventBusOnConnectionEstablished(object sender, Version version)
    {
      // Validate version
      var profileRequest = WebsocketMessage.Request("users", "profile", ApiEvents.UserProfile);
      eventBus.PostWebsocketMessage(profileRequest);

      var rescuesRequest = WebsocketMessage.Request("rescues", "read", ApiEvents.RescueRead);
      rescuesRequest.AddData(nameof(Rescue.Status).ToApiName(), WebsocketMessage.Data("$not", RescueState.Closed.ToApiName()));
      eventBus.PostWebsocketMessage(rescuesRequest);
    }

    private void EventBusOnProfileLoaded(object sender, User receivedUser)
    {
      playerInfo.User = receivedUser;
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