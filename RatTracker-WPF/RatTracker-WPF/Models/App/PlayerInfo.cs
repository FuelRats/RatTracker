using System.Collections.Generic;

namespace RatTracker_WPF.Models.App
{
  public class PlayerInfo : PropertyChangedBase
  {
    private string currentSystem;
    private float jumpRange;
    private bool onDuty;
    private bool superCruise;
    private List<string> ratId;
    private string ratname;

    public string RatName
    {
      get => ratname;
      set
      {
        ratname = value;
        NotifyPropertyChanged();
      }
    }

    public List<string> RatId
    {
      get => ratId;
      set
      {
        ratId = value;
        NotifyPropertyChanged();
      }
    }

    public string CurrentSystem
    {
      get => currentSystem;
      set
      {
        currentSystem = value;
        NotifyPropertyChanged();
      }
    }

    public bool OnDuty
    {
      get => onDuty;
      set
      {
        onDuty = value;
        NotifyPropertyChanged();
      }
    }

    public float JumpRange
    {
      get => jumpRange;
      set
      {
        jumpRange = value;
        NotifyPropertyChanged();
      }
    }

    public bool SuperCruise
    {
      get => superCruise;
      set
      {
        superCruise = value;
        NotifyPropertyChanged();
      }
    }
  }
}