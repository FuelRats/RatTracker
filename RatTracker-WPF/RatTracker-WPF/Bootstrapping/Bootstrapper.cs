using System.Windows;
using Caliburn.Micro;
using RatTracker_WPF.ViewModels;

namespace RatTracker_WPF.Bootstrapping
{
  public class Bootstrapper : BootstrapperBase
  {
    public Bootstrapper()
    {
      Initialize();
    }

    protected override void OnStartup(object sender, StartupEventArgs e)
    {
      DisplayRootViewFor<RatTrackerViewModel>();
    }
  }
}