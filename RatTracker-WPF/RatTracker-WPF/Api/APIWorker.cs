using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using log4net;
using Microsoft.Win32;
using Newtonsoft.Json;
using RatTracker_WPF.Api.Queries;
using RatTracker_WPF.Infrastructure;
using RatTracker_WPF.Models.Api.V2;
using RatTracker_WPF.Models.Api.V2.TPA;
using RatTracker_WPF.Properties;
using WebSocket4Net;
using ErrorEventArgs = SuperSocket.ClientEngine.ErrorEventArgs;

namespace RatTracker_WPF.Api
{
  public class ApiWorker
  {
    private static readonly ILog logger = LogManager.GetLogger(Assembly.GetCallingAssembly().GetName().Name);
    public WebSocket Ws;
    public WebsocketResponseHandler ResponseHandler = new WebsocketResponseHandler();
    private bool stopping;
    private bool failing;

    public void InitWs(bool includeToken)
    {
      if (Thread.CurrentThread.Name == null)
      {
        Thread.CurrentThread.Name = "APIWorker";
      }

      if (Ws != null)
      {
        Ws.Error -= WebsocketClientError;
        Ws.Opened -= WebsocketClientOpened;
        Ws.MessageReceived -= WebsocketClientMessageReceieved;
        Ws.Closed -= WebsocketClientClosed;
        Ws = null;
      }

      try
      {
        var wsurl = GetApiWssUrl(includeToken);
        logger.Info("Connecting to WS at " + wsurl);
        Ws = new WebSocket(wsurl, "", WebSocketVersion.Rfc6455) {AllowUnstrustedCertificate = true};
        Ws.Error += WebsocketClientError;
        Ws.Opened += WebsocketClientOpened;
        Ws.MessageReceived += WebsocketClientMessageReceieved;
        Ws.Closed += WebsocketClientClosed;
        ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
      }
      catch (Exception ex)
      {
        logger.Fatal("Well, that went tits up real fast: " + ex.Message);
      }
    }

    public void OpenWs()
    {
      if (Ws == null)
      {
        logger.Debug("Attempted to open uninitialized WS connection.");
        return;
      }
      Ws.Open();
      logger.Debug("WS client is " + Ws.State);
    }

    public void DisconnectWs()
    {
      stopping = true;
      Ws?.Close();
    }

    public void SendQuery(Query query)
    {
      var json = JsonConvert.SerializeObject(query.GetData());
      logger.Debug("Sent an APIQuery serialized as: " + json);
      Ws.Send(json);
    }

    public void SendTpaMessage(TpaMessage message)
    {
      if (Ws == null)
      {
        logger.Debug("Attempt to send TPA message over uninitialized WS connection!");
        return;
      }

      message.Data.Add("platform", "PC");
      message.Id = Settings.Default.AppID;
      var json = JsonConvert.SerializeObject(message);
      logger.Debug("Serialized TPA data: " + json);
      Ws.Send(json);
    }

    public void QueryRescues()
    {
      var query = Query.Request("rescues", "read");
      query.AddData(nameof(Rescue.Status).ToApiName(), Query.Data("$not", RescueState.Closed.ToApiName()));
      SendQuery(query);
      logger.Info("Sent RescueGrid Update request.");
    }

    public void SubscribeStream(string applicationId)
    {
      var query = Query.Request("stream", "subscribe");
      query.AddData("id", applicationId);
      logger.Debug("Subscribing to stream: " + applicationId);
      SendQuery(query);
    }

