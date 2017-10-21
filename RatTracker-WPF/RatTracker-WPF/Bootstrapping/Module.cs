using Ninject.Modules;
using RatTracker_WPF.Api;

namespace RatTracker_WPF.Bootstrapping
{
  public class Module : NinjectModule
  {
    public override void Load()
    {
      Bind<ApiWorker>().ToSelf().InSingletonScope();
    }
  }
}