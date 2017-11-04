using System;
using System.Threading.Tasks;
using System.Windows;
using Caliburn.Micro;
using RatTracker.Infrastructure;
using RatTracker.Infrastructure.Events;
using RatTracker.Models.App.Rescues;

namespace RatTracker.ViewModels
{
  public class SelectedRescueViewModel : Screen
  {
    private RescueModel rescueModel;
    private double distance;
    private int jumps;

    public SelectedRescueViewModel(EventBus eventBus)
    {
      eventBus.Rescues.SelectedRescueChanged += RescuesOnSelectedRescueChanged;
    }

    public RescueModel RescueModel
    {
      get => rescueModel;
      set
      {
        rescueModel = value;
        NotifyOfPropertyChange();
        RecalculateJumps(value.Rescue.System);
      }
    }

    public double Distance
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

    private async void RecalculateJumps(string rescueSystem)
    {
      // TODO implement jump calculation
      var jumpCount = await Task.Run(() => !string.IsNullOrWhiteSpace(rescueSystem) ? new Random().Next(10) : 0);

      if (RescueModel?.Rescue.System == rescueSystem)
      {
        Jumps = jumpCount;
      }
    }

    private void RescuesOnSelectedRescueChanged(object sender, RescueModel rescue)
    {
      RescueModel = rescue;
    }
  }
}