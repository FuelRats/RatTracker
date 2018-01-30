using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Caliburn.Micro;
using Newtonsoft.Json.Linq;
using Ninject;
using RatTracker.Api;
using RatTracker.Api.Fuelrats;
using RatTracker.Infrastructure;
using RatTracker.Infrastructure.Events;
using RatTracker.Infrastructure.Resources.Styles;
using RatTracker.Journal;
using RatTracker.Properties;
using RatTracker.ViewModels;
using ILog = log4net.ILog;

namespace RatTracker.Bootstrapping
{
  public class Bootstrapper : BootstrapperBase
  {
    private StandardKernel kernel;
    private EventBus eventBus;
    private ILog logger;

    public Bootstrapper()
    {
      Initialize();
      kernel.Get<Windows7StyleHack>().Hack();
    }

    protected override void Configure()
    {
      kernel = new StandardKernel(new Module());
      logger = kernel.Get<ILog>();
    }

    protected override object GetInstance(Type service, string key)
    {
      return kernel.Get(service);
    }

    protected override IEnumerable<object> GetAllInstances(Type service)
    {
      return kernel.GetAll(service);
    }

    protected override void OnExit(object sender, EventArgs e)
    {
      kernel.Get<EventBus>().PostApplicationExit(sender);
      base.OnExit(sender, e);
    }

    protected override async void OnStartup(object sender, StartupEventArgs e)
    {
      kernel.Get<ExceptionHandler>();
      var commandLineArgs = Environment.GetCommandLineArgs();
      var oauthArg = commandLineArgs.FirstOrDefault(x => x.StartsWith("rattracker"));
      if (oauthArg != null)
      {
        logger.Debug("Starting RT with OAuth header");
        var oAuthHandler = kernel.Get<OAuthHandler>();
        await oAuthHandler.ExchangeToken(oauthArg);
      }

      if (string.IsNullOrWhiteSpace(Settings.Default.OAuthToken))
      {
        logger.Debug("Starting RT without OAuth token");
        DisplayRootViewFor<OAuthStartupDialogViewModel>();
      }
      else
      {
        logger.Debug("Starting RT with OAuth token (normal startup)");
        DisplayRootViewFor<RatTrackerViewModel>();
        eventBus = kernel.Get<EventBus>();
        eventBus.ApiError += EventBusOnApiError;
        kernel.Get<Cache>();
        var journalReader = kernel.Get<JournalReader>();
        var websocketHandler = kernel.Get<WebsocketHandler>();
        var websocketTask = Task.Run(() => websocketHandler.Initialize(true));
        var journalTask = Task.Run(() => journalReader.Initialize());

        await Task.WhenAll(websocketTask, journalTask);
      }
    }

    private void EventBusOnApiError(object sender, dynamic data)
    {
      logger.Fatal($"Error on websocket: {data.code} - {data.title} - {data.status} - {data.detail}");

      if (data.code == 401)
      {
        DialogHelper.ShowWarning("Invalid login token. Resetting token and restarting RatTracker.");
        Settings.Default.OAuthToken = null;
        Settings.Default.Save();
        var oAuthHandler = kernel.Get<OAuthHandler>();
        oAuthHandler.RestartRatTracker();
      }

      if (data.meta is JObject meta && meta.TryGetValue("event", out var metaEvent))
      {
        var eventName = metaEvent.Value<string>();
        switch (eventName)
        {
          case ApiEventNames.Connection:
            DialogHelper.ShowWarning("Error connecting to API: connection event missing version info. RatTracker cannot run and will be closed.");
            eventBus.PostApplicationExit(this);
            break;
          default:
#if DEBUG
            DialogHelper.ShowWarning($"Yo, you forgot to add error handling for this '{eventName}'!");
#endif
            break;
        }
      }
    }
  }
}