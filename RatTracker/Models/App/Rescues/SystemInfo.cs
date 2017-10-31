namespace RatTracker.Models.App.Rescues
{
  public class SystemInfo : PropertyChangedBase
  {
    private string name;
    private double x;
    private double y;
    private double z;

    public string Name
    {
      get => name;
      set
      {
        name = value;
        NotifyPropertyChanged();
      }
    }

    public double X
    {
      get => x;
      set
      {
        x = value;
        NotifyPropertyChanged();
      }
    }

    public double Y
    {
      get => y;
      set
      {
        y = value;
        NotifyPropertyChanged();
      }
    }

    public double Z
    {
      get => z;
      set
      {
        z = value;
        NotifyPropertyChanged();
      }
    }
  }
}