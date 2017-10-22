using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using Caliburn.Micro;
using Ninject;
using RatTracker.Api;
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

    protected override void OnStartup(object sender, StartupEventArgs e)
    {
      DisplayRootViewFor<RatTrackerViewModel>();
      kernel.Get<EventBus>();
      kernel.Get<Cache>();
      var websocketHandler = kernel.Get<WebsocketHandler>();
      Task.Run(() => { websocketHandler.Initialize(true); });
    }
  }
}