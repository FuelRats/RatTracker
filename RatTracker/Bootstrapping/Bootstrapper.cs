using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Caliburn.Micro;
using Ninject;
using RatTracker.Api;
using RatTracker.Firebird;
using RatTracker.Properties;
using RatTracker.ViewModels;

namespace RatTracker.Bootstrapping
{
  public class Bootstrapper : BootstrapperBase
  {
    private StandardKernel kernel;

    public Bootstrapper()
    {
      Initialize();
    }

    protected override void Configure()
    {
      kernel = new StandardKernel(new Module());
      kernel.Bind<IWindowManager>().To<WindowManager>().InSingletonScope();
      kernel.Bind<IEventAggregator>().To<EventAggregator>().InSingletonScope();
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
      var starSystemDatabase = kernel.Get<StarSystemDatabase>();
      starSystemDatabase.CloseConnection();
      base.OnExit(sender, e);
    }

    protected override async void OnStartup(object sender, StartupEventArgs e)
    {
      var commandLineArgs = Environment.GetCommandLineArgs();
      var oauthArg = commandLineArgs.FirstOrDefault(x => x.StartsWith("rattracker"));
      if (oauthArg != null)
      {
        var oAuthHandler = kernel.Get<OAuthHandler>();
        await oAuthHandler.ExchangeToken(oauthArg);
      }

      if (string.IsNullOrWhiteSpace(Settings.Default.OAuthToken))
      {
        DisplayRootViewFor<OAuthStartupDialogViewModel>();
      }
      else
      {
        DisplayRootViewFor<RatTrackerViewModel>();
        kernel.Get<EventBus>();
        kernel.Get<Cache>();
        var websocketHandler = kernel.Get<WebsocketHandler>();
        var starSystemDatabase = kernel.Get<StarSystemDatabase>();
        var websocketTask = Task.Run(() => { websocketHandler.Initialize(true); });
        var systemsDataBaseTask = Task.Run(() => { starSystemDatabase.Initialize(); });

        //var updater = kernel.Get<Updater>();

        //systemsDataBaseTask = systemsDataBaseTask.ContinueWith(async task =>
        //{
        //  var downloaderTask = Task.Run(async () => await updater.DownloadSystems(starSystemDatabase));
        //  await Task.WhenAll(downloaderTask);

        //  var stations = new BlockingCollection<EddbStation>(new ConcurrentQueue<EddbStation>());
        //  var downloaderTask2 = Task.Run(() => updater.DownloadStations(stations));
        //  var inserterTask2 = Task.Run(() => starSystemDatabase.Insert(stations));
        //  await Task.WhenAll(downloaderTask2, inserterTask2);
        //});

        await Task.WhenAll(websocketTask, systemsDataBaseTask);
      }
    }
  }
}