using Caliburn.Micro;

namespace RatTracker.ViewModels
{
  public sealed class RatTrackerViewModel : Screen
  {
    private readonly IWindowManager windowManager;
    private readonly SettingsViewModel settingsViewModel;
    private AssignedRescueViewModel assignedRescue;
    private RescuesViewModel rescues;

    public RatTrackerViewModel(IWindowManager windowManager, AssignedRescueViewModel assignedRescueViewModel, RescuesViewModel rescuesViewModel, SettingsViewModel settingsViewModel)
    {
      this.windowManager = windowManager;
      this.settingsViewModel = settingsViewModel;
      DisplayName = "RatTracker";
      AssignedRescue = assignedRescueViewModel;
      Rescues = rescuesViewModel;
    }

    public AssignedRescueViewModel AssignedRescue
    {
      get => assignedRescue;
      set
      {
        assignedRescue = value;
        NotifyOfPropertyChange();
      }
    }

    public RescuesViewModel Rescues
    {
      get => rescues;
      set
      {
        rescues = value;
        NotifyOfPropertyChange();
      }
    }

    public void OpenSettings()
    {
      windowManager.ShowDialog(settingsViewModel);
    }
  }
}