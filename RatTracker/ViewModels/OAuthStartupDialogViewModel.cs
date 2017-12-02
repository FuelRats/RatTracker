using System.Windows;
using Caliburn.Micro;
using RatTracker.Api.Fuelrats;

namespace RatTracker.ViewModels
{
  public sealed class OAuthStartupDialogViewModel : Screen
  {
    private readonly OAuthHandler oAuthHandler;

    public OAuthStartupDialogViewModel(OAuthHandler oAuthHandler)
    {
      DisplayName = "RatTracker";
      this.oAuthHandler = oAuthHandler;
    }

    public void Authorize()
    {
      oAuthHandler.RequestToken();
    }

    public void DiscardAndClose()
    {
      Application.Current.Shutdown();
    }
  }
}