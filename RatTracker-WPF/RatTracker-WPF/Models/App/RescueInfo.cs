using System.Collections.Generic;
using System.Collections.ObjectModel;
using RatTracker_WPF.Models.Api.V2;

namespace RatTracker_WPF.Models.App
{
  public class RescueInfo : PropertyChangedBase
  {
    private Rescue rescue;
    private string clientName;
    private string clientSystem;
    
    public string ClientName
    {
      get => clientName;
      set
      {
        clientName = value;
        NotifyPropertyChanged();
      }
    }

    public string ClientId { get; set; }
    public string ClientState { get; set; }
    public string ClientIp { get; set; }
    public string SessionId { get; set; }

    public string ClientSystem
    {
      get => clientSystem;
      set
      {
        clientSystem = value;
        NotifyPropertyChanged();
      }
    }

    public Rescue Rescue
    {
      get => rescue;
      set
      {
        rescue = value;
        Rats = rescue.Rats;
        NotifyPropertyChanged();
        NotifyPropertyChanged(nameof(Rats));
      }
    }

    public IEnumerable<Rat> Rats { get; private set; }

    public RatState Self { get; } = new RatState();

    public RatState Rat2 { get; } = new RatState();

    public RatState Rat3 { get; } = new RatState();
  }
}