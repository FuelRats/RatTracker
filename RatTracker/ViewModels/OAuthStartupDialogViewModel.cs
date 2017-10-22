using System.Windows;
using Caliburn.Micro;
using RatTracker.Api;
using RatTracker.Properties;

namespace RatTracker.ViewModels
{
  public sealed class OAuthStartupDialogViewModel : Screen
  {
    private readonly OAuthHandler oAuthHandler;
    private string email;

    public OAuthStartupDialogViewModel(OAuthHandler oAuthHandler)
    {
      DisplayName = "RatTracker";
      this.oAuthHandler = oAuthHandler;
      Email = Settings.Default.ApiUserEmail;
    }

    public string Email
    {
      get => email;
      set
      {
        email = value;
        NotifyOfPropertyChange();
      }
    }

    public void SaveAndAuthorize()
    {
      if (Settings.Default.ApiUserEmail != Email)
      {
        Settings.Default.ApiUserEmail = Email;
        Settings.Default.Save();
      }

      oAuthHandler.RequestToken(Email);
    }

    public void DiscardAndClose()
    {
      Application.Current.Shutdown();
    }
  }
}