using Caliburn.Micro;

namespace RatTracker.ViewModels
{
  public sealed class RatTrackerViewModel : Screen
  {
    private readonly IWindowManager windowManager;
    private readonly SettingsViewModel settingsViewModel;
    private AssignedRescueViewModel assignedRescue;
    private RescuesViewModel rescues;
    private PlayerInformationViewModel playerInformation;
    private SelectedRescueViewModel selectedRescue;
    private FilterInfoViewModel filterInfoViewModel;

    public RatTrackerViewModel(
      IWindowManager windowManager,
      AssignedRescueViewModel assignedRescueViewModel,
      SelectedRescueViewModel selectedRescueViewModel,
      RescuesViewModel rescuesViewModel,
      PlayerInformationViewModel playerInformation,
      FilterInfoViewModel filterInfoViewModel,
      SettingsViewModel settingsViewModel)
    {
      this.windowManager = windowManager;
      this.settingsViewModel = settingsViewModel;
      DisplayName = "RatTracker";
      AssignedRescue = assignedRescueViewModel;
      SelectedRescue = selectedRescueViewModel;
      Rescues = rescuesViewModel;
      PlayerInformation = playerInformation;
      FilterInfoViewModel = filterInfoViewModel;
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

    public SelectedRescueViewModel SelectedRescue
    {
      get => selectedRescue;
      set
      {
        selectedRescue = value;
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

    public PlayerInformationViewModel PlayerInformation
    {
      get => playerInformation;
      set
      {
        playerInformation = value;
        NotifyOfPropertyChange();
      }
    }

    public FilterInfoViewModel FilterInfoViewModel
    {
      get => filterInfoViewModel;
      set
      {
        filterInfoViewModel = value;
        NotifyOfPropertyChange();
      }
    }

    public void OpenSettings()
    {
      windowManager.ShowDialog(settingsViewModel);
    }
  }
}