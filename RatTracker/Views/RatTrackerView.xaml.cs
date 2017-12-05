using System;
using System.ComponentModel;
using RatTracker.Infrastructure.NativeInterop;
using RatTracker.Properties;

namespace RatTracker.Views
{
  /// <summary>
  ///   Interaktionslogik für RatTrackerView.xaml
  /// </summary>
  public partial class RatTrackerView
  {
    public RatTrackerView()
    {
      InitializeComponent();
      Closing += OnClosing;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
      base.OnSourceInitialized(e);
      this.SetPlacement(Settings.Default.WindowPlacement);
    }

    private void OnClosing(object sender, CancelEventArgs e)
    {
      Settings.Default.WindowPlacement = this.GetPlacement();
      Settings.Default.Save();
    }
  }
}