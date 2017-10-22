using System;
using System.Collections.Generic;
using System.Linq;

namespace RatTracker.Api
{
  public class WebsocketMessage
  {
    private readonly IDictionary<string, object> queryData = new Dictionary<string, object>();

    private WebsocketMessage()
    {
    }

    public static WebsocketMessage Request(string controller, string action, string eventName)
    {
      var query = new WebsocketMessage();
      if (string.IsNullOrWhiteSpace(controller))
      {
        throw new ArgumentNullException(nameof(controller));
      }

      if (string.IsNullOrWhiteSpace(action))
      {
        throw new ArgumentNullException(nameof(action));
      }

      query.AddData("action", new[] { controller, action });
      query.AddData("meta", Data("event", eventName));
      return query;
    }

    public static WebsocketMessage Data(string key, object data)
    {
      var query = new WebsocketMessage();
      query.AddData(key, data);
      return query;
    }

    public WebsocketMessage AddData(string key, object data)
    {
      queryData[key] = data;
      return this;
    }

    public IDictionary<string, object> GetData()
    {
      foreach (var keyValuePair in queryData.ToDictionary(pair => pair.Key, pair => pair.Value))
      {
        var key = keyValuePair.Key;
        var query = keyValuePair.Value as WebsocketMessage;
        if (query == null)
        {
          continue;
        }

        queryData[key] = query.GetData();
      }

      return queryData;
    }
  }
}