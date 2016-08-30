using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.Web;
using System.Net.Http;
using System.Diagnostics;
using System.IO;
using WebSocket4Net;
using Newtonsoft.Json;
using ErrorEventArgs = SuperSocket.ClientEngine.ErrorEventArgs;
using System.Net;
using log4net;
using RatTracker_WPF.Models.Api;
using System.Security.Principal;
using System.Windows;

namespace RatTracker_WPF
{

	/*
     * APIWorker provides both HTTP and Websocket connection to the API. 
     */
	public class ApiWorker
	{
		private static readonly ILog logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		private bool stopping;
		public WebSocket ws;
		private bool failing;
		/*
         * queryAPI sends a GET request to the API. Kindasorta deprecated behavior.
         */
		public void InitWs()
		{
			if (Thread.CurrentThread.Name == null)
			{
				Thread.CurrentThread.Name = "APIWorker";
			}

			try
			{
				const string wsurl = "ws://orthanc.localecho.net:7070/"; //TODO: Remove this hardcoding!
																   //string wsurl = "ws://dev.api.fuelrats.com/";
				logger.Info("Connecting to WS at " + wsurl);
				ws = new WebSocket(wsurl, "", WebSocketVersion.Rfc6455) {AllowUnstrustedCertificate = true};
				ws.Error += websocketClient_Error;
				ws.Opened += websocketClient_Opened;
				ws.MessageReceived += websocketClient_MessageReceieved;
				ws.Closed += websocket_Client_Closed;
				ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
			}
			catch (Exception ex)
			{
				logger.Fatal("Well, that went tits up real fast: " + ex.Message);
			}
		}

		public void OpenWs()
		{
			if (ws == null)
			{
				logger.Debug("Attempted to open uninitialized WS connection.");
				return;
			}
			ws.Open();
			logger.Debug("WS client is " + ws.State);
		}

		public void DisconnectWs()
		{
			stopping = true;
			ws?.Close();
		}

		public void SendWs(string action, IDictionary<string, string> data)
		{
			if (ws == null)
			{
				logger.Debug("Attempt to send data over uninitialized WS connection!");
				return;
			}

			switch (action)
			{
				case "rescues:read":
					logger.Debug("SendWS is requesting a rescues update.");
					break;
			}

			APIQuery myquery = new APIQuery
			{
				Action = action,
				Data = data
			};
			string json = JsonConvert.SerializeObject(myquery);
			logger.Debug("sendWS Serialized to: " + json);
			ws.Send(json);
		}

		public void SendQuery(APIQuery myquery)
		{
			string json = JsonConvert.SerializeObject(myquery);
			logger.Debug("Sent an APIQuery serialized as: " + json);
			ws.Send(json);
		}

		public void SendTpaMessage(TPAMessage message)
		{
			if (ws == null)
			{
				logger.Debug("Attempt to send TPA message over uninitialized WS connection!");
				return;
			}

			message.data.Add("platform", "PC");
			message.applicationId = Properties.Settings.Default.AppID;
			string json = JsonConvert.SerializeObject(message);
			logger.Debug("Serialized TPA data: " + json);
			ws.Send(json);
		}
		private async void websocket_Client_Closed(object sender, EventArgs e)
		{
			if (stopping)
			{
				logger.Info("Disconnected from API WS server, stopping...");
			}
			else
			{
				if (failing)
				{
					logger.Info("API reconnect failed. Waiting...");
					await Task.Delay(5000);
					OpenWs();
				}

				logger.Info("API WS Connection closed unexpectedly. Reconnecting...");
				failing = true;
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
				logger.Fatal("API error! " + data.data);
				return;
			}

			//TODO: Implement actual pass to our 3PA logic.
			if (data.application != null)
			{
				logger.Debug("Got an application message, pass to own parser.");
				return;
			}

			switch ((string)data.action)
			{
				case "welcome":
					logger.Info("API MOTD: " + data.data);
					break;
			}

			//appendStatus("Direct parse. Type:" + data.type + " Data:" + data.data);
		}

		public void websocketClient_Error(object sender, ErrorEventArgs e)
		{
			logger.Fatal("Websocket: Exception thrown: ", e.Exception);
		}

