using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RatTracker.Models.Journal;

namespace RatTracker.Journal
{
  public class JournalEntryToSubTypeConverter : JsonConverter
  {
    public override bool CanWrite => false;

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
      throw new NotSupportedException();
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
      var jObject = JObject.Load(reader);
      var eventName = (string) jObject.Property("event");

      var targetType = Type.GetType("RatTracker.Models.Journal." + eventName, false);
      if (targetType == null) { return null; }

      var target = Activator.CreateInstance(targetType);
      serializer.Populate(jObject.CreateReader(), target);

      return target;
    }

    public override bool CanConvert(Type objectType)
    {
      return typeof(JournalEntryBase).IsAssignableFrom(objectType);
    }
  }
}