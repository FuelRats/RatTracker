using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using RatTracker_WPF.Models.Api.V2;

namespace RatTracker_WPF.Caches
{
  public class Cache
  {
    private readonly ConcurrentDictionary<Guid, Rescue> rescues = new ConcurrentDictionary<Guid, Rescue>();
    private readonly ConcurrentDictionary<Guid, Rat> rats = new ConcurrentDictionary<Guid, Rat>();

    public void AddRescue(Rescue rescue)
    {
      rescues.AddOrUpdate(rescue.Id, rescue, (guid, oldValue) => rescue);
      AddRats(rescue.Rats);
    }

    public void AddRescues(IEnumerable<Rescue> rescuesToAdd)
    {
      foreach (var rescue in rescuesToAdd)
      {
        AddRescue(rescue);
      }
    }

    public void RemoveRescue(Rescue rescue)
    {
      RemoveRescue(rescue.Id);
    }

    public void AddRat(Rat rat)
    {
      rats.AddOrUpdate(rat.Id, rat, (guid, oldValue) => rat);
    }

    public void AddRats(IEnumerable<Rat> ratsToAdd)
    {
      foreach (var rat in ratsToAdd)
      {
        AddRat(rat);
      }
    }

    public void RemoveRat(Rat rat)
    {
      RemoveRat(rat.Id);
    }

    public void RemoveRats(IEnumerable<Rat> ratsToRemove)
    {
      RemoveRats(ratsToRemove.Select(x => x.Id));
    }

    public void RemoveRescues(IEnumerable<Rescue> rescuesToRemove)
    {
      RemoveRescues(rescuesToRemove.Select(x => x.Id));
    }

    public void RemoveRats(IEnumerable<Guid> ratsToRemove)
    {
      foreach (var ratId in ratsToRemove)
      {
        RemoveRat(ratId);
      }
    }

    public void RemoveRescues(IEnumerable<Guid> rescuesToRemove)
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