using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Caliburn.Micro;
using Ninject;
using RatTracker.Api;
using RatTracker.Api.Fuelrats;
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
        kernel.Get<EventBus>();
        kernel.Get<Cache>();
        var journalReader = kernel.Get<JournalReader>();
        var websocketHandler = kernel.Get<WebsocketHandler>();
        var websocketTask = Task.Run(() => websocketHandler.Initialize(true));
        var journalTask = Task.Run(() => journalReader.Initialize());

        await Task.WhenAll(websocketTask, journalTask);
      }
    }
  }
}