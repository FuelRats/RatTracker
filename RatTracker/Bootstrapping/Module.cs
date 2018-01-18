using System.Reflection;
using Caliburn.Micro;
using Ninject.Modules;
using RatTracker.Api;
using RatTracker.Api.Fuelrats;
using RatTracker.Infrastructure;
using RatTracker.Infrastructure.Events;
using ILog = log4net.ILog;
using LogManager = log4net.LogManager;

namespace RatTracker.Bootstrapping
{
  public class Module : NinjectModule
  {
    public override void Load()
    {
      // log4Net
      Bind<ILog>().ToConstant(LogManager.GetLogger(Assembly.GetEntryAssembly().GetName().Name));

      // Caliburn.Micro
      Bind<IWindowManager>().To<WindowManager>().InSingletonScope();
      Bind<IEventAggregator>().To<EventAggregator>().InSingletonScope();

      // Error handling
      Bind<ExceptionHandler>().ToSelf().InSingletonScope();

      // RatTracker
      Bind<WebsocketHandler>().ToSelf().InSingletonScope();
      Bind<Cache>().ToSelf().InSingletonScope();
      Bind<EventBus>().ToSelf().InSingletonScope();
    }
  }
}