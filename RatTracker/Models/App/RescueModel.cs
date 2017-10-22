using RatTracker.Models.Api.Rescues;

namespace RatTracker.Models.App
{
  public class RescueModel : PropertyChangedBase
  {
    private Rescue rescue;

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
  }
}