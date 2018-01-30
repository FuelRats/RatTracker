using System;
using System.Collections.Generic;
using Caliburn.Micro;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RatTracker.Api;
using RatTracker.Api.Fuelrats;
using RatTracker.Models.Apis.FuelRats;
using RatTracker.Models.Apis.FuelRats.Rescues;
using ILog = log4net.ILog;

namespace RatTracker.Infrastructure.Events
{
  public class EventBus
  {
    private readonly ILog log;
    private readonly WebsocketHandler websocketHandler;

    public EventBus(ILog log, WebsocketHandler websocketHandler, JournalEvents journalEvents, RescueEvents rescueEvents)
    {
      this.log = log;
      this.websocketHandler = websocketHandler;
      Journal = journalEvents;
      Rescues = rescueEvents;
      websocketHandler.MessageReceived += WebsocketHandlerOnMessageReceived;
    }

    public JournalEvents Journal { get; }

    public RescueEvents Rescues { get; }

    public event EventHandler ApplicationExit;
    public event EventHandler SettingsChanged;
    public event EventHandler<Version> ConnectionEstablished;
    public event EventHandler<User> ProfileLoaded;
    public event EventHandler<IEnumerable<Rescue>> RescuesReloaded;
    public event EventHandler<Rescue> RescueCreated;
    public event EventHandler<Rescue> RescueUpdated;
    public event EventHandler<Rescue> RescueClosed;
    public event EventHandler<dynamic> ApiError;

    public void PostWebsocketMessage(WebsocketMessage websocketMessage)
    {
      websocketHandler.SendQuery(websocketMessage);
    }

    public void PostApplicationExit(object sender = null)
    {
      ApplicationExit?.Invoke(sender ?? this, EventArgs.Empty);
    }

    public void PostSettingsChanged(object sender = null)
    {
      SettingsChanged?.Invoke(sender ?? this, EventArgs.Empty);
    }

    private void WebsocketHandlerOnMessageReceived(object sender, string message)
    {
      try
      {
        dynamic data = JsonConvert.DeserializeObject(message);
        if (data.status >= 400)
        {
          Invoke(ApiError, data);
          return;
        }

        if (data.meta is JObject meta && meta.TryGetValue("event", out var metaEvent))
        {
          var eventName = metaEvent.Value<string>();
          log.Debug($"Received ws message with event '{eventName}', posting to eventbus");
          switch (eventName)
          {
            case ApiEventNames.Connection:
              if (data.result?.attributes is JObject welcome && welcome.TryGetValue("version", out var versionString))
              {
                Version.TryParse(versionString.Value<string>(), out var version);
                Invoke(ConnectionEstablished, version);
              }
              else
              {
                // TODO remove this else branch as soon as API is updated with correct WS welcome
                var version = Version.Parse("2.1");
                Invoke(ConnectionEstablished, version);
              }

              break;
            case ApiEventNames.StreamSubscribe:
              break;
            case ApiEventNames.UserProfile:
              var user = JsonApi.Deserialize<User>(message);
              ProfileLoaded?.Invoke(this, user);
              break;
            case ApiEventNames.RescueCreated:
              var rescues = JsonApi.Deserialize<Rescue[]>(message);
              foreach (var rescue in rescues)
              {
                Invoke(RescueCreated, rescue);
              }

              break;
            case ApiEventNames.RescueUpdated:
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
            case ApiEventNames.RescueRead:
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