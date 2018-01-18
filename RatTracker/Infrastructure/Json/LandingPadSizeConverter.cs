using System;
using Newtonsoft.Json;

namespace RatTracker.Infrastructure.Json
{
  public class LandingPadSizeConverter : JsonConverter
  {
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
      var value = reader.Value as string;
      if (value?.Length == 1)
      {
        return value[0];
      }

      if (value == null || "None" == value)
      {
        return 'N';
      }

      throw new NotSupportedException($"Cannot handle '{value}' as a landing pad size");
    }

    public override bool CanConvert(Type objectType)
    {
      return objectType == typeof(char);
    }
  }
}