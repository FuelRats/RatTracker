using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using log4net;
using Microsoft.Win32;
using Newtonsoft.Json;
using RatTracker.Models.Apis.FuelRats.OAuth;
using RatTracker.Properties;

namespace RatTracker.Api.Fuelrats
{
  public class OAuthHandler
  {
    private const string OAuthScope = "user.read.me rescue.read";
    private readonly ILog logger;

    public OAuthHandler(ILog log)
    {
      logger = log;
    }

    public void RequestToken()
    {
      SetURIHandlerIfNecessary();
      var authcontent = new UriBuilder(
        $"{Settings.Default.WebsiteUrl}authorize?client_id={Settings.Default.OAuthClientId}&scope={OAuthScope}&redirect_uri=rattracker://auth&state=preinit&response_type=code")
      {
        Port = Settings.Default.WebSitePort
      };
      Process.Start(authcontent.ToString());
      CloseRatTracker();
    }

    public async Task ExchangeToken(string oauthArg)
    {
      var match = Regex.Match(oauthArg, ".*?code=(.*)?&state=preinit", RegexOptions.IgnoreCase);
      if (!match.Success)
      {
        MessageBox.Show(
          "RatTracker was started with an invalid argument for OAuth processing.");
        CloseRatTracker();
      }

      logger.Debug("Calling OAuth authentication...");
      var code = match.Groups[1].ToString();
      using (var hc = new HttpClient())
      {
        var content = new UriBuilder(Path.Combine($"{Settings.Default.ApiUrl}oauth2/token"))
        {
          Port = Settings.Default.ApiPort
        };
        var clientauthheader = Settings.Default.OAuthClientId + ":" + Settings.Default.OAuthAppSecret;
        hc.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
          Convert.ToBase64String(Encoding.ASCII.GetBytes(
            clientauthheader)));
        var urlEncodedContent = new FormUrlEncodedContent(new[]
        {
          new KeyValuePair<string, string>("code", code),
          new KeyValuePair<string, string>("grant_type", "authorization_code"),
          new KeyValuePair<string, string>("redirect_uri", "rattracker://auth")
        });
        var response = await hc.PostAsync(content.ToString(), urlEncodedContent).ConfigureAwait(false);
        var mycontent = response.Content;
        var data = await mycontent.ReadAsStringAsync();
        if (data.Contains("access_token"))
        {
          var token = JsonConvert.DeserializeObject<TokenResponse>(data);
          Settings.Default.OAuthToken = token.AccessToken;
          Settings.Default.Save();
        }
      }
    }

    public void RestartRatTracker(bool elevated = false)
    {
      var proc = new ProcessStartInfo
      {
        UseShellExecute = true,
        WorkingDirectory = Environment.CurrentDirectory,
        FileName = Process.GetCurrentProcess().MainModule.FileName
      };

      if (elevated)
      {
        proc.Verb = "runas";
      }

      Process.Start(proc);
      CloseRatTracker();
    }

    private void SetURIHandlerIfNecessary()
    {
      var programFileName = Process.GetCurrentProcess().MainModule.FileName;
      var registryKey = Registry.ClassesRoot.OpenSubKey(@"RatTracker\shell\open\command", false);
      var isHandlerSet = false;
      if (registryKey != null)
      {
        var mypath = (string) registryKey.GetValue("");
        logger.Debug("Have a URI handler registered: " + mypath + " and my procname is " + programFileName);
        if (mypath.Contains(programFileName))
        {
          isHandlerSet = true;
        }
      }

      if (isHandlerSet) { return; }

      ElevateApp();
      logger.Debug("Creating registry key");
      var key = Registry.ClassesRoot;
      key.CreateSubKey("RatTracker", true);
      key = key.OpenSubKey("RatTracker", true);
      if (key != null)
      {
        key.SetValue("URL Protocol", "");
        key.SetValue("DefaultIcon", "");
        key.CreateSubKey("shell", true);
        key = key.OpenSubKey("shell", true);
        if (key != null)
        {
          key.CreateSubKey("open", true);
          key = key.OpenSubKey("open", true);
          if (key != null)
          {
            key.CreateSubKey("command", true);
            key = key.OpenSubKey("command", true);
            key?.SetValue("", "\"" + Process.GetCurrentProcess().MainModule.FileName + "\" \"%1\"");
          }
        }
      }
    }

    private void ElevateApp()
    {
      var identity = WindowsIdentity.GetCurrent();
      var principal = new WindowsPrincipal(identity);
      if (principal.IsInRole(WindowsBuiltInRole.Administrator)) { return; }

      var result = MessageBox.Show(
        "RatTracker needs to set up a custom URL handler to handle OAuth authentication. This requires elevated privileges. Please click OK to restart RatTracker as Administrator. It will restart with normal privileges once the OAuth process is complete.",
        "RT Needs elevation", MessageBoxButton.OKCancel, MessageBoxImage.Exclamation);
      switch (result)
      {
        case MessageBoxResult.OK:
          RestartRatTracker(true);
          break;
        case MessageBoxResult.Cancel:
          MessageBox.Show(
            "RatTracker will not be able to connect to the API in this state! Continuing, but this will probably break RT.");
          CloseRatTracker();
          break;
        default:
          throw new ArgumentOutOfRangeException();
      }
    }

    private static void CloseRatTracker()
    {
      Environment.Exit(0);
    }
  }
}