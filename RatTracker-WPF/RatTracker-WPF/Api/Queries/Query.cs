using System;
using System.Collections.Generic;
using System.Linq;

namespace RatTracker_WPF.Api.Queries
{
  public class Query
  {
    private readonly IDictionary<string, object> queryData = new Dictionary<string, object>();

    private Query()
    {
    }

    public static Query Request(string controller, string action)
    {
      var query = new Query();
      if (string.IsNullOrWhiteSpace(controller))
      {
        throw new ArgumentNullException(nameof(controller));
      }

      if (string.IsNullOrWhiteSpace(action))
      {
        throw new ArgumentNullException(nameof(action));
      }

      query.AddData("action", new[] {controller, action});
      return query;
    }

    public static Query Data(string key, object data)
    {
      var query = new Query();
      query.AddData(key, data);
      return query;
    }

    public Query AddData(string key, object data)
    {
      queryData[key] = data;
      return this;
    }

    public IDictionary<string, object> GetData()
    {
      foreach (var keyValuePair in queryData.ToDictionary(pair => pair.Key, pair => pair.Value))
      {
        var key = keyValuePair.Key;
        var query = keyValuePair.Value as Query;
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