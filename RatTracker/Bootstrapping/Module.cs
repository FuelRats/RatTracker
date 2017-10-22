using System.Reflection;
using log4net;
using Ninject.Modules;
using RatTracker.Api;

namespace RatTracker.Bootstrapping
{
  public class Module : NinjectModule
  {
    public override void Load()
    {
      Bind<ILog>().ToConstant(LogManager.GetLogger(Assembly.GetCallingAssembly().GetName().Name));

      Bind<WebsocketHandler>().ToSelf().InSingletonScope();
      Bind<Cache>().ToSelf().InSingletonScope();
      Bind<EventBus>().ToSelf().InSingletonScope();
    }
  }
}