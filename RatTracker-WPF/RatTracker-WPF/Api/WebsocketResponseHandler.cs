using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using log4net;
using Newtonsoft.Json;

namespace RatTracker_WPF.Api
{
  public class WebsocketResponseHandler
  {
    private static readonly ILog Logger = LogManager.GetLogger(Assembly.GetCallingAssembly().GetName().Name);

    private readonly ConcurrentDictionary<string, ConcurrentBag<Callback>> allCallbacks =
      new ConcurrentDictionary<string, ConcurrentBag<Callback>>();

    public void AddCallback(string action, Action<string> callback, bool continueOnError = true)
    {
      var bag = allCallbacks.GetOrAdd(action, s => new ConcurrentBag<Callback>());
      bag.Add(new Callback(callback, continueOnError));
    }

    /// <summary>
    ///   Handles received messages from websocket.
    /// </summary>
    /// <param name="message">The message received from WebSocket</param>
    public void MessageReceived(string message)
    {
      try
      {
        //logger.Debug("Raw JSON from WS: " + e.Message);
        dynamic data = JsonConvert.DeserializeObject(message);
        var meta = data.meta;
        if (data.code == 400)
        {
          Logger.Fatal(data);
        }

        if (meta?.Action != null)
        {
          string action = ParseAction(meta.Action);
          Logger.Debug($"Received ws message with action '{action}'");
          if (allCallbacks.TryGetValue(action, out ConcurrentBag<Callback> callbackBag))
          {
            var callbacks = callbackBag.ToArray();
            foreach (var callback in callbacks)
            {
              try
              {
                callback.CallbackAction(message);
              }
              catch (Exception e)
              {
                HandleException(e, $"Error calling callback for action '{action}'");
                if (!callback.ContinueOnError)
                {
                  throw;
                }
              }
            }
          }
        }
      }
      catch (Exception ex)
      {
        HandleException(ex, "Exception in WSClient_MessageReceived: " + ex.Message);
      }
    }

    private static string ParseAction(dynamic action)
    {
      string value;
      if (action is string[])
      {
        value = string.Join(":", action);
      }
      else
      {
        value = action.ToString();
      }

      return value;
    }

    private static void HandleException(Exception ex, string logMessage)
    {
      Logger.Fatal(logMessage, ex);
#if DEBUG
      if (Debugger.IsAttached)
      {
        Debugger.Break();
      }
#endif
    }

    private class Callback
    {
      public Callback(Action<string> callbackAction, bool continueOnError)
      {
        CallbackAction = callbackAction;
        ContinueOnError = continueOnError;
      }

      public Action<string> CallbackAction { get; }
      public bool ContinueOnError { get; }
    }
  }
}