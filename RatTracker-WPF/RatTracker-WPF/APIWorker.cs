﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using log4net;
using Microsoft.Win32;
using Newtonsoft.Json;
using RatTracker_WPF.Infrastructure;
using RatTracker_WPF.Models.Api;
using RatTracker_WPF.Models.Api.V2;
using RatTracker_WPF.Properties;
using WebSocket4Net;
using ErrorEventArgs = SuperSocket.ClientEngine.ErrorEventArgs;

namespace RatTracker_WPF
{
  /*
     * APIWorker provides both HTTP and Websocket connection to the API. 
     */
  public class ApiWorker
  {
    private static readonly ILog Logger = LogManager.GetLogger(Assembly.GetCallingAssembly().GetName().Name);
    public WebSocket Ws;
    private bool _stopping;

    private bool _failing;

    /*
         * queryAPI sends a GET request to the API. Kindasorta deprecated behavior.
         */
    public void InitWs(bool includeToken)
    {
      if (Thread.CurrentThread.Name == null)
      {
        Thread.CurrentThread.Name = "APIWorker";
      }

      if (Ws != null)
      {
        Ws.Error -= websocketClient_Error;
        Ws.Opened -= websocketClient_Opened;
        Ws.MessageReceived -= websocketClient_MessageReceieved;
        Ws.Closed -= websocket_Client_Closed;
        Ws = null;
      }

      try
      {
        var wsurl = GetApiWssUrl(includeToken);
        Logger.Info("Connecting to WS at " + wsurl);
        Ws = new WebSocket(wsurl, "", WebSocketVersion.Rfc6455) {AllowUnstrustedCertificate = true};
        Ws.Error += websocketClient_Error;
        Ws.Opened += websocketClient_Opened;
        Ws.MessageReceived += websocketClient_MessageReceieved;
        Ws.Closed += websocket_Client_Closed;
        ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
      }
      catch (Exception ex)
      {
        Logger.Fatal("Well, that went tits up real fast: " + ex.Message);
      }
    }

    public void OpenWs()
    {
      if (Ws == null)
      {
        Logger.Debug("Attempted to open uninitialized WS connection.");
        return;
      }
      Ws.Open();
      Logger.Debug("WS client is " + Ws.State);
    }

    public void DisconnectWs()
    {
      _stopping = true;
      Ws?.Close();
    }

    public void SendWs(string action, IDictionary<string, string> data)
    {
      if (Ws == null)
      {
        Logger.Debug("Attempt to send data over uninitialized WS connection!");
        return;
      }

      switch (action)
      {
        case "rescues:read":
          Logger.Debug("SendWS is requesting a rescues update.");
          break;
      }

      var myquery = new APIQuery
      {
        action = action.Split(':'),
        data = data
      };
      var json = JsonConvert.SerializeObject(myquery);
      Logger.Debug("sendWS Serialized to: " + json);
      Ws.Send(json);
    }

    public void SendQuery(APIQuery myquery)
    {
      var json = JsonConvert.SerializeObject(myquery);
      Logger.Debug("Sent an APIQuery serialized as: " + json);
      Ws.Send(json);
    }

    public void SendQuery(IDictionary<string, object> myquery)
    {
      var json = JsonConvert.SerializeObject(myquery);
      Logger.Debug("Sent an APIQuery serialized as: " + json);
      Ws.Send(json);
    }
    
    public void SendTpaMessage(TPAMessage message)
    {
      if (Ws == null)
      {
        Logger.Debug("Attempt to send TPA message over uninitialized WS connection!");
        return;
      }

      message.data.Add("platform", "PC");
      message.applicationId = Settings.Default.AppID;
      var json = JsonConvert.SerializeObject(message);
      Logger.Debug("Serialized TPA data: " + json);
      Ws.Send(json);
    }

    public void websocketClient_Error(object sender, ErrorEventArgs e)
    {
      Logger.Fatal("Websocket: Exception thrown: ", e.Exception);
    }