		public void websocketClient_Opened(object sender, EventArgs e)
		{
			failing = false;
			logger.Info("Websocket: Connection to API established.");
			SubscribeStream("0xDEADBEEF");
			APIQuery login = new APIQuery
			{
				Action = "users:read",
				Data = new Dictionary<string, string> {{"email", Properties.Settings.Default.APIUsername}}
			};
			SendQuery(login);
			logger.Info("Sent login for " + Properties.Settings.Default.APIUsername);
			//TODO: Put stream subscription messages here when Mecha goes live. Do we want to listen to ourselves?
		}
		public void SubscribeStream(string applicationId)
		{
			Dictionary<string, string> data = new Dictionary<string, string>();
			data.Add("action", "stream:subscribe");
			data.Add("applicationId", applicationId);
			logger.Debug("Subscribing to stream: " + applicationId);
			string json = JsonConvert.SerializeObject(data);
			ws.Send(json);
		}
		public async Task<string> QueryApi(string action, Dictionary<string, string> data)
		{
			try
			{
				using (var client = new HttpClient())
				{
					var content = new UriBuilder(Properties.Settings.Default.APIURL + action + "/")
					{
						Port = Properties.Settings.Default.APIPort
					};
					var query = HttpUtility.ParseQueryString(content.Query);
					foreach (KeyValuePair<string, string> entry in data)
					{
						query[entry.Key] = entry.Value;
					}

					content.Query = query.ToString();
					logger.Debug("Built query string:" + content);
					var response = await client.GetAsync(content.ToString());
					//appendStatus("AsyncPost sent.");
					if (response.IsSuccessStatusCode)
					{
						var responseString = await response.Content.ReadAsStringAsync();
						logger.Debug("Starting response string parse task.");
						return ApiGetResponse(responseString);
					}

					logger.Debug("HTTP request returned an error:" + response.StatusCode);
					return "";
				}
			}
			catch (Exception ex)
			{
				logger.Fatal("Exception in QueryAPI: ", ex);
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
			if (Properties.Settings.Default.OAuthToken != "")
			{
				logger.Info("Authenticating with OAuth token...");
				return "";
			}

			/* Connect to the API here. */
			logger.Debug("No OAuth token stored, attempting to authorize app.");
			ProcessStartInfo proc = new ProcessStartInfo
			{
				UseShellExecute = true,
				WorkingDirectory = Environment.CurrentDirectory,
				FileName = Process.GetCurrentProcess().MainModule.FileName,
				Verb = "runas"
			};
			WindowsIdentity identity = WindowsIdentity.GetCurrent();
			WindowsPrincipal principal = new WindowsPrincipal(identity);
			if (Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(@"RatTracker\shell\open\command", true) == null)
			{
				logger.Debug("No custom URI configured for RatTracker.");
			}
			else
			{
				Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(@"RatTracker\shell\open\command", true);
				string mypath = (string)key.GetValue("");
				logger.Debug("Have a URI handler registered: " + mypath + " and my procname is "+proc.FileName);
				if (!mypath.Contains(proc.FileName))
				{
					logger.Debug("URI handler is not set to current version of RatTracker, resetting...");
				}
				else
				{
					logger.Debug("RatTracker URI is properly configured, launching OAuth authentication process.");
					var authcontent = new UriBuilder(Path.Combine(Properties.Settings.Default.APIURL + "oauth2/authorise?client_id=RatTracker&scope=*&redirect_uri=rattracker://auth&state=preinit&response_type=code"))
					{
						Port = Properties.Settings.Default.APIPort
					};
					Process.Start(authcontent.ToString());
					Process.GetCurrentProcess().Kill();
					return "";
				}
			}

			logger.Debug("Passing elevation check.");
			if (!principal.IsInRole(WindowsBuiltInRole.Administrator))
			{
				logger.Debug("Not running as administrator, requesting elevation.");
				try
				{
					logger.Debug("Not admin and key does not exist, requesting elevation.");
					MessageBoxResult result = MessageBox.Show("RatTracker needs to set up a custom URL handler to handle OAuth authentication. This requires elevated privileges. Please click OK to restart RatTracker as Administrator. It will restart with normal privileges once the OAuth process is complete.", "RT Needs elevation", MessageBoxButton.YesNo, MessageBoxImage.Exclamation);
					switch (result)
					{
						case MessageBoxResult.Yes:
							Process.Start(proc);
							Process.GetCurrentProcess().Kill();  // Die! DIEEE!!!
							break;
						case MessageBoxResult.No:
							MessageBox.Show("RatTracker will not be able to connect to the API in this state! Continuing, but this will probably break RT.");
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
				Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.ClassesRoot;
				key.CreateSubKey("RatTracker", true);
				key = key.OpenSubKey("RatTracker", true);
				key.SetValue("URL Protocol", "");
				key.SetValue("DefaultIcon", "");
				key.CreateSubKey("shell", true);
				key = key.OpenSubKey("shell", true);
				key.CreateSubKey("open", true);
				key = key.OpenSubKey("open", true);
				key.CreateSubKey("command", true);
				key = key.OpenSubKey("command", true);
				key.SetValue("", "\"" + Process.GetCurrentProcess().MainModule.FileName + "\" \"%1\"");
				logger.Debug("Launching authorization process.");
				var authcontent =
					new UriBuilder(Path.Combine(Properties.Settings.Default.APIURL +"oauth2/authorise?client_id=RatTracker&scope=*&redirect_uri=rattracker://auth&state=preinit&response_type=code"))
					{
						Port = Properties.Settings.Default.APIPort
					};
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
	
		// ReSharper disable once UnusedMember.Local TODO
		private void SubmitPaperwork(string url)
        {
            Process.Start(url);
        }

        public static string ApiResponse(string data)
        {
            return data;
        }

        /* sendAPI is called from the main class to send POST requests to the API.
         * Primarily used for login, as most of what we need to do is handled through WS.
         * REDONE: Now returns the response as string, process in main.
         */
        public async Task<string> sendAPI(string action, List<KeyValuePair<string, string>> data)
        {
            logger.Debug("SendAPI was called with action" + action);
            try
            {
                using (var client = new HttpClient())
                {
                    var content = new FormUrlEncodedContent(data);
                    var response = await client.PostAsync(Properties.Settings.Default.APIURL + action, content); //TODO: This does not pull port number!
                    logger.Debug("AsyncPost sent.");
                    if (response.IsSuccessStatusCode)
                    {
                        var responseString = await response.Content.ReadAsStringAsync();
                        logger.Debug("Starting response string parse task.");
                        return ApiResponse(responseString);
                    }

					logger.Info("HTTP request returned an error:" + response.StatusCode);
	                return "";
	                //connectWS();
                }
            }
            catch (Exception ex)
            {
                logger.Fatal("Well, that didn't go well. SendAPI exception: ", ex);
                return "";
            }
        }

    }
}
