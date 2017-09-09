using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using RatTracker_WPF.Api;
using RatTracker_WPF.Models.Api.V2;

namespace RatTracker_WPF.Caches
{
  public class Cache
  {
    private readonly ConcurrentDictionary<Guid, Rescue> rescues = new ConcurrentDictionary<Guid, Rescue>();
    private readonly ConcurrentDictionary<Guid, Rat> rats = new ConcurrentDictionary<Guid, Rat>();
    public event EventHandler RescuesReloaded;
    public event EventHandler<Rescue> RescueCreated; 
    public event EventHandler<Rescue> RescueUpdated; 
    public event EventHandler<Rescue> RescueClosed; 

    public void Init(WebsocketResponseHandler responseHandler)
    {
      responseHandler.AddCallback("rescues:read", RescuesRead);
      responseHandler.AddCallback("rescues:created", RescuesCreated);
      responseHandler.AddCallback("rescues:updated", RescuesUpdated);
    }

    private void RescuesUpdated(string message)
    {
      var rescue = JsonApi.Deserialize<Rescue>(message);
      if (rescue.Status== RescueState.Closed)
      {
        RemoveRescue(rescue);
        RescueClosed?.Invoke(this, rescue);
        return;
      }
      
      AddRescue(rescue);
      RescueUpdated?.Invoke(this, rescue);
    }
    
    public IEnumerable<Rescue> GetRescues()
    {
      return rescues.Values.ToList().OrderBy(x => x.Data.BoardIndex);
    }

    public Rescue GetRescue(Guid id)
    {
      var match = rescues.TryGetValue(id, out var rescue);
      return match ? rescue : null;
    }

    public Rat GetRat(Guid id)
    {
      var match = rats.TryGetValue(id, out var rat);
      return match ? rat : null;
    }

    public IEnumerable<Rat> GetRats()
    {
      return rats.Values.ToList();
    }

    private void RescuesCreated(string message)
    {
      var rescue = JsonApi.Deserialize<Rescue>(message);
      AddRescue(rescue);
      RescueCreated?.Invoke(this, rescue);
    }
    
    private void RescuesRead(string message)
    {
      var newRescues = JsonApi.Deserialize<Rescue[]>(message);
      rescues.Clear();
      AddRescues(newRescues);
      RescuesReloaded?.Invoke(this, EventArgs.Empty);
    }

    private void AddRescue(Rescue rescue)
    {
      rescues.AddOrUpdate(rescue.Id, rescue, (guid, oldValue) => rescue);
      AddRats(rescue.Rats);
    }

    private void AddRescues(IEnumerable<Rescue> rescuesToAdd)
    {
      foreach (var rescue in rescuesToAdd)
      {
        AddRescue(rescue);
      }
    }

    private void RemoveRescue(Rescue rescue)
    {
      RemoveRescue(rescue.Id);
    }

    private void AddRat(Rat rat)
    {
      rats.AddOrUpdate(rat.Id, rat, (guid, oldValue) => rat);
    }

    private void AddRats(IEnumerable<Rat> ratsToAdd)
    {
      foreach (var rat in ratsToAdd)
      {
        AddRat(rat);
      }
    }

    private void RemoveRat(Rat rat)
    {
      RemoveRat(rat.Id);
    }

    private void RemoveRats(IEnumerable<Rat> ratsToRemove)
    {
      RemoveRats(ratsToRemove.Select(x => x.Id));
    }

    private void RemoveRescues(IEnumerable<Rescue> rescuesToRemove)
    {
      RemoveRescues(rescuesToRemove.Select(x => x.Id));
    }

    private void RemoveRats(IEnumerable<Guid> ratsToRemove)
    {
      foreach (var ratId in ratsToRemove)
      {
        RemoveRat(ratId);
      }
    }

    private void RemoveRescues(IEnumerable<Guid> rescuesToRemove)
    {
      foreach (var rescueId in rescuesToRemove)
      {
        RemoveRescue(rescueId);
      }
    }

    private void RemoveRescue(Guid rescueId)
    {
      rescues.TryRemove(rescueId, out var unused);
    }

    private void RemoveRat(Guid ratId)
    {
      rats.TryRemove(ratId, out var unused);
    }
  }
}