    public void websocketClient_Opened(object sender, EventArgs e)
    {
      _failing = false;
      Logger.Info("Websocket: Connection to API established.");
      if (Settings.Default.OAuthToken == null)
      {
        Logger.Error("Oops! I am attempting to connect without an OAuth token. That's probably bad.");
      }
      else
      {
        SubscribeStream("0xDEADBEEF");
        QueryRescues();
      }
      //TODO: Put stream subscription messages here when Mecha goes live. Do we want to listen to ourselves?
    }

    public void QueryRescues()
    {
      IDictionary<string, object> login = new ExpandoObject();
      login.Add("action", new[] {"rescues", "read"});
      login.Add(nameof(Models.Api.V2.Rescue.Status).ToApiName(), RescueState.Open.ToApiName());
      SendQuery(login);
      Logger.Info("Sent RescueGrid Update request.");
    }

    public void SubscribeStream(string applicationId)
    {
      var data = new Dictionary<string, object>();
      data.Add("action", "stream:subscribe".Split(':'));
      data.Add("id", applicationId);
      Logger.Debug("Subscribing to stream: " + applicationId);
      var json = JsonConvert.SerializeObject(data);
      Logger.Debug("Sent a Subscribe serialized as: " + json);
      Ws.Send(json);
    }

    public async Task<string> QueryApi(string action, Dictionary<string, string> data)
    {
      try
      {
        using (var client = new HttpClient())
        {
          var content = new UriBuilder(Settings.Default.APIURL + action + "/")
          {
            Port = Settings.Default.APIPort
          };
          var query = HttpUtility.ParseQueryString(content.Query);
          foreach (var entry in data)
          {
            query[entry.Key] = entry.Value;
          }

          content.Query = query.ToString();
          Logger.Debug("Built query string:" + content);
          var response = await client.GetAsync(content.ToString());
          //appendStatus("AsyncPost sent.");
          if (response.IsSuccessStatusCode)
          {
            var responseString = await response.Content.ReadAsStringAsync();
            Logger.Debug("Starting response string parse task.");
            return ApiGetResponse(responseString);
          }

          Logger.Debug("HTTP request returned an error:" + response.StatusCode);
          return "";
        }
      }
      catch (Exception ex)
      {
        Logger.Fatal("Exception in QueryAPI: ", ex);
        return "";
      }
    }

    /* apiGetResponse is called by queryAPI asynchronously when the response arrives from the
         * server. This is then passed as a NVC to the main class.
         * TODO: Recode to use IDictionary.
         * REDONE: Now returns the JSON itself, do parsing in main.
         */
    public static string ApiGetResponse(string data)
    {
      //Console.WriteLine("apiGetResponse has string:" + data);
      return data;
    }

