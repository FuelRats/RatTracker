using System.Collections.Generic;
using System.Collections.ObjectModel;
using Caliburn.Micro;
using RatTracker.Models.Api;

namespace RatTracker.ViewModels
{
  public class RescuesViewModel : Screen
  {
    private Rescue selectedRescue;
    private double distance;
    private int jumps;

    public RescuesViewModel()
    {
      Rescues.Add(new Rescue {System = "Sol", Client = "TestClient", Rats = new List<Rat>{new Rat{Name = "Rat1"}}});
    }

    public ObservableCollection<Rescue> Rescues { get; } = new ObservableCollection<Rescue>();

    public Rescue SelectedRescue
    {
      get => selectedRescue;
      set
      {
        selectedRescue = value;
        NotifyOfPropertyChange();
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

    public void FindNearestStation()
    {
      
    }
  }
}