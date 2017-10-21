using Caliburn.Micro;

namespace RatTracker_WPF.ViewModels
{
  public class RatTrackerViewModel : Screen
  {
    private AssignedRescueViewModel assignedRescue;
    private RescuesViewModel rescues;

    public RatTrackerViewModel()
    {
      AssignedRescue = new AssignedRescueViewModel();
      Rescues = new RescuesViewModel();
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