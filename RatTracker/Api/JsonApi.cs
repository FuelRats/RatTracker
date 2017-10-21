using JsonApiSerializer;
using JsonApiSerializer.JsonApi;
using Newtonsoft.Json;

namespace RatTracker.Api
{
  public static class JsonApi
  {
    public static T Deserialize<T>(string json) where T : class
    {
      var models = JsonConvert.DeserializeObject<T>(json, new JsonApiSerializerSettings());
      return models;
    }

    public static DocumentRoot<T> DeserializeWithMeta<T>(string json) where T : class
    {
      var docRoot = JsonConvert.DeserializeObject<DocumentRoot<T>>(json, new JsonApiSerializerSettings());
      return docRoot;
    }
  }
}