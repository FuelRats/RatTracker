using System;
using Newtonsoft.Json;
using RatTracker.Models.Apis.FuelRats.Rescues;

namespace RatTracker.Infrastructure.Json
{
  public class PlatformConverter : JsonConverter
  {
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
      var value = reader.Value as string;
      switch (value)
      {
        case "pc":
          return Platform.Pc;
        case "xb":
          return Platform.Xb;
        case "ps":
          return Platform.Ps;
        case null:
          return Platform.Unknown;
        default:
          throw new NotSupportedException($"Cannot handle '{value}' as a platform");
      }
    }

    public override bool CanConvert(Type objectType)
    {
      return objectType == typeof(char);
    }
  }
}