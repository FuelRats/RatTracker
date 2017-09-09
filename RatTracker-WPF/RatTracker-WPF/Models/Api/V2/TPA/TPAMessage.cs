using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json.Linq;

namespace RatTracker_WPF.Models.Api.V2.TPA
{
  [SuppressMessage("ReSharper", "InconsistentNaming")] // JS API names....
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

      @event = new[] {action};
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

      @event = new[] {controller, action};
    }

    public IReadOnlyList<string> action => broadcastAction;

    public IReadOnlyList<string> @event { get; }

    public string id { get; set; }

    public JObject data { get; set; }
  }
}