    // TODO MA See if this can be refactored into a separate class. Handles Elevation for OAuth stuff.
    public static string ConnectApi()
    {
      logger.Info("Oauth token is " + Settings.Default.OAuthToken);
      if (Settings.Default.OAuthToken != "")
      {
        logger.Info("Authenticating with OAuth token...");
        return "";
      }
      var rtPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
      if (File.Exists(rtPath + @"\RatTracker\OAuthToken.tmp"))
      {
        logger.Debug("Cheaty McCheatyFile detected.");
        var token = File.ReadAllText(rtPath + @"\RatTracker\OauthToken.tmp");
        Settings.Default.OAuthToken = token;
        Settings.Default.Save();
        return "";
      }
      /* Connect to the API here. */
      logger.Debug("No OAuth token stored, attempting to authorize app.");
      var proc = new ProcessStartInfo
      {
        UseShellExecute = true,
        WorkingDirectory = Environment.CurrentDirectory,
        FileName = Process.GetCurrentProcess().MainModule.FileName,
        Verb = "runas"
      };
      logger.Debug("Requesting identity.");
      var identity = WindowsIdentity.GetCurrent();
      var principal = new WindowsPrincipal(identity);
      logger.Debug("Checking registry key...");
      if (Registry.ClassesRoot.OpenSubKey(@"RatTracker\shell\open\command", false) == null)
      {
        logger.Debug("No custom URI configured for RatTracker.");
      }
      else
      {
        logger.Debug("An URI handler is set, checking...");
        var key = Registry.ClassesRoot.OpenSubKey(@"RatTracker\shell\open\command", false);
        if (key != null)
        {
          var mypath = (string) key.GetValue("");
          logger.Debug("Have a URI handler registered: " + mypath + " and my procname is " + proc.FileName);
          if (!mypath.Contains(proc.FileName))
          {
            logger.Debug("URI handler is not set to current version of RatTracker, resetting...");
          }
          else
          {
            logger.Debug("RatTracker URI is properly configured, launching OAuth authentication process.");
            var authcontent = new UriBuilder(Path.Combine(Settings.Default.APIURL + "oauth2/authorize?client_id=" +
                                                          Settings.Default.ClientID +
                                                          "&scope=*&redirect_uri=rattracker://auth&state=preinit&response_type=code"))
            {
              Port = Settings.Default.APIPort
            };
            Process.Start(authcontent.ToString());
            Process.GetCurrentProcess().Kill();
            return "";
          }
        }
      }

      logger.Debug("Passing elevation check.");
      if (!principal.IsInRole(WindowsBuiltInRole.Administrator))
      {
        logger.Debug("Not running as administrator, requesting elevation.");
        try
        {
          logger.Debug("Not admin and key does not exist, requesting elevation.");
          var result = MessageBox.Show(
            "RatTracker needs to set up a custom URL handler to handle OAuth authentication. This requires elevated privileges. Please click OK to restart RatTracker as Administrator. It will restart with normal privileges once the OAuth process is complete.",
            "RT Needs elevation", MessageBoxButton.OKCancel, MessageBoxImage.Exclamation);
          switch (result)
          {
            case MessageBoxResult.Yes:
              Thread.Sleep(10000); // Wait for the initializers to finish their business before killing the task.
              Process.Start(proc);
              Process.GetCurrentProcess().Kill(); // Die! DIEEE!!!
              break;
            case MessageBoxResult.No:
              MessageBox.Show(
                "RatTracker will not be able to connect to the API in this state! Continuing, but this will probably break RT.");
              break;
          }
        }
        catch
        {
          // Do nothing and return ...
          return "";
        }
      }

      try
      {
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
        logger.Debug("Launching authorization process.");
        var authcontent =
          new UriBuilder(Path.Combine(Settings.Default.APIURL + "oauth2/authorize?client_id=" +
                                      Settings.Default.ClientID +
                                      "&scope=*&redirect_uri=rattracker://auth&state=preinit&response_type=code"))
          {
            Port = Settings.Default.APIPort
          };
        Thread.Sleep(5000);
        Process.Start(authcontent.ToString());
        Process.GetCurrentProcess().Kill();
        return "";
      }
      catch (Exception ex)
      {
        logger.Fatal("Exception in ConnectAPI: ", ex);
        return "";
      }
    }

    private void WebsocketClientError(object sender, ErrorEventArgs e)
    {
      logger.Fatal("Websocket: Exception thrown: ", e.Exception);
    }

    private void WebsocketClientOpened(object sender, EventArgs e)
    {
      failing = false;
      logger.Info("Websocket: Connection to API established.");
      if (Settings.Default.OAuthToken == null)
      {
        logger.Error("Oops! I am attempting to connect without an OAuth token. That's probably bad.");
      }
      else
      {
        SubscribeStream("0xDEADBEEF");
        var login = Query.Request("users", "profile");
        SendQuery(login);
        QueryRescues();
      }
      //TODO: Put stream subscription messages here when Mecha goes live. Do we want to listen to ourselves?
    }

    private static string GetApiWssUrl(bool includeToken)
    {
      var apiurl = Settings.Default.APIURL.Replace("https://", "wss://").Replace("http://", "ws://");
      if (apiurl.EndsWith("/"))
      {
        apiurl = apiurl.Substring(0, apiurl.Length - 1);
      }

      apiurl = $"{apiurl}:{Settings.Default.APIPort}";
      if (includeToken)
      {
        apiurl += $"?bearer={Settings.Default.OAuthToken}";
      }

      return apiurl;
    }

    private async void WebsocketClientClosed(object sender, EventArgs e)
    {
      if (stopping)
      {
        logger.Info("Disconnected from API WS server, stopping...");
      }
      else
      {
        if (Ws.State == WebSocketState.Connecting)
        {
          logger.Debug("Connection attempt already underway, ignoring reconnect attempt.");
          return;
        }
        if (failing)
        {
          logger.Info("API reconnect still failing. Waiting...");
          await Task.Delay(20000);
          OpenWs();
          return;
        }

        logger.Info("API WS Connection closed unexpectedly. Reconnecting in five seconds...");
        failing = true;
        await Task.Delay(5000);
        OpenWs();
      }
    }

    private void WebsocketClientMessageReceieved(object sender, MessageReceivedEventArgs e)
    {
      ResponseHandler.MessageReceived(e.Message);

      /* We can let everything below this go away fairly soon, since there's not much we can do in the API Worker anyways
             * at this point. Maybe attach a logger here?
             */
      dynamic data = JsonConvert.DeserializeObject(e.Message);
      //TODO: Implement error handling.
      if (data.errors != null)
      {
        logger.Fatal("The API returned an error when deserializing in APIWorker! " + data);
        return;
      }

      //TODO: Implement actual pass to our 3PA logic.
      if (data.application != null)
      {
        logger.Debug("Got an application message, pass to own parser.");
        return;
      }

      switch ((string) data.action)
      {
        case "welcome":
          logger.Info("API MOTD: " + data.data);
          break;
      }

      //appendStatus("Direct parse. Type:" + data.type + " Data:" + data.data);
    }
  }
}