    public static string ConnectApi()
    {
      Logger.Info("Oauth token is " + Settings.Default.OAuthToken);
      if (Settings.Default.OAuthToken != "")
      {
        Logger.Info("Authenticating with OAuth token...");
        return "";
      }
      var _rtPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
      if (File.Exists(_rtPath + @"\RatTracker\OAuthToken.tmp"))
      {
        Logger.Debug("Cheaty McCheatyFile detected.");
        var token = File.ReadAllText(_rtPath + @"\RatTracker\OauthToken.tmp");
        Settings.Default.OAuthToken = token;
        Settings.Default.Save();
        return "";
      }
      /* Connect to the API here. */
      Logger.Debug("No OAuth token stored, attempting to authorize app.");
      var proc = new ProcessStartInfo
      {
        UseShellExecute = true,
        WorkingDirectory = Environment.CurrentDirectory,
        FileName = Process.GetCurrentProcess().MainModule.FileName,
        Verb = "runas"
      };
      Logger.Debug("Requesting identity.");
      var identity = WindowsIdentity.GetCurrent();
      var principal = new WindowsPrincipal(identity);
      Logger.Debug("Checking registry key...");
      if (Registry.ClassesRoot.OpenSubKey(@"RatTracker\shell\open\command", false) == null)
      {
        Logger.Debug("No custom URI configured for RatTracker.");
      }
      else
      {
        Logger.Debug("An URI handler is set, checking...");
        var key = Registry.ClassesRoot.OpenSubKey(@"RatTracker\shell\open\command", false);
        if (key != null)
        {
          var mypath = (string) key.GetValue("");
          Logger.Debug("Have a URI handler registered: " + mypath + " and my procname is " + proc.FileName);
          if (!mypath.Contains(proc.FileName))
          {
            Logger.Debug("URI handler is not set to current version of RatTracker, resetting...");
          }
          else
          {
            Logger.Debug("RatTracker URI is properly configured, launching OAuth authentication process.");
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

      Logger.Debug("Passing elevation check.");
      if (!principal.IsInRole(WindowsBuiltInRole.Administrator))
      {
        Logger.Debug("Not running as administrator, requesting elevation.");
        try
        {
          Logger.Debug("Not admin and key does not exist, requesting elevation.");
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
        Logger.Debug("Creating registry key");
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
              if (key != null)
              {
                key.SetValue("", "\"" + Process.GetCurrentProcess().MainModule.FileName + "\" \"%1\"");
              }
            }
          }
        }
        Logger.Debug("Launching authorization process.");
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
        Logger.Fatal("Exception in ConnectAPI: ", ex);
        return "";
      }
    }

    public static string ApiResponse(string data)
    {
      return data;
    }

    /* sendAPI is called from the main class to send POST requests to the API.
         * Primarily used for login, as most of what we need to do is handled through WS.
         * REDONE: Now returns the response as string, process in main.
         */
    public async Task<string> SendApi(string action, List<KeyValuePair<string, string>> data)
    {
      Logger.Debug("SendAPI was called with action" + action);
      try
      {
        using (var client = new HttpClient())
        {
          var content = new FormUrlEncodedContent(data);
          var response =
            await client.PostAsync(Settings.Default.APIURL + action, content); //TODO: This does not pull port number!
          Logger.Debug("AsyncPost sent.");
          if (response.IsSuccessStatusCode)
          {
            var responseString = await response.Content.ReadAsStringAsync();
            Logger.Debug("Starting response string parse task.");
            return ApiResponse(responseString);
          }

          Logger.Info("HTTP request returned an error:" + response.StatusCode);
          return "";
          //connectWS();
        }
      }
      catch (Exception ex)
      {
        Logger.Fatal("Well, that didn't go well. SendAPI exception: ", ex);
        return "";
      }
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

    private async void websocket_Client_Closed(object sender, EventArgs e)
    {
      if (_stopping)
      {
        Logger.Info("Disconnected from API WS server, stopping...");
      }
      else
      {
        if (Ws.State == WebSocketState.Connecting)
        {
          Logger.Debug("Connection attempt already underway, ignoring reconnect attempt.");
          return;
        }
        if (_failing)
        {
          Logger.Info("API reconnect still failing. Waiting...");
          await Task.Delay(20000);
          OpenWs();
          return;
        }

        Logger.Info("API WS Connection closed unexpectedly. Reconnecting in five seconds...");
        _failing = true;
        await Task.Delay(5000);
        OpenWs();
      }
    }

    private void websocketClient_MessageReceieved(object sender, MessageReceivedEventArgs e)
    {
      /* We can let this go away fairly soon, since there's not much we can do in the API Worker anyways
             * at this point. Maybe attach a logger here?
             */
      dynamic data = JsonConvert.DeserializeObject(e.Message);
      //TODO: Implement error handling.
      if (data.errors != null)
      {
        Logger.Fatal("The API returned an error when deserializing in APIWorker! " + data);
        return;
      }

      //TODO: Implement actual pass to our 3PA logic.
      if (data.application != null)
      {
        Logger.Debug("Got an application message, pass to own parser.");
        return;
      }

      switch ((string) data.action)
      {
        case "welcome":
          Logger.Info("API MOTD: " + data.data);
          break;
      }

      //appendStatus("Direct parse. Type:" + data.type + " Data:" + data.data);
    }

    // ReSharper disable once UnusedMember.Local TODO
    private void SubmitPaperwork(string url)
    {
      Process.Start(url);
    }
  }
}