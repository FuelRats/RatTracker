using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using Caliburn.Micro;
using Ookii.Dialogs.Wpf;
using RatTracker.Api;
using RatTracker.Infrastructure;
using RatTracker.Infrastructure.Extensions;
using RatTracker.Properties;

namespace RatTracker.ViewModels
{
  public sealed class SettingsViewModel : Screen
  {
    private readonly OAuthHandler oAuthHandler;

    public SettingsViewModel(OAuthHandler oAuthHandler)
    {
      this.oAuthHandler = oAuthHandler;
      DisplayName = "Settings";
      AllMonitors = new ObservableCollection<string>();
    }

    public ObservableCollection<string> AllMonitors { get; }

    public void SelectLauncherDirectory()
    {
      var vfbd = new VistaFolderBrowserDialog
      {
        Description = "Please select your Elite:Dangerous folder (Containing the launcher executable)",
        UseDescriptionForTitle = true
      };

      if (vfbd.ShowDialog() == true)
      {
        Settings.Default.LauncherDirectory = vfbd.SelectedPath;
        Settings.Default.LogDirectory = vfbd.SelectedPath + @"\Products\elite-dangerous-64\Logs";
      }
    }

    public void SelectLogDirectory()
    {
      var vfbd = new VistaFolderBrowserDialog
      {
        Description = "Please select your Elite:Dangerous NetLog folder (Containing NetLog files)",
        UseDescriptionForTitle = true
      };

      if (vfbd.ShowDialog() == true)
      {
        Settings.Default.LogDirectory = vfbd.SelectedPath;
      }
    }

    public void SaveAndClose()
    {
      Settings.Default.Save();
      TryClose();
    }

    public void Logout()
    {
      var result = MessageBox.Show("Do you want to log out? This will restart RatTracker.", "Logout?", MessageBoxButton.YesNo, MessageBoxImage.Question);
      if (result == MessageBoxResult.Yes)
      {
        Settings.Default.OAuthToken = null;
        Settings.Default.Save();
        oAuthHandler.RestartRatTracker();
      }
    }

    public void Cancel()
    {
      Settings.Default.Reload();
      TryClose();
    }

    protected override void OnInitialize()
    {
      base.OnInitialize();
      AllMonitors.AddAll(Monitor.AllMonitors.Select(x => x.Name));
    }
  }
}