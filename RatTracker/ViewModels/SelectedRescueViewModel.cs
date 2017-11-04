using System;
using System.ComponentModel;
using System.Windows;
using Caliburn.Micro;
using RatTracker.Infrastructure;
using RatTracker.Infrastructure.Events;
using RatTracker.Models.App.Rescues;
using RatTracker.Models.Journal;
using RatTracker.Properties;

namespace RatTracker.ViewModels
{
  public class SelectedRescueViewModel : Screen
  {
    private SystemInfo currentLocation;
    private RescueModel rescueModel;
    private string distance;
    private int jumps;

    public SelectedRescueViewModel(EventBus eventBus)
    {
      eventBus.Rescues.SelectedRescueChanged += RescuesOnSelectedRescueChanged;
      eventBus.Journal.Location += JournalOnLocation;
    }

    public RescueModel RescueModel
    {
      get => rescueModel;
      set
      {
        if (rescueModel != null) { rescueModel.PropertyChanged -= RescueModelOnPropertyChanged; }
        rescueModel = value;
        if (rescueModel != null) { rescueModel.PropertyChanged += RescueModelOnPropertyChanged; }
        NotifyOfPropertyChange();
        RecalculateJumps();
      }
    }

    public string Distance
    {
      get => distance;
      set
      {
        distance = value;
        NotifyOfPropertyChange();
      }
    }

    public int Jumps
    {
      get => jumps;
      set
      {
        jumps = value;
        NotifyOfPropertyChange();
      }
    }

    public void CallJumps()
    {
      DialogHelper.ShowWarning("This function is not implemented yet.");
    }

    public void CopyClientName()
    {
      if (RescueModel == null) { return; }
      Clipboard.SetText(RescueModel.Rescue.Client);
    }

    public void CopySystemName()
    {
      if (RescueModel == null) { return; }
      Clipboard.SetText(RescueModel.Rescue.System);
    }

    private void RescueModelOnPropertyChanged(object sender, PropertyChangedEventArgs args)
    {
      if (args.PropertyName == nameof(RescueModel.System))
      {
        RecalculateJumps();
      }
    }

    private void JournalOnLocation(object sender, Location location)
    {
      currentLocation = new SystemInfo
      {
        Name = location.SystemName,
        X = location.Coordinates[0],
        Y = location.Coordinates[1],
        Z = location.Coordinates[2]
      };
      RecalculateJumps();
    }

    private void RecalculateJumps()
    {
      if (currentLocation == null || RescueModel?.System == null) { return; }
      var system = RescueModel.System;

      var deltaX = currentLocation.X - system.X;
      var deltaY = currentLocation.Y - system.Y;
      var deltaZ = currentLocation.Z - system.Z;
      var distanceToClient = Math.Sqrt(deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ);
      Distance = $"{distanceToClient:N} ly";
      Jumps = (int) Math.Ceiling(distanceToClient / Settings.Default.JumpRange);
    }

    private void RescuesOnSelectedRescueChanged(object sender, RescueModel rescue)
    {
      RescueModel = rescue;
    }
  }
}