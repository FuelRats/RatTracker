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
    public event EventHandler<Friends> Friends;
    public event EventHandler<WingInvite> WingInvite;
    public event EventHandler<WingJoin> WingJoin;
    public event EventHandler<WingAdd> WingAdd;
    public event EventHandler<WingLeave> WingLeave;

    public void PostJournalEvent(object sender, JournalEntryBase journalEntry)
    {
      sender = sender ?? this;
      switch (journalEntry)
      {
        case FSDJump jump:
          Location?.Invoke(sender, jump);
          break;
        case Location location:
          Location?.Invoke(sender, location);
          break;
        case Friends friends:
          Friends?.Invoke(sender, friends);
          break;
        case WingInvite wingInvite:
          WingInvite?.Invoke(sender, wingInvite);
          break;
        case WingJoin wingJoin:
          WingJoin?.Invoke(sender, wingJoin);
          break;
        case WingAdd wingAdd:
          WingAdd?.Invoke(sender, wingAdd);
          break;
        case WingLeave wingLeave:
          WingLeave?.Invoke(sender, wingLeave);
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