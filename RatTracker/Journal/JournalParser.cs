using System.Collections.Generic;
using Newtonsoft.Json;
using RatTracker.Infrastructure.Events;
using RatTracker.Models.Journal;

namespace RatTracker.Journal
{
  public class JournalParser
  {
    private readonly EventBus eventBus;
    private readonly JsonSerializerSettings jsonSerializerSettings;

    public JournalParser(EventBus eventBus)
    {
      this.eventBus = eventBus;
      jsonSerializerSettings = new JsonSerializerSettings
      {
        Converters = new List<JsonConverter>
        {
          new JournalEntryToSubTypeConverter()
        }
      };
    }

    public void Parse(string line)
    {
      var journalEntry = JsonConvert.DeserializeObject<JournalEntryBase>(line, jsonSerializerSettings);
      eventBus.Journal.PostJournalEvent(this, journalEntry);
    }
  }
}