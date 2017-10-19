using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using RatTracker_WPF.Models.App;
using RatTracker_WPF.ViewModels;

namespace RatTracker_WPF.Infrastructure.Controls
{
  public class RatStateControl : Control
  {
    public static readonly DependencyProperty StateProperty = DependencyProperty.Register(nameof(State), typeof(RatState), typeof(RatStateControl));
    public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(nameof(ViewModel), typeof(AssignedRescueViewModel), typeof(RatStateControl));

    static RatStateControl()
    {
      DefaultStyleKeyProperty.OverrideMetadata(typeof(RatStateControl), new FrameworkPropertyMetadata(typeof(RatStateControl)));
    }

    public RatStateControl()
    {
      FriendRequest = new RelayCommand(o => true, o => ViewModel?.ToggleFriendRequest(State));
      WingRequest = new RelayCommand(o => true, o => ViewModel?.ToggleWingRequest(State));
      InSystem = new RelayCommand(o => true, o => ViewModel?.ToggleInSystem(State));
      BeaconVisible = new RelayCommand(o => true, o => ViewModel?.ToggleBeaconVisible(State));
      InInstance = new RelayCommand(o => true, o => ViewModel?.ToggleInInstance(State));
    }

    public RatState State
    {
      get => (RatState) GetValue(StateProperty);
      set => SetValue(StateProperty, value);
    }

    public AssignedRescueViewModel ViewModel
    {
      get => (AssignedRescueViewModel) GetValue(ViewModelProperty);
      set => SetValue(ViewModelProperty, value);
    }

    public ICommand FriendRequest { get; }
    public ICommand WingRequest { get; }
    public ICommand InSystem { get; }
    public ICommand BeaconVisible { get; }
    public ICommand InInstance { get; }
  }
}