using System;
using System.Linq;
using RatTracker.Models.Api;
using RatTracker.Models.Api.Rescues;
using RatTracker_WPF.Models.App;

namespace RatTracker.Models.App
{
  public class PlayerInfo : PropertyChangedBase
  {
    private string currentSystem;
    private float jumpRange;
    private bool onDuty;
    private bool superCruise;
    private string ratname;
    private User user;

    public string RatName
    {
      get => ratname;
      set
      {
        ratname = value;
        NotifyPropertyChanged();
      }
    }

    public User User
    {
      get => user;
      set
      {
        user = value;
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

    public Rat GetDisplayRat()
    {
      if (User == null) { throw new Exception("Rats not usable until profile information received"); }
      return User.DisplayRat?.Platform == Platform.Pc ? User.DisplayRat : User.Rats.FirstOrDefault(x => x.Platform == Platform.Pc);
    }
  }
}