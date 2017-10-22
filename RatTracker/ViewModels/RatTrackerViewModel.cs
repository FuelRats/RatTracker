using Caliburn.Micro;

namespace RatTracker.ViewModels
{
  public class RatTrackerViewModel : Screen
  {
    private AssignedRescueViewModel assignedRescue;
    private RescuesViewModel rescues;

    public RatTrackerViewModel(AssignedRescueViewModel assignedRescueViewModel, RescuesViewModel rescuesViewModel)
    {
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
  }
}