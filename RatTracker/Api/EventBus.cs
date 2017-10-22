using System;
using System.Collections.Generic;
using Caliburn.Micro;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RatTracker.Models.Api;
using RatTracker.Models.Api.Rescues;
using ILog = log4net.ILog;

namespace RatTracker.Api
{
  public class EventBus
  {
    private readonly ILog log;
    private readonly WebsocketHandler websocketHandler;

    public EventBus(ILog log, WebsocketHandler websocketHandler)
    {
      this.log = log;
      this.websocketHandler = websocketHandler;
      websocketHandler.MessageReceived += WebsocketHandlerOnMessageReceived;
    }

    public event EventHandler<Version> ConnectionEstablished;
    public event EventHandler<User> ProfileLoaded;
    public event EventHandler<IEnumerable<Rescue>> RescuesReloaded;
    public event EventHandler<Rescue> RescueCreated;
    public event EventHandler<Rescue> RescueUpdated;
    public event EventHandler<Rescue> RescueClosed;

    public void PostRequest(WebsocketMessage websocketMessage)
    {
      websocketHandler.SendQuery(websocketMessage);
    }

    private void WebsocketHandlerOnMessageReceived(object sender, string message)
    {
      try
      {
        //logger.Debug("Raw JSON from WS: " + e.Message);
        dynamic data = JsonConvert.DeserializeObject(message);
        if (data.code >= 400)
        {
          log.Fatal($"Error on websocket: {data.code} - {data.title} - {data.status} - {data.detail}");
          return;
        }

        if (data.meta is JObject meta && meta.TryGetValue("event", out var metaEvent))
        {
          var eventName = metaEvent.Value<string>();
          log.Debug($"Received ws message with event '{eventName}'");
          switch (eventName)
          {
            case ApiEvents.Connection:
              if (data.result?.attributes is JObject welcome && welcome.TryGetValue("version", out var versionString))
              {
                Version.TryParse(versionString.Value<string>(), out var version);
                Invoke(ConnectionEstablished, version);
              }

              break;
            case ApiEvents.StreamSubscribe:
              break;
            case ApiEvents.UserProfile:
              var user = JsonApi.Deserialize<User>(message);
              ProfileLoaded?.Invoke(this, user);
              break;
            case ApiEvents.RescueCreated:
              var rescues = JsonApi.Deserialize<Rescue[]>(message);
              foreach (var rescue in rescues)
              {
                Invoke(RescueCreated, rescue);
              }

              break;
            case ApiEvents.RescueUpdated:
              rescues = JsonApi.Deserialize<Rescue[]>(message);
              foreach (var rescue in rescues)
              {
                if (rescue.Status == RescueState.Closed)
                {
                  Invoke(RescueClosed, rescue);
                  return;
                }

                Invoke(RescueUpdated, rescue);
              }

              break;
            case ApiEvents.RescueRead:
              rescues = JsonApi.Deserialize<Rescue[]>(message);
              Invoke(RescuesReloaded, rescues);
              break;
            default:
              log.Debug($"Received unmapped message: event '{eventName}', message '{message}'");
              break;
          }
        }
      }
      catch (Exception ex)
      {
        log.Fatal("Exception in WSClient_MessageReceived: " + ex.Message, ex);
      }
    }

    private void Invoke<T>(EventHandler<T> eventHandler, T value)
    {
      Execute.OnUIThreadAsync(() => eventHandler?.Invoke(this, value));
    }
  }
}