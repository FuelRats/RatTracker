using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using Caliburn.Micro;
using Ookii.Dialogs.Wpf;
using RatTracker.Api.Fuelrats;
using RatTracker.Infrastructure.Events;
using RatTracker.Infrastructure.Extensions;
using RatTracker.Infrastructure.NativeInterop;
using RatTracker.Properties;

namespace RatTracker.ViewModels
{
  public sealed class SettingsViewModel : Screen
  {
    private readonly EventBus eventBus;
    private readonly OAuthHandler oAuthHandler;

    public SettingsViewModel(EventBus eventBus, OAuthHandler oAuthHandler)
    {
      this.eventBus = eventBus;
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

    public void SelectJournalDirectory()
    {
      var vfbd = new VistaFolderBrowserDialog
      {
        Description = "Please select your Elite:Dangerous Journal folder (Containing Journal files)",
        UseDescriptionForTitle = true
      };

      if (vfbd.ShowDialog() == true)
      {
        Settings.Default.JournalDirectory = vfbd.SelectedPath;
      }
    }

    public void SaveAndClose()
    {
      Settings.Default.Save();
      eventBus.PostSettingsChanged(this);
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
      if (string.IsNullOrWhiteSpace(Settings.Default.JournalDirectory) &&
          SHGetKnownFolderPath(new Guid("4C5C32FF-BB9D-43B0-B5B4-2D72E54EAAA4"), 0, new IntPtr(0), out var path) >= 0)
      {
        var journalParentDirectory = Marshal.PtrToStringUni(path);
        if (journalParentDirectory != null)
        {
          Settings.Default.JournalDirectory = Path.Combine(journalParentDirectory, "Frontier Developments", "Elite Dangerous");
        }
      }
    }

    //turns out that this is how Elite gets the file path. Neat.
    [DllImport("Shell32.dll")]
    private static extern int SHGetKnownFolderPath([MarshalAs(UnmanagedType.LPStruct)] Guid rfid, uint dwFlags, IntPtr hToken, out IntPtr ppszPath);
  }
}