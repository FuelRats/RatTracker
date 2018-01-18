using RatTracker.Models.Apis.FuelRats.Rescues;
using RatTracker.Models.Apis.Systems;

namespace RatTracker.Models.App.Rescues
{
  public class RescueModel : PropertyChangedBase
  {
    private Rescue rescue;
    private StarSystem nearestSystem;
    private Station nearestStation;
    private StarSystem system;

    public RescueModel(Rescue rescue)
    {
      Rescue = rescue;
    }

    public Rescue Rescue
    {
      get => rescue;
      set
      {
        rescue = value;
        NotifyPropertyChanged();
        NotifyPropertyChanged(nameof(Rat1));
        NotifyPropertyChanged(nameof(Rat2));
        NotifyPropertyChanged(nameof(Rat3));
      }
    }

    public Rat Rat1 => Rescue?.Rats.Count > 0 ? Rescue?.Rats[0] : null;
    public Rat Rat2 => Rescue?.Rats.Count > 1 ? Rescue?.Rats[1] : null;
    public Rat Rat3 => Rescue?.Rats.Count > 2 ? Rescue?.Rats[2] : null;

    public StarSystem NearestSystem
    {
      get => nearestSystem;
      set
      {
        nearestSystem = value;
        NotifyPropertyChanged();
      }
    }

    public Station NearestStation
    {
      get => nearestStation;
      set
      {
        nearestStation = value;
        NotifyPropertyChanged();
      }
    }

    public StarSystem System
    {
      get => system;
      set
      {
        system = value;
        NotifyPropertyChanged();
      }
    }
  }
}