using System.Windows;
using System.Windows.Controls;
using RatTracker.Models.App.Rescues;

namespace RatTracker.Infrastructure.Controls
{
  public class RatStateButton : Button
  {
    public static readonly DependencyProperty RequestStateProperty = DependencyProperty.Register(nameof(RequestState), typeof(RequestState), typeof(RatStateButton));
    public static readonly DependencyProperty StatusProperty = DependencyProperty.Register(nameof(Status), typeof(bool?), typeof(RatStateButton));

    static RatStateButton()
    {
      DefaultStyleKeyProperty.OverrideMetadata(typeof(RatStateButton), new FrameworkPropertyMetadata(typeof(Button)));
    }

    public RequestState RequestState
    {
      get => (RequestState) GetValue(RequestStateProperty);
      set => SetValue(RequestStateProperty, value);
    }

    public bool? Status
    {
      get => (bool?) GetValue(StatusProperty);
      set => SetValue(StatusProperty, value);
    }
  }
}