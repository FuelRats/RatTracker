using System;
using System.Net;
using log4net;
using Newtonsoft.Json;
using RatTracker.Properties;
using SuperSocket.ClientEngine;
using WebSocket4Net;

namespace RatTracker.Api.Fuelrats
{
  public class WebsocketHandler
  {
    private readonly ILog log;
    private WebSocket webSocket;

    public WebsocketHandler(ILog log)
    {
      this.log = log;
    }

    public event EventHandler<string> MessageReceived;

    public void Initialize(bool includeToken)
    {
      try
      {
        var wsurl = GetApiWssUrl(includeToken);
        log.Info("Connecting to WS at " + wsurl);
        webSocket = new WebSocket(wsurl, "", WebSocketVersion.Rfc6455) {Security = {AllowUnstrustedCertificate = true}};
        webSocket.Opened += WebSocketOnOpened;
        webSocket.Error += WebsocketClientError;
        webSocket.MessageReceived += WebsocketClientMessageReceieved;
        ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
        webSocket.Open();
      }
      catch (Exception ex)
      {
        log.Fatal("Well, that went tits up real fast: " + ex.Message);
      }
    }

    public void SendQuery(WebsocketMessage websocketMessage)
    {
      var json = JsonConvert.SerializeObject(websocketMessage.GetData());
      log.Debug("Sent an APIQuery serialized as: " + json);
      webSocket.Send(json);
    }

    private void WebSocketOnOpened(object o, EventArgs eventArgs)
    {
      SubscribeStream("0xDEADBEEF");
    }

    private void SubscribeStream(string applicationId)
    {
      var query = WebsocketMessage.Request("stream", "subscribe", ApiEventNames.StreamSubscribe);
      query.AddData("id", applicationId);
      log.Debug("Subscribing to stream: " + applicationId);
      SendQuery(query);
    }

    private void WebsocketClientMessageReceieved(object sender, MessageReceivedEventArgs e)
    {
      MessageReceived?.Invoke(this, e.Message);
    }

    private static string GetApiWssUrl(bool includeToken)
    {
      var apiurl = Settings.Default.ApiUrl.Replace("https://", "wss://").Replace("http://", "ws://");
      if (apiurl.EndsWith("/"))
      {
        apiurl = apiurl.Substring(0, apiurl.Length - 1);
      }

      apiurl = $"{apiurl}:{Settings.Default.ApiPort}/";
      if (includeToken)
      {
        apiurl += $"?bearer={Settings.Default.OAuthToken}";
      }

      return apiurl;
    }

    private void WebsocketClientError(object sender, ErrorEventArgs e)
    {
      log.Fatal("Websocket: Exception thrown: ", e.Exception);
    }
  }
}