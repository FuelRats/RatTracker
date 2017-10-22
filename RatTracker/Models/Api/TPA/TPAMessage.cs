using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RatTracker.Properties;

namespace RatTracker.Models.Api.TPA
{
  public class TpaMessage
  {
    private static readonly IReadOnlyList<string> broadcastAction = new[] {"stream", "broadcast"};

    public TpaMessage(string action)
    {
      if (string.IsNullOrWhiteSpace(action))
      {
        throw new ArgumentNullException(nameof(action));
      }

      if (action.Contains(":"))
      {
        throw new ArgumentException("Action must not have a ':'", nameof(action));
      }

      Event = new[] {action};
    }

    public TpaMessage(string controller, string action)
    {
      if (string.IsNullOrWhiteSpace(controller))
      {
        throw new ArgumentNullException(nameof(controller));
      }

      if (string.IsNullOrWhiteSpace(action))
      {
        throw new ArgumentNullException(nameof(action));
      }

      Event = new[] {controller, action};
    }

    [JsonProperty(PropertyName = "action")]
    public IReadOnlyList<string> Action => broadcastAction;

    [JsonProperty(PropertyName = "event")]
    public IReadOnlyList<string> Event { get; }

    [JsonProperty(PropertyName = "id")]
    public string Id { get; set; } //= Settings.Default.AppID;

    [JsonProperty(PropertyName = "data")]
    public JObject Data { get; set; }
  }
}