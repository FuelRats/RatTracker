namespace RatTracker_WPF.Models.App
{
  public class RatState : PropertyChangedBase
  {
    private bool beacon;
    private RequestState friendRequest;
    private bool fueled;
    private bool inInstance;
    private bool inSystem;
    private RequestState wingRequest;
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

    public RequestState FriendRequest
    {
      get => friendRequest;
      set
      {
        friendRequest = value;
        NotifyPropertyChanged();
      }
    }

    public RequestState WingRequest
    {
      get => wingRequest;
      set
      {
        wingRequest = value;
        NotifyPropertyChanged();
      }
    }

    public bool Beacon
    {
      get => beacon;
      set
      {
        beacon = value;
        NotifyPropertyChanged();
      }
    }

    public bool InSystem
    {
      get => inSystem;
      set
      {
        inSystem = value;
        NotifyPropertyChanged();
      }
    }

    public bool InInstance
    {
      get => inInstance;
      set
      {
        inInstance = value;
        NotifyPropertyChanged();
      }
    }

    public bool Fueled
    {
      get => fueled;
      set
      {
        fueled = value;
        NotifyPropertyChanged();
      }
    }
  }
}