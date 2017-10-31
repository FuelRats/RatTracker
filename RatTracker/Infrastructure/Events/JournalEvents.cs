using System;
using log4net;
using RatTracker.Models.Journal;

namespace RatTracker.Infrastructure.Events
{
  public class JournalEvents
  {
    private readonly ILog log;

    public JournalEvents(ILog log)
    {
      this.log = log;
    }

    public event EventHandler<Location> Location;
    public event EventHandler<FSDJump> Jump;

    public void PostJournalEvent(object sender, JournalEntryBase journalEntry)
    {
      sender = sender ?? this;
      switch (journalEntry)
      {
        case Location location:
          Location?.Invoke(sender, location);
          break;
        case FSDJump jump:
          Jump?.Invoke(sender, jump);
          break;
        case null:
          break;
        default:
          log.Warn($"Unmapped journal event '{journalEntry.GetType().Name}'");
          break;
      }
    }
  }
}