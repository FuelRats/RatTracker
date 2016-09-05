using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml;
using System.Xml.Linq;
using log4net;
using Microsoft.ApplicationInsights;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RatTracker_WPF.Models.Api;
using RatTracker_WPF.Models.App;
using RatTracker_WPF.Models.Edsm;
using RatTracker_WPF.Models.NetLog;
using RatTracker_WPF.Properties;
using WebSocket4Net;
using RatTracker_WPF.Models.Eddb;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using Microsoft.ApplicationInsights.DataContracts;


namespace RatTracker_WPF
{
	/// <summary>
	///     Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window, INotifyPropertyChanged
	{
		#region GlobalVars
		// private const bool TestingMode = true; // Use the TestingMode bool to point RT to test API endpoints and non-live queries. Set to false when deployed.
		private const string Unknown = "unknown";
		/* These can not be static readonly. They may be changed by the UI XML pulled from E:D. */
		public static readonly Brush RatStatusColourPositive = Brushes.LightGreen;
		public static readonly Brush RatStatusColourPending = Brushes.Orange;
		public static readonly Brush RatStatusColourNegative = Brushes.Red;
		private const string EdsmUrl = "http://www.edsm.net/api-v1/";
		private bool OauthProcessing = false;

		private static readonly ILog logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		private static readonly EdsmCoords fuelumCoords = new EdsmCoords() {X = 52, Y = -52.65625, Z = 49.8125}; // Static coords to Fuelum, saves a EDSM query

		//private readonly SpVoice voice = new SpVoice();
		private ApiWorker apworker; // Provides connection to the API
		private string assignedRats; // String representation of assigned rats to current case, bound to the UI 
		public ConnectionInfo conninfo = new ConnectionInfo(); // The rat's connection information
		private double distanceToClient; // Bound to UI element
		private string distanceToClientString; // Bound to UI element
		private long fileOffset; // Current offset in NetLog file
		private long fileSize; // Size of current netlog file
		private string jumpsToClient; // Bound to UI element

		// ReSharper disable once UnusedMember.Local TODO ??
		private string logDirectory = Settings.Default.NetLogPath; // TODO: Remove this assignment and pull live from Settings, have the logfile watcher reaquire file if settings are changed.
		private FileInfo logFile; // Pointed to the live netLog file.
		private ClientInfo myClient = new ClientInfo(); // Semi-redundant UI bound data model. Somewhat duplicates myrescue, needs revision.
		private PlayerInfo myplayer = new PlayerInfo(); // Playerinfo, bound to various UI elements
		Datum myrescue; // TODO: See myClient - must be refactored.
		private ICollection<TravelLog> myTravelLog; // Log of recently visited systems.
		private Overlay overlay; // Pointer to UI overlay
		private RootObject rescues; // Current rescues. Source for items in rescues datagrid
		private string scState; // Supercruise state.
		public bool stopNetLog; // Used to terminate netlog reader thread.
		private readonly TelemetryClient tc = new TelemetryClient(); 
		private Thread threadLogWatcher; // Holds logwatcher thread.
		private FileSystemWatcher watcher; // FSW for the Netlog directory.
		private EddbData eddbworker;
		
		// TODO
#pragma warning disable 649
		private string oAuthCode;
#pragma warning restore 649
		#endregion
		
		public MainWindow()
		{
			logger.Info("---Starting RatTracker---");
			try
			{
				foreach (string arg in Environment.GetCommandLineArgs())
				{
					logger.Debug("Arg: " + arg);
					if (arg.Contains("rattracker"))
					{
						logger.Debug("RatTracker was invoked for OAuth code authentication.");
						OauthProcessing = true;
						string reMatchToken = ".*?code=(.*)?&state=preinit";
						Match match = Regex.Match(arg, reMatchToken, RegexOptions.IgnoreCase);
						if (match.Success)
						{
							logger.Debug("Calling OAuth authentication...");
							OAuth_Authorize(match.Groups[1].ToString());
						}
						else
						{
							logger.Debug("Failed to match token?!!");
						}
					}
				}
			}
			catch(Exception ex)
			{
				logger.Debug("Exception in token parse: "+ex.Message);
				return;
			}

			tc.Context.Session.Id = Guid.NewGuid().ToString();
			tc.Context.Device.OperatingSystem = Environment.OSVersion.ToString();
			tc.Context.User.Id = Environment.UserName;
			tc.Context.Component.Version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
			tc.TrackPageView("MainWindow");
			InitializeComponent();
			this.Loaded += Window_Loaded;
			logger.Debug("Parsing AppConfig...");
			if (ParseEdAppConfig())
			{
				CheckLogDirectory();
			}
			else
			{
				AppendStatus("RatTracker does not have a valid path to your E:D directory. This will probably break RT! Please check your settings.");
			}
		}

		private async void OAuth_Authorize(string code)
		{
			if (code.Length > 0)
			{
				logger.Debug("A code was passed to connectAPI, attempting token exchange.");
				using (HttpClient hc = new HttpClient())
				{
					Auth myauth = new Auth
					{
						code = code,
						grant_type = "authorization_code",
						redirect_url = "rattracker://auth"
					};
					string json = JsonConvert.SerializeObject(myauth);
					logger.Debug("Passing auth JSON: " + json);
					var content = new UriBuilder(Path.Combine(Settings.Default.APIURL + "oauth2/token"))
					{
						Port = Settings.Default.APIPort
					};
					logger.Debug("Passing code: " + code);
					hc.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes(
						"4eace3e0-6564-4d41-87d4-bae2e2d2f6df:0f9b77d273f97cdd2341af31c3cbb373be65229192c4c98d")));
					logger.Debug("Built query string:" + content);
					var formenc = new FormUrlEncodedContent(new[]
					{
						new KeyValuePair<string,string>("code",code),
						new KeyValuePair<string,string>("grant_type","authorization_code"),
						new KeyValuePair<string,string>("redirect_uri","rattracker://auth")
					});
					HttpResponseMessage response = await hc.PostAsync(content.ToString(), formenc).ConfigureAwait(false);
					logger.Debug("AsyncPost sent with ConfigureAwait false.");
					HttpContent mycontent = response.Content;
					string data = mycontent.ReadAsStringAsync().Result;
					logger.Debug("Got data: " + data);
					if (data.Contains("access_token"))
					{
						logger.Debug("In access token true.");
						var token = JsonConvert.DeserializeObject<TokenResponse>(data);
						logger.Debug("Access token received: " + token.access_token);
						Settings.Default.OAuthToken = token.access_token;
						Settings.Default.Save();
						logger.Debug("Access token saved.");
						AppendStatus("OAuth authentication transaction successful, bearer token stored. Initializing RatTracker.");
						OauthProcessing = false;
						DoInitialize();
					}
					else
					{
						logger.Debug("No access token in data response!");
						AppendStatus("OAuth authentication failed. Please restart RatTracker to retry the operation.");
					}
				}
			}
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			DoInitialize();
		}

		public void DoInitialize()
		{
			BackgroundWorker apiWorker = new BackgroundWorker();
			BackgroundWorker eddbWorker = new BackgroundWorker();
			BackgroundWorker piWorker = new BackgroundWorker();

			if (OauthProcessing == false)
			{
				apiWorker.DoWork += (s, args) =>
				{
					logger.Debug("Initialize API...");
					InitApi(false);
				};
				eddbWorker.DoWork += async delegate
				{
					logger.Debug("Initialize EDDB...");
					await InitEddb();
				};
				piWorker.DoWork += async delegate
				{
					logger.Debug("Initialize player data...");
					await InitPlayer();
				};

				eddbWorker.RunWorkerAsync();
				apiWorker.RunWorkerAsync();
				piWorker.RunWorkerAsync();
			}
			else
			{
				logger.Debug("Skipping initialization, OAuth is processing.");
			}
		}

		public async void Reinitialize()
		{
			logger.Debug("Reinitializing application...");
			if (ParseEdAppConfig())
			{
				CheckLogDirectory();
			}
			else
			{
				AppendStatus("RatTracker does not have a valid path to your E:D directory. This will probably break RT! Please check your settings.");
			}

			InitApi(true); 
			await InitEddb();
			await InitPlayer();
			logger.Debug("Reinitialization complete.");
		}

		#region PropertyNotifiers
		public ObservableCollection<Datum> ItemsSource { get; } = new ObservableCollection<Datum>();
		public static ConcurrentDictionary<string, Rat> Rats { get; } = new ConcurrentDictionary<string, Rat>();
		public ConnectionInfo ConnInfo
		{
			get { return conninfo; }
			set
			{
				conninfo = value;
				NotifyPropertyChanged();
			}
		}

		public ClientInfo MyClient
		{
			get { return myClient; }
			set
			{
				myClient = value;
				NotifyPropertyChanged();
			}
		}

		public string JumpsToClient
		{
			get { return string.IsNullOrWhiteSpace(jumpsToClient) ? Unknown : "~" + jumpsToClient; }
			set
			{
				jumpsToClient = value;
				NotifyPropertyChanged();
			}
		}

		public double DistanceToClient
		{
			get { return distanceToClient; }
			set
			{
				distanceToClient = value;
				NotifyPropertyChanged();
				DistanceToClientString = string.Empty;
			}
		}

		public string DistanceToClientString
		{
			get
			{
				return DistanceToClient >= 0
					? DistanceToClient.ToString()
					: !string.IsNullOrWhiteSpace(distanceToClientString) ? distanceToClientString : Unknown;
			}
			set
			{
				distanceToClientString = value;
				NotifyPropertyChanged();
			}
		}

		public string AssignedRats
		{
			get { return assignedRats; }
			set
			{
				assignedRats = value;
				NotifyPropertyChanged();
			}
		}

		public PlayerInfo MyPlayer
		{
			get { return myplayer; }
			set
			{
				myplayer = value;
				NotifyPropertyChanged();
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;
		#endregion

		#region Initializers

		private void InitApi(bool reinitialize)
		{
			try
			{
				logger.Info("Initializing API connection...");
				if(apworker==null)
					apworker = new ApiWorker();
				apworker.InitWs();
				apworker.OpenWs();
				if (!reinitialize)
				{
					apworker.ws.MessageReceived += websocketClient_MessageReceieved;
					apworker.ws.Opened += websocketClient_Opened;
				}

				if (oAuthCode != null)
				{
					ApiWorker.ConnectApi();
				}
				else
				{
					ApiWorker.ConnectApi();
				}
			}
			catch (Exception ex)
			{
				logger.Fatal("Exception in InitAPI: " + ex.Message);
			}
		}

		private void websocketClient_Opened(object sender, EventArgs e)
		{
			InitRescueGrid();
		}

		public Task InitPlayer()
		{
			myplayer.JumpRange = Settings.Default.JumpRange > 0 ? Settings.Default.JumpRange : 30;
			myplayer.CurrentSystem = "Fuelum";
			return Task.FromResult(true);
		}

		private async Task InitEddb()
		{
			AppendStatus("Initializing EDDB.");
			if (eddbworker == null)
			{
				eddbworker = new EddbData();
			}

			string status = await eddbworker.UpdateEddbData();
			AppendStatus("EDDB: " + status);
		}

		#endregion

		/* Moved WS connection to the apworker, but to actually parse the messages we have to hook the event
         * handler here too.
         */
		private async void websocketClient_MessageReceieved(object sender, MessageReceivedEventArgs e)
		{
			Dispatcher disp = Dispatcher;
			try
			{
				//logger.Debug("Raw JSON from WS: " + e.Message);
				dynamic data = JsonConvert.DeserializeObject(e.Message);
				dynamic meta = data.meta;
				dynamic realdata = data.data;
				logger.Debug("Meta data from API: " + meta);
				switch ((string)meta.action)
				{
					case "welcome":
						logger.Info("API MOTD: " + data.data);
						break;
					case "assignment":
						logger.Debug("Got a new assignment datafield: " + data.data);
						break;
					case "rescues:read":
						logger.Debug("Got a list of rescues: " + realdata);
						rescues = JsonConvert.DeserializeObject<RootObject>(e.Message);
						await disp.BeginInvoke(DispatcherPriority.Normal, (Action)(() => ItemsSource.Clear()));
						await disp.BeginInvoke(DispatcherPriority.Normal, (Action)(() => rescues.Data.ForEach(datum => ItemsSource.Add(datum))));
						//await disp.BeginInvoke(DispatcherPriority.Normal,
						//	(Action)(() => RescueGrid.ItemsSource = rescues.Data));
						await GetMissingRats(rescues);
						break;
					case "message:send":
						/* We got a message broadcast on our channel. */
						AppendStatus("Test 3PA data from WS receieved: " + realdata);
						break;
					case "users:read":
						logger.Info("Parsing login information..." + meta.count + " elements");
						logger.Debug("Raw: " + realdata[0]);

						AppendStatus("Got user data for " + realdata[0].email);
						MyPlayer.RatId = new List<string>();
						foreach (dynamic cmdrdata in realdata[0].CMDRs)
						{
							AppendStatus("RatID " + cmdrdata + " added to identity list.");
							MyPlayer.RatId.Add(cmdrdata.ToString());
						}
						myplayer.RatName = await GetRatName(MyPlayer.RatId.FirstOrDefault()); // This will have to be redone when we go WS, as we can't load the variable then.
						break;
					case "rats:read":
						logger.Info("Received rat identification: " + meta.count + " elements");
						logger.Debug("Raw: " + realdata[0]);
						break;

					case "rescue:updated":
						Datum updrescue = realdata.ToObject<Datum>();
						if (updrescue == null)
						{
							logger.Debug("null rescue update object, breaking...");
							break;
						}
						logger.Debug("Updrescue _ID is " + updrescue.id);
						Datum myRescue = rescues.Data.FirstOrDefault(r => r.id == updrescue.id);
						if (myRescue == null)
						{
							logger.Debug("Myrescue is null in updaterescue, reinitialize grid.");
							APIQuery rescuequery = new APIQuery
							{
								action = "rescues:read",
								data = new Dictionary<string, string> {{"open", "true"}}
							};
							apworker.SendQuery(rescuequery);
							break;
						}
						if (updrescue.Open == false)
						{
							AppendStatus("Rescue closed: " + updrescue.Client);
							logger.Debug("Rescue closed: " + updrescue.Client);
							await disp.BeginInvoke(DispatcherPriority.Normal, (Action)(() => ItemsSource.Remove(myRescue)));
						}
						else
						{
							rescues.Data[rescues.Data.IndexOf(myRescue)] = updrescue;
							await disp.BeginInvoke(DispatcherPriority.Normal, (Action)(() => ItemsSource[ItemsSource.IndexOf(myRescue)] = updrescue));
							logger.Debug("Rescue updated: " + updrescue.Client);
						}
						break;
					case "rescue:created":
						Datum newrescue = realdata.ToObject<Datum>();
						AppendStatus("New rescue: " + newrescue.Client);
						OverlayMessage nr = new OverlayMessage
						{
							Line1Header = "New rescue:",
							Line1Content = newrescue.Client,
							Line2Header = "System:",
							Line2Content = newrescue.System,
							Line3Header = "Platform:",
							Line3Content = newrescue.Platform,
							Line4Header = "Press Ctrl-Alt-C to copy system name to clipboard"
						};
						if (overlay != null)
						{
							await disp.BeginInvoke(DispatcherPriority.Normal, (Action)(() => overlay.Queue_Message(nr, 30)));
						}

						await disp.BeginInvoke(DispatcherPriority.Normal, (Action)(() => ItemsSource.Add(newrescue))); 
						break;
					case "stream:subscribe":
						logger.Debug("Subscribed to 3PA stream " + data.ToString());
						break;
					default:
						logger.Info("Unknown API action field: " + meta.action);
						//tc.TrackMetric("UnknownAPIField", 1, new IDictionary<string,string>["type", meta.action]);
						break;
				}
				if (meta.id != null)
				{
					AppendStatus("My connID: " + meta.id);
				}
			}
			catch (Exception ex)
			{
				logger.Fatal("Exception in WSClient_MessageReceived: " + ex.Message);
				tc.TrackException(ex);
			}
		}

		/*
		 * Application Insights exception tracking. This SHOULD send off any unhandled fatal exceptions
		 * to AI for investigation.
		 */
		// ReSharper disable once UnusedParameter.Global  TODO what to do with this?
		public void TrackFatalException(Exception ex)
		{
			var exceptionTelemetry = new ExceptionTelemetry(new Exception())
			{
				HandledAt = ExceptionHandledAt.Unhandled
			};
			tc.TrackException(exceptionTelemetry);
		}

		// ReSharper disable once UnusedMember.Local TODO what to do with this?
		// ReSharper disable once UnusedParameter.Local TODO what to do with this?
		private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			TrackFatalException(e.ExceptionObject as Exception);
			tc.Flush();
		}
		/*
		 * Parses E:D's AppConfig and looks for the configuration variables we need to make RT work.
		 * Offers to change them if not set correctly.
		 */
		public bool ParseEdAppConfig()
		{
			string edProductDir = Settings.Default.EDPath + "\\Products";
			logger.Debug("Looking for Product dirs in " + Settings.Default.EDPath + "\\Products");
			try {
				if (!Directory.Exists(edProductDir))
				{
					logger.Fatal("Couldn't find E:D product directory, looking for Windows 10 installation...");
					edProductDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Frontier_Developments\\Products"; //Attempt Windows 10 path.
					logger.Debug("Looking in " + edProductDir);
					if (!Directory.Exists(edProductDir))
					{
						logger.Fatal("Couldn't find E:D product directory. Aborting AppConfig parse. You must set the path manually in settings.");
						return false;
					}

					logger.Debug("Found Windows 10 installation. Setting application paths...");
					Settings.Default.EDPath = edProductDir;
					Settings.Default.NetLogPath = edProductDir + "\\logs";
					Settings.Default.Save();
				}
			}
			catch (Exception ex)
			{
				logger.Fatal("Error during edProductDir check?!" + ex.Message);
				tc.TrackException(ex);
			}

			foreach (string dir in Directory.GetDirectories(edProductDir))
			{
				if (dir.Contains("COMBAT_TUTORIAL_DEMO"))
				{
					break; // We don't need to do AppConfig work on that.
				}

				logger.Info("Checking AppConfig in Product directory " + dir);
				try
				{
					logger.Debug("Loading " + dir + @"\AppConfig.xml");
					XDocument appconf = XDocument.Load(dir + @"\AppConfig.xml");

					XElement networknode = appconf.Element("AppConfig").Element("Network");
					if (networknode.Attribute("VerboseLogging") == null)
					{
						// Nothing is set up! This makes testing the attributes difficult, so initialize VerboseLogging at least.
						networknode.SetAttributeValue("VerboseLogging", 0);
						logger.Info("No VerboseLogging configuration at all. Setting temporarily for testing.");
					}

					var xAttribute = networknode.Attribute("VerboseLogging");
					if (xAttribute == null ||
					    (xAttribute.Value == "1" && networknode.Attribute("ReportSentLetters") != null &&
					     networknode.Attribute("ReportReceivedLetters") != null)) continue;
					logger.Error("WARNING: Your Elite:Dangerous AppConfig is not set up correctly to allow RatTracker to work!");
					MessageBoxResult result =
						MessageBox.Show(
							"Your AppConfig in " + dir +
							" is not configured correctly to allow RatTracker to perform its function. Would you like to alter the configuration to enable Verbose Logging? Your old AppConfig will be backed up.",
							"Incorrect AppConfig", MessageBoxButton.YesNo, MessageBoxImage.Exclamation);
					tc.TrackEvent("AppConfigNotCorrectlySetUp");
					switch (result)
					{
						case MessageBoxResult.Yes:
							File.Copy(dir + @"\AppConfig.xml", dir + @"\AppConfig-BeforeRatTracker.xml", true);

							networknode.SetAttributeValue("VerboseLogging", "1");
							networknode.SetAttributeValue("ReportSentLetters", 1);
							networknode.SetAttributeValue("ReportReceivedLetters", 1);
							XmlWriterSettings settings = new XmlWriterSettings
							{
								OmitXmlDeclaration = true,
								Indent = true,
								NewLineOnAttributes = true
							};
							using (XmlWriter xw = XmlWriter.Create(dir + @"\AppConfig.xml", settings))
							{
								appconf.Save(xw);
							}

							logger.Info("Wrote new configuration to " + dir + @"\AppConfig.xml");
							tc.TrackEvent("AppConfigAutofixed");
							return true;
						case MessageBoxResult.No:
							logger.Info("No alterations performed.");
							tc.TrackEvent("AppConfigDenied");
							return false;
						case MessageBoxResult.None:
							break;
						case MessageBoxResult.OK:
							break;
						case MessageBoxResult.Cancel:
							break;
						default:
							throw new ArgumentOutOfRangeException();
					}

					return true;
				}
				catch (Exception ex)
				{
					logger.Fatal("Exception in AppConfigReader!", ex);
					tc.TrackException(ex);
					return false;
				}
			}

			return true;
		}

		/*
		 * Appends text to our status display window.
		 */
		public void AppendStatus(string text)
		{
			if (StatusDisplay.Dispatcher.CheckAccess())
			{
				StatusDisplay.Text += "\n" + text;
				StatusDisplay.ScrollToEnd();
				StatusDisplay.CaretIndex = StatusDisplay.Text.Length;
			}
			else
			{
				StatusDisplay.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action<string>(AppendStatus), text);
			}
		}

		/*
		 * Converter for E:Ds UTF encoded CMDR names. Seen in NetLog.
		 */
		public static byte[] StringToByteArray(string hex)
		{
			return Enumerable.Range(0, hex.Length)
				.Where(x => x%2 == 0)
				.Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
				.ToArray();
		}

		private void OnRenamed(object source, RenamedEventArgs e)
		{
			/* Stop watching the renamed file, look for new onChanged. */
		}


		private void ParseFriendsList(string friendsList)
		{
			/* Sanitize the XML, it can break if over 40 friends long or so. */
			int count = 0;
			string xmlData = friendsList.Substring(friendsList.IndexOf("<", StringComparison.Ordinal) + friendsList.Length);
			logger.Debug("Raw xmlData: " + xmlData);
			try
			{
				XDocument xdoc = XDocument.Parse(friendsList);
				logger.Debug("Successful XML parse.");
				XElement rettest = xdoc.Element("OK");
				if (rettest != null)
				{
					logger.Debug("Last friendslist action: " + xdoc.Element("OK").Value);
				}

				IEnumerable<XElement> friends = xdoc.Descendants("item");
				foreach (XElement friend in friends)
				{
					/* string preencode = Regex.Replace(friend.Element("name").Value, ".{2}", "\\x$0"); */
					byte[] byteenc = StringToByteArray(friend.Element("name").Value);
					//appendStatus("Friend:" + System.Text.Encoding.UTF8.GetString(byteenc));
					count++;
					if (friend.Element("pending").Value == "1")
					{
						AppendStatus("Pending invite from CMDR " + Encoding.UTF8.GetString(byteenc) + "detected!");
						if (Encoding.UTF8.GetString(byteenc) == MyClient.ClientName)
						{
							MyClient.Self.FriendRequest = RequestState.Recieved;
							AppendStatus("FR is from our client. Notifying Dispatch.");
							TPAMessage frmsg = new TPAMessage
							{
								action = "FriendRequest:update",
								data = new Dictionary<string, string>
								{
									{"FRReceived", "true"},
									{"RatID", "abcdef1234567890"},
									{"RescueID", "abcdef1234567890"}
								}
							};
							apworker.SendTpaMessage(frmsg);
						}
					}
				}

				/* Check the OK status field, which can contain useful information on successful FRs. */
				foreach (XElement element in xdoc.Descendants())
				{
					if (element.Name != "OK") continue;
					logger.Debug("Return code: " + xdoc.Element("data").Element("OK").Value);
					var xElement = xdoc.Element("data");
					var o = xElement.Element("OK");
					if (o != null && (!o.Value.Contains("Invitation accepted"))) continue;
					AppendStatus("Friend request accepted.");
					MyClient.Self.FriendRequest = RequestState.Accepted;
				}

				AppendStatus("Parsed " + count + " friends in FRXML.");
			}
			catch (XmlException ex)
			{
				logger.Fatal("XML Parsing exception - Probably netlog overflow. " + ex.Message);
			}
			catch (Exception ex)
			{
				logger.Fatal("XML Parsing exception:" + ex.Message);
				tc.TrackException(ex);
			}
		}

		private void ParseWingInvite(string wingInvite)
		{
			logger.Debug("Raw xmlData: " + wingInvite);
			try
			{
				XDocument xdoc = XDocument.Parse(wingInvite);
				logger.Debug("Successful XML parse.");
				//voice.Speak("Wing invite detected.");
				IEnumerable<XElement> wing = xdoc.Descendants("commander");
				foreach (XElement wingdata in wing)
				{
					byte[] byteenc = StringToByteArray(wingdata.Element("name").Value);
					AppendStatus("Wingmember:" + Encoding.UTF8.GetString(byteenc));
					if (myrescue != null)
					{
						if (Encoding.UTF8.GetString(byteenc) == myrescue.Client)
						{
							AppendStatus("This data matches our current client! Storing information...");
							MyClient.ClientId = wingdata.Element("id").Value;
							AppendStatus("Wingmember IP data:" + xdoc.Element("connectionDetails"));
							string wingIPPattern = "IP4NAT:([0-9.]+):\\d+\\,";
							Match wingMatch = Regex.Match(wingInvite, wingIPPattern, RegexOptions.IgnoreCase);
							if (wingMatch.Success)
							{
								AppendStatus("Successful IP data match: " + wingMatch.Groups[1]);
								MyClient.ClientIp = wingMatch.Groups[1].Value;
							}

							/* If the friend request matches the client name, store his session ID. */
							MyClient.ClientId = wingdata.Element("commander_id").Value;
							MyClient.SessionId = wingdata.Element("session_runid").Value;
							MyClient.Self.WingRequest = RequestState.Recieved;
							TPAMessage wrmsg = new TPAMessage
							{
								action = "WingRequest:update",
								data = new Dictionary<string, string>
								{
									{"WRReceived", "true"},
									{"RatID", "abcdef1234567890"},
									{"RescueID", "abcdef1234567890"}
								}
							};
							apworker.SendTpaMessage(wrmsg);
						}
					}
				}
			}
			catch (Exception ex)
			{
				logger.Fatal("Error in parseWingInvite: " + ex.Message);
				tc.TrackException(ex);
			}
		}

		private void MainWindow_Closing(object sender, CancelEventArgs e)
		{
			stopNetLog = true;
			apworker?.DisconnectWs();
			tc?.Flush();
			Thread.Sleep(1000);
			Application.Current.Shutdown();
		}

		private void CheckLogDirectory()
		{
			logger.Debug("Checking log directories.");
			try {
				if (Thread.CurrentThread.Name == null)
				{
					Thread.CurrentThread.Name = "MainThread";
				}

				if (Settings.Default.NetLogPath == null | Settings.Default.NetLogPath == "")
				{
					MessageBox.Show("Error: No log directory is specified, please do so before attempting to go on duty.");
					return;
				}

				if (!Directory.Exists(Settings.Default.NetLogPath))
				{
					MessageBox.Show("Error: Couldn't find E:D Netlog directory: " + Settings.Default.NetLogPath +
									". Please ensure that it is correct in Settings.");
					return;
				}

				AppendStatus("Beginning to watch " + Settings.Default.NetLogPath + " for changes...");
				if (watcher == null)
				{
					watcher = new FileSystemWatcher
					{
						Path = Settings.Default.NetLogPath,
						NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName |
										NotifyFilters.DirectoryName | NotifyFilters.Size,
						Filter = "*.log"
					};
					watcher.Changed += OnChanged;
					watcher.Created += OnChanged;
					watcher.Deleted += OnChanged;
					watcher.Renamed += OnRenamed;
					watcher.EnableRaisingEvents = true;
				}

				DirectoryInfo tempDir = new DirectoryInfo(Settings.Default.NetLogPath);
				logFile = (from f in tempDir.GetFiles("netLog*.log") orderby f.LastWriteTime descending select f).First();
				AppendStatus("Started watching file " + logFile.FullName);
				logger.Debug("Watching file: " + logFile.FullName);
				CheckClientConn(logFile.FullName);
				ReadLogfile(logFile.FullName);
				myTravelLog = new Collection<TravelLog>();
			}
			catch (Exception ex)
			{
				logger.Debug("Exception in CheckLogDirectory! " + ex.Message);
				tc.TrackException(ex);
			}
		}

		private void CheckClientConn(string lf)
		{
			bool stopSnooping = false;
			AppendStatus("Detecting your connectivity...");
			try
			{
				using (
					StreamReader sr =
						new StreamReader(new FileStream(lf, FileMode.Open, FileAccess.Read,
							FileShare.ReadWrite | FileShare.Delete)))
				{
					int count = 0;
					while (stopSnooping != true && sr.Peek() != -1 && count < 10000)
					{
						count++;
						string line = sr.ReadLine();
						// TODO: Populate WAN, STUN and Turn server labels. Make cleaner TURN detection.
						if (line.Contains("Local machine is"))
						{
							logger.Info("My RunID: " + line.Substring(line.IndexOf("is ", StringComparison.Ordinal)));
							ConnInfo.RunId = line.Substring(line.IndexOf("is ", StringComparison.Ordinal));
						}
						if (line.Contains("RxRoute"))
						{
							// Yes, this early in the netlog, I figure we can just parse the RxRoute without checking for ID. Don't do this later though.
							const string rxpattern = "IP4NAT:(\\d{1,3}\\.\\d{1,3}\\.\\d{1,3}\\.\\d{1,3})+:(\\d{1,5}),(\\d{1,3}\\.\\d{1,3}\\.\\d{1,3}\\.\\d{1,3})+:(\\d{1,5}),(\\d{1,3}\\.\\d{1,3}\\.\\d{1,3}\\.\\d{1,3})+:(\\d{1,5}),(\\d{1,3}\\.\\d{1,3}\\.\\d{1,3}\\.\\d{1,3})+:(\\d{1,5}),(\\d),(\\d),(\\d),(\\d{1,4})";
							Match match = Regex.Match(line, rxpattern, RegexOptions.IgnoreCase);
							if (match.Success)
							{
								logger.Info("Route info: WAN:" + match.Groups[1].Value + " port " + match.Groups[2].Value + ", LAN:" +
											match.Groups[3].Value + " port " + match.Groups[4].Value + ", STUN: " + match.Groups[5].Value + ":" +
											match.Groups[6].Value + ", TURN: " + match.Groups[7].Value + ":" + match.Groups[8].Value +
											" MTU: " + match.Groups[12].Value + " NAT type: " + match.Groups[9].Value + " uPnP: " +
											match.Groups[10].Value + " MultiNAT: " + match.Groups[11].Value);
								ConnInfo.WanAddress = match.Groups[1].Value + ":" + match.Groups[2].Value;
								ConnInfo.Mtu = int.Parse(match.Groups[12].Value);
								tc.TrackMetric("Rat_Detected_MTU", ConnInfo.Mtu);
								ConnInfo.NatType = (NatType) Enum.Parse(typeof (NatType), match.Groups[9].Value);
								if (match.Groups[2].Value == match.Groups[4].Value && match.Groups[10].Value == "0")
								{
									logger.Debug("Probably using static portmapping, source and destination port matches and uPnP disabled.");
									ConnInfo.PortMapped = true;
								}
								/*
                                 * This is not detecting properly. Why? 
                                 *
                                if (match.Groups[11].Value == "0")
                                {
                                    AppendStatus("Warning: E:D thinks you have multiple levels of NAT! This is VERY bad for instancing. If possible, ensure you have only one NAT device between your computer and the internet.");
                                    tc.TrackMetric("MultipleNAT", 1);
                                }
                                */
								ConnInfo.TurnServer = match.Groups[7].Value + ":" + match.Groups[8].Value;
							}
						}
						if (line.Contains("failed to initialise upnp"))
						{
							AppendStatus(
								"CRITICAL: E:D has failed to establish a upnp port mapping, but E:D is configured to use upnp. Disable upnp in netlog if you have a router that can't do UPnP, and forward ports manually.");
						}
						if (line.Contains("Sync Established"))
						{
							AppendStatus("Sync Established.");
						}

						if (line.Contains("ConnectToServerActivity:StartRescueServer"))
						{
							AppendStatus(
								"Client connectivity parsing complete.");
							stopSnooping = true;
						}
					}

					AppendStatus("Parsed " + count + " lines to derive client info.");
					switch (ConnInfo.NatType)
					{
						case NatType.Blocked:
							AppendStatus(
								"WARNING: E:D reports that your network port appears to be blocked! This will prevent you from instancing with other players!");
							ConnTypeLabel.Content = "Blocked!";
							ConnTypeLabel.Foreground = Brushes.Red;
							tc.TrackMetric("NATBlocked", 1);
							break;
						case NatType.Unknown:
							if (ConnInfo.PortMapped != true)
							{
								AppendStatus(
									"WARNING: E:D is unable to determine the status of your network port. This may be indicative of a condition that may cause instancing problems!");
								ConnTypeLabel.Content = "Unknown";
								tc.TrackMetric("NATUnknown", 1);
							}
							else
							{
								AppendStatus("Unable to determine NAT type, but you seem to have a statically mapped port forward.");
								tc.TrackMetric("ManualPortMap", 1);
							}
							break;
						case NatType.Open:
							ConnTypeLabel.Content = "Open";
							tc.TrackMetric("NATOpen", 1);
							break;
						case NatType.FullCone:
							ConnTypeLabel.Content = "Full cone NAT";
							tc.TrackMetric("NATFullCone", 1);
							break;
						case NatType.Failed:
							AppendStatus("WARNING: E:D failed to detect your NAT type. This might be problematic for instancing.");
							ConnTypeLabel.Content = "Failed to detect!";
							tc.TrackMetric("NATFailed", 1);
							break;
						case NatType.SymmetricUdp:
							if (ConnInfo.PortMapped != true)
							{
								AppendStatus(
									"WARNING: Symmetric NAT detected! Although your NAT allows UDP, this may cause SEVERE problems when instancing!");
								ConnTypeLabel.Content = "Symmetric UDP";
								tc.TrackMetric("NATSymmetricUDP", 1);
							}

							else
							{
								AppendStatus("Symmetric UDP NAT with static port mapping detected.");
								tc.TrackMetric("ManualPortMap", 1);
							}

							break;
						case NatType.Restricted:
							if (ConnInfo.PortMapped != true)
							{
								AppendStatus("WARNING: Port restricted NAT detected. This may cause instancing problems!");
								ConnTypeLabel.Content = "Port restricted NAT";
								tc.TrackMetric("NATRestricted", 1);
							}
							else
							{
								AppendStatus("Port restricted NAT with static port mapping detected.");
								tc.TrackMetric("ManualPortMap", 1);
							}
							break;
						case NatType.Symmetric:
							if (ConnInfo.PortMapped != true)
							{
								AppendStatus(
									"WARNING: Symmetric NAT detected. This is usually VERY BAD for instancing. If you do not have a manual port mapping set up, you should consider changing your network configuration.");
								tc.TrackMetric("NATSymmetric", 1);
							}
							else
							{
								AppendStatus("Symmetric NAT with static port mapping detected.");
								tc.TrackMetric("ManualPortMap", 1);
							}
							break;
					}
					if (stopSnooping == false)
					{
						AppendStatus(
							"Client connectivity detection complete.");
					}
					tc.Flush();
				}
			}
			catch (Exception ex)
			{
				logger.Fatal("Exception in checkClientConn:" + ex.Message);
				tc.TrackException(ex);
			}
		}

		private void ReadLogfile(string lf)
		{
			if (!File.Exists(lf))
			{
				AppendStatus("RatTracker tried to read a logfile that does not exist. Are you sure your path settings are correct?");
				return;
			}
			try
			{
				using (
					StreamReader sr =
						new StreamReader(new FileStream(lf, FileMode.Open, FileAccess.Read,
							FileShare.ReadWrite | FileShare.Delete)))
				{
					if (fileOffset == 0L)
					{
						logger.Debug("First peek...");
						if (sr.BaseStream.Length > 5000)
						{
							sr.BaseStream.Seek(-5000, SeekOrigin.End);
							/* First peek into the file, rewind a bit and scan from there. */
						}
					}
					else
					{
						sr.BaseStream.Seek(this.fileOffset, SeekOrigin.Begin);
					}

					while (sr.Peek() != -1)
					{
						string line = sr.ReadLine();
						if (string.IsNullOrEmpty(line))
						{
							logger.Error("Empty line while attempting to read a log line!");
						}
						else
						{
							ParseLine(line);
						}
					}

					//appendStatus("Parsed " + count + " new lines. Old fileOffset was "+fileOffset+" and length was "+logFile.Length);
				}
			}
			catch (Exception ex)
			{
				logger.Fatal("Exception in readLogFile: ", ex);
				logger.Debug("StackTrace: "+ ex.StackTrace);
				tc.TrackException(ex);
			}
		}

		private void ParseLine(string line)
		{
			try
			{
				if (string.IsNullOrEmpty(line))
				{
					logger.Error("ParseLine was passed a null or empty line. This should not happen!");
					return;
				}
				// string reMatchSystem = ".*?(System:).*?\\(((?:[^)]+)).*?\\)"; // Pre-1.6/2.1 style
				const string reMatchSystem = ".*?(System:)\"(.*)?\".*?\\(((?:[^)]+)).*?\\)";
				Match match = Regex.Match(line, reMatchSystem, RegexOptions.IgnoreCase);
				if (match.Success)
				{
					if (match.Groups[2].Value == myplayer.CurrentSystem)
						return;
					TriggerSystemChange(match.Groups[2].Value);
				}

				const string reMatchPlayer = "\\{.+\\} (\\d+) x (\\d+).*\\(\\(([0-9.]+):\\d+\\)\\)Name (.+)$";
				Match frmatch = Regex.Match(line, reMatchPlayer, RegexOptions.IgnoreCase);
				if (frmatch.Success)
				{
					if (scState == "Normalspace" && myrescue!=null)
					{
						AppendStatus("Successful ID match in normal space. Sending good instance.");
						MyClient.Self.InInstance = true;
						TPAMessage instmsg = new TPAMessage
						{
							action = "InstanceSuccessful:update",
							data = new Dictionary<string, string>
							{
								{"RatID", myplayer.RatId.ToString()},
								{"InstanceSuccessful", "true"},
								{"RescueID", myrescue.id}
							}
						};
						apworker.SendTpaMessage(instmsg);
					}
					AppendStatus("Successful identity match! ID: " + frmatch.Groups[1] + " IP:" + frmatch.Groups[3]);
				}

				const string reMatchNat = @"RxRoute:(\d+)+ Comp:(\d)\[IP4NAT:(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})+:(\d{1,5}),(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})+:(\d{1,5}),(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})+:(\d{1,5}),(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})+:(\d{1,5}),(\d),(\d),(\d),(\d{1,4})\]\[Relay:";
				Match natmatch = Regex.Match(line, reMatchNat, RegexOptions.IgnoreCase);
				if (natmatch.Success)
				{
					logger.Debug("Found NAT datapoint for runID " + natmatch.Groups[1] + ": " + natmatch.Groups[11]);
					NatType clientnat = (NatType) Enum.Parse(typeof (NatType), match.Groups[11].Value);
					switch (clientnat)
					{
						case NatType.Blocked:
							tc.TrackMetric("ClientNATBlocked", 1);
							break;
						case NatType.Unknown:
							tc.TrackMetric("ClientNATUnknown", 1);
							break;
						case NatType.Open:
							tc.TrackMetric("ClientNATOpen", 1);
							break;
						case NatType.FullCone:
							tc.TrackMetric("ClientNATFullCone", 1);
							break;
						case NatType.Failed:
							tc.TrackMetric("ClientNATFailed", 1);
							break;
						case NatType.SymmetricUdp:
							tc.TrackMetric("ClientNATSymmetricUDP", 1);
							break;
						case NatType.Restricted:
							tc.TrackMetric("ClientNATRestricted", 1);
							break;
						case NatType.Symmetric:
							tc.TrackMetric("ClientNATSymmetric", 1);
							break;
					}
					tc.Flush();
				}

				const string reMatchStats = "machines=(\\d+)&numturnlinks=(\\d+)&backlogtotal=(\\d+)&backlogmax=(\\d+)&avgsrtt=(\\d+)&loss=([0-9]*(?:\\.[0-9]*)+)&&jit=([0-9]*(?:\\.[0-9]*)+)&act1=([0-9]*(?:\\.[0-9]*)+)&act2=([0-9]*(?:\\.[0-9]*)+)";
				Match statmatch = Regex.Match(line, reMatchStats, RegexOptions.IgnoreCase);
				if (statmatch.Success)
				{
					logger.Info("Updating connection statistics.");
					ConnInfo.Srtt = int.Parse(statmatch.Groups[5].Value);
					ConnInfo.Loss = float.Parse(statmatch.Groups[6].Value);
					ConnInfo.Jitter = float.Parse(statmatch.Groups[7].Value);
					ConnInfo.Act1 = float.Parse(statmatch.Groups[8].Value);
					ConnInfo.Act2 = float.Parse(statmatch.Groups[9].Value);
					Dispatcher disp = Dispatcher;
					disp.BeginInvoke(DispatcherPriority.Normal,
						(Action)
							(() =>
								connectionStatus.Text =
									"SRTT: " + conninfo.Srtt + " Jitter: " + conninfo.Jitter + " Loss: " +
									conninfo.Loss + " In: " + conninfo.Act1 + " Out: " + conninfo.Act2));
				}
				if (line.Contains("<data>"))
				{
					logger.Debug("Line sent to XML parser");
					ParseFriendsList(line);
				}

				if (line.Contains("<FriendWingInvite>"))
				{
					logger.Debug("Wing invite detected, parsing...");
					ParseWingInvite(line);
				}

				if (line.Contains("JoinSession:WingSession:") && line.Contains(MyClient.ClientIp))
				{
					logger.Debug("Prewing communication underway...");
				}

				if (line.Contains("TalkChannelManager::OpenOutgoingChannelTo") && line.Contains(MyClient.ClientIp))
				{
					AppendStatus("Wing established, opening voice comms.");
					//voice.Speak("Wing established.");
					MyClient.Self.WingRequest = RequestState.Accepted;
				}

				if (line.Contains("ListenResponse->Listening (SUCCESS: User has responded via local talkchannel)"))
				{
					AppendStatus("Voice communications established.");
				}

				if (line.Contains("NormalFlight") && scState == "Supercruise")
				{
					scState = "Normalspace";
					logger.Debug("Drop to normal space detected.");
					//voice.Speak("Dropping to normal space.");
				}

				if (line.Contains("Supercruise") && scState == "Normalspace")
				{
					scState = "Supercruise";
					logger.Debug("Entering supercruise.");
					//voice.Speak("Entering supercruise.");
				}
				if (line.Contains("JoinSession:BeaconSession") && line.Contains(MyClient.ClientIp))
				{
					AppendStatus("Client's Beacon in sight.");
					MyClient.Self.Beacon = true;
					TPAMessage bcnmsg = new TPAMessage
					{
						action = "BeaconSpotted:update",
						data = new Dictionary<string, string>
						{
							{"BeaconSpotted", "true"},
							{"RatID", myplayer.RatId.ToString()},
							{"RescueID", myrescue.id}
						}
					};
					apworker.SendTpaMessage(bcnmsg);
				}
			}
			catch (Exception ex)
			{
				logger.Debug("Exception in ParseLine: " + ex.Message);
				tc.TrackException(ex);
			}
		}

		private async void TriggerSystemChange(string value)
		{
			Dispatcher disp = Dispatcher;
			if (value == MyPlayer.CurrentSystem)
			{
				return; // Already know we're in that system, thanks.
			}

			MyPlayer.CurrentSystem = value;
			try
			{
				tc.TrackEvent("SystemChange");
				using (HttpClient client = new HttpClient())
				{
					UriBuilder content = new UriBuilder(EdsmUrl + "systems?sysname=" + value + "&coords=1") {Port = -1};
					NameValueCollection query = HttpUtility.ParseQueryString(content.Query);
					content.Query = query.ToString();
					HttpResponseMessage response = await client.GetAsync(content.ToString());
					response.EnsureSuccessStatusCode();
					string responseString = await response.Content.ReadAsStringAsync();
					IEnumerable<EdsmSystem> m = JsonConvert.DeserializeObject<IEnumerable<EdsmSystem>>(responseString);
					EdsmSystem firstsys = m.FirstOrDefault();
					// EDSM should return the closest lexical match as the first element. Trust that - for now.
					if (firstsys.Name == value)
					{
						if (firstsys.Coords == default(EdsmCoords))
							logger.Debug("Got a match on " + firstsys.Name + " but it has no coords.");
						else
							logger.Debug("Got definite match in first pos, disregarding extra hits:" + firstsys.Name + " X:" +
										firstsys.Coords.X + " Y:" + firstsys.Coords.Y + " Z:" + firstsys.Coords.Z);
						//AppendStatus("Got M:" + firstsys.name + " X:" + firstsys.coords.x + " Y:" + firstsys.coords.y + " Z:" + firstsys.coords.z);
						if (myTravelLog == null)
						{
							myTravelLog=new Collection<TravelLog>();
						}

						myTravelLog.Add(new TravelLog() {System = firstsys, LastVisited = DateTime.Now});
						logger.Debug("Added system to TravelLog.");
						// Should we add systems even if they don't exist in EDSM? Maybe submit them?
					}

					if (myrescue != null)
					{
						if (myrescue.System == value)
						{
							AppendStatus("Arrived in client system. Notifying dispatch.");
							TPAMessage sysmsg = new TPAMessage
							{
								action = "SysArrived:update",
								data = new Dictionary<string, string>
								{
									{"SysArrived", "true"},
									{"RatID", myplayer.RatId.FirstOrDefault()},
									{"RescueID", myrescue.id}
								}
							};
							apworker.SendTpaMessage(sysmsg);
							MyClient.Self.InSystem = true;
						}
					}

					await disp.BeginInvoke(DispatcherPriority.Normal, (Action) (() => SystemNameLabel.Content = value));
					if (responseString.Contains("-1"))
					{
						await
							disp.BeginInvoke(DispatcherPriority.Normal,
								(Action) (() => SystemNameLabel.Foreground = Brushes.Red));
					}
					else
					{
						await
							disp.BeginInvoke(DispatcherPriority.Normal,
								(Action) (() => SystemNameLabel.Foreground = Brushes.Orange));
					}
					if (responseString.Contains("coords"))
					{
						await
							disp.BeginInvoke(DispatcherPriority.Normal,
								(Action) (() => SystemNameLabel.Foreground = Brushes.Green));
						logger.Debug("Getting distance from fuelum to " + firstsys.Name);
						double distance = await CalculateEdsmDistance("Fuelum", firstsys.Name);
						distance = Math.Round(distance, 2);
						await
							disp.BeginInvoke(DispatcherPriority.Normal,
								(Action) (() => distanceLabel.Content = distance + "LY from Fuelum"));
					}
				}
			}
			catch (Exception ex)
			{
				logger.Fatal("Exception in triggerSystemChange: " + ex.Message);
				tc.TrackException(ex);
			}
		}

		private void OnChanged(object source, FileSystemEventArgs e)
		{
			logFile = new FileInfo(e.FullPath);
			/* Handle changed events */
		}

		private void DutyButton_Click(object sender, RoutedEventArgs e)
		{
			if (MyPlayer.OnDuty == false)
			{
				Button.Content = "On Duty";
				MyPlayer.OnDuty = true;
				watcher.EnableRaisingEvents = true;
				AppendStatus("Started watching for events in netlog.");
				Button.Background = Brushes.Green;
				stopNetLog = false;
				threadLogWatcher = new Thread(NetLogWatcher) {Name = "Netlog watcher"};
				threadLogWatcher.Start();
			}
			else
			{
				Button.Content = "Off Duty";
				MyPlayer.OnDuty = false;
				watcher.EnableRaisingEvents = false;
				AppendStatus("\nStopped watching for events in netlog.");
				Button.Background = Brushes.Red;
				stopNetLog = true;
			}
			try {
				TPAMessage dutymessage = new TPAMessage
				{
					action = "OnDuty:update",
					data = new Dictionary<string, string>
					{
						{"OnDuty", MyPlayer.OnDuty.ToString()},
						{"RatID", myplayer.RatId.FirstOrDefault()},
						{"currentSystem", MyPlayer.CurrentSystem}
					}
				};
				apworker.SendTpaMessage(dutymessage);
			}
			catch (Exception ex)
			{
				logger.Fatal("Exception in sendTPAMessage: " + ex.Message);
				tc.TrackException(ex);
			}
		}

		private void NetLogWatcher()
		{
			AppendStatus("Netlogwatcher started.");
			try
			{
				while (!stopNetLog)
				{
					Thread.Sleep(2000);

					FileInfo fi = new FileInfo(logFile.FullName);
					if (fi.Length != fileSize)
					{
						ReadLogfile(fi.FullName); /* Maybe a poke on the FS is enough to wake watcher? */
						fileOffset = fi.Length;
						fileSize = fi.Length;
					}
				}
			}
			catch (Exception ex)
			{
				logger.Debug("Netlog exception: " + ex.Message);
				tc.TrackException(ex);
			}
		}

		private void Main_Menu_Click(object sender, RoutedEventArgs e)
		{
			/* Fleh? */
		}

		private void currentButton_Click(object sender, RoutedEventArgs e)
		{
			AppendStatus("Setting client location to current system: "+myplayer.CurrentSystem);
			// SystemName.Text = "Fuelum";
			// Do actual system name update through Mecha with 3PAM
		}

		private void UpdateButton_Click(object sender, RoutedEventArgs e)
		{
			// No more of this testing bull, let's actually send the updated system now.
			if (MyClient==null)
			{
				logger.Debug("No current rescue, ignoring system update request.");
				return;
			}

			TPAMessage systemmessage = new TPAMessage
			{
				action = "ClientSystem:update",
				data = new Dictionary<string, string>
				{
					{"SystemName", SystemName.Text},
					{"RatID", myplayer.RatId.FirstOrDefault()},
					{"RescueID", MyClient.Rescue.id}
				}
			};
			apworker.SendTpaMessage(systemmessage);
		}
		private async void InitRescueGrid()
		{
			try {
				logger.Info("Initializing Rescues grid");
				Dictionary<string, string> data = new Dictionary<string, string> {{"open", "true"}};
				rescues = new RootObject();
				if (apworker.ws.State != WebSocketState.Open)
				{
					logger.Info("No available WebSocket connection, falling back to HTML API.");
					string col = await apworker.QueryApi("rescues", data);
					logger.Debug(col == null ? "No COL returned from Rescues." : "Rescue data received from HTML API.");
				}
				else
				{
					logger.Info("Fetching rescues from WS API.");
					APIQuery rescuequery = new APIQuery
					{
						action = "rescues:read",
						data = new Dictionary<string, string> {{"open", "true"}}
					};
					apworker.SendQuery(rescuequery);
				}

				RescueGrid.AutoGenerateColumns = false;
				//await disp.BeginInvoke(DispatcherPriority.Normal, (Action)(() => ItemsSource.Clear()));
				//await disp.BeginInvoke(DispatcherPriority.Normal, (Action)(() => rescues.Data.ForEach(datum => ItemsSource.Add(datum))));
				//await GetMissingRats(rescues);
			}
			catch(Exception ex)
			{
				logger.Fatal("Exception in InitRescueGrid: " + ex.Message);
				tc.TrackException(ex);
			}
		}

		private async void RescueGrid_SelectionChanged(object sender, EventArgs e)
		{
			try
			{
				if (RescueGrid.SelectedItem == null)
					return;
				Datum myrow = (Datum)RescueGrid.SelectedItem;
				logger.Debug("Client is " + myrow.Client);

				var rats = Rats.Where(r => myrow.Rats.Contains(r.Key)).Select(r => r.Value.CmdrName).ToList();
				var count = rats.Count;

				/* TODO: Fix this. Needs to be smrt about figuring out if your own ratID is assigned. */
				MyClient = new ClientInfo { Rescue = myrow };
				if (count > 0)
				{
					MyClient.Self.RatName = rats[0]; // Nope! We have no guarantee that the first listed rat in the rescue is ourself.
				}
				if (count > 1)
				{
					MyClient.Rat2.RatName = rats[1];
				}
				if (count > 2)
				{
					MyClient.Rat3.RatName = rats[2];
				}

				DistanceToClient = -1;
				DistanceToClientString = "Calculating...";
				JumpsToClient = string.Empty;
				ClientName.Text = myrow.Client;

				AssignedRats = myrow.Rats.Any()
					? string.Join(", ", rats)
					: string.Empty;
				SystemName.Text = myrow.System;
				ClientDistance distance = await GetDistanceToClient(myrow.System);
				DistanceToClient = Math.Round(distance.Distance, 2);
				JumpsToClient = MyPlayer.JumpRange > 0 ? Math.Ceiling(distance.Distance / MyPlayer.JumpRange).ToString() : string.Empty;
			}
			catch(Exception ex)
			{
				logger.Fatal("Exception in RescueGrid_SelectionChanged: " + ex.Message);
				tc.TrackException(ex);
			}
		}

		private async Task GetMissingRats(RootObject localRescues)
		{
			try
			{
				IEnumerable<string> ratIdsToGet = new List<string>();
				if (localRescues.Data == null)
				{
					return;
				}

				IEnumerable<List<string>> datas = localRescues.Data.Select(d => d.Rats);
				ratIdsToGet = datas.Aggregate(ratIdsToGet, (current, list) => current.Concat(list));
				ratIdsToGet = ratIdsToGet.Distinct().Except(Rats.Values.Select(x => x.id));

				foreach (string ratId in ratIdsToGet)
				{
					string response = await apworker.QueryApi("rats", new Dictionary<string, string> { { "_id", ratId }, { "limit", "1" } });
					JObject jsonRepsonse = JObject.Parse(response);
					List<JToken> tokens = jsonRepsonse["data"].Children().ToList();
					Rat rat = JsonConvert.DeserializeObject<Rat>(tokens[0].ToString());
					Rats.TryAdd(ratId, rat);

					logger.Debug("Got name for " + ratId + ": " + rat.CmdrName);
				}
			}
			catch(Exception ex)
			{
				logger.Fatal("Exception in GetMissingRats: " + ex.Message);
				tc.TrackException(ex);
			}
		}

		private async Task<string> GetRatName(string ratid)
		{
			try
			{ 
				string response = await apworker.QueryApi("rats", new Dictionary<string, string> { { "_id", ratid }, { "limit", "1" } });
				JObject jsonRepsonse = JObject.Parse(response);
				List<JToken> tokens = jsonRepsonse["data"].Children().ToList();
				Rat rat = JsonConvert.DeserializeObject<Rat>(tokens[0].ToString());
				logger.Debug("Got name for " + ratid + ": " + rat.CmdrName);
				return rat.CmdrName;
			}
			catch(Exception ex)
			{
				logger.Fatal("Exception in GetRatName: " + ex.Message);
				tc.TrackException(ex);
				return "Unknown rat";
			}
		}
		private void StartButton_Click(object sender, RoutedEventArgs e)
		{
			AppendStatus("Starting case: " + ClientName.Text);
		}

		private void MenuItem_Click(object sender, RoutedEventArgs e)
		{
			wndSettings swindow = new wndSettings();
			bool? result = swindow.ShowDialog();
			if(result==true)
			{
				AppendStatus("Reinitializing application due to configuration change...");
				Reinitialize();
				return;
			}
			AppendStatus("No changes made, not reinitializing.");
		}

		#region EDSM
		public async Task<IEnumerable<EdsmSystem>> QueryEdsmSystem(string system)
		{
			logger.Debug("Querying EDSM for system " + system);
			if (system.Length < 3)
			{
				//This would pretty much download the entire EDSM database. Refuse to do it.
				logger.Fatal("Too short EDSM query passed to QueryEDSMSystem: " + system);
				return new List<EdsmSystem>();
			}
			try
			{
				tc.TrackEvent("EDSMQuery");
				using (HttpClient client = new HttpClient())
				{
					UriBuilder content = new UriBuilder(EdsmUrl + "systems?sysname=" + system + "&coords=1") {Port = -1};
					AppendStatus("Querying EDSM for " + system);
					logger.Debug("Building query: " + content);
					NameValueCollection query = HttpUtility.ParseQueryString(content.Query);
					content.Query = query.ToString();
					HttpResponseMessage response = await client.GetAsync(content.ToString());
					response.EnsureSuccessStatusCode();
					string responseString = response.Content.ReadAsStringAsync().Result;
					//logger.Debug("Got response: " + responseString[0-100]);
					if (responseString == "[]")
					{
						return new List<EdsmSystem>();
					}

					IEnumerable<EdsmSystem> m = JsonConvert.DeserializeObject<IEnumerable<EdsmSystem>>(responseString);
					return m;
				}
			}
			catch (Exception ex)
			{
				logger.Fatal("Exception in QueryEDSMSystem: " + ex.Message);
				tc.TrackException(ex);
				return new List<EdsmSystem>();
			}
		}

		// TODO: Be less stupid and actually support finding systems that AREN'T procedural. Duh.
		public async Task<IEnumerable<EdsmSystem>> GetCandidateSystems(string target)
		{
			logger.Debug("Finding candidate systems for " + target);
			try {
				string sysmatch = "([A-Z][A-Z]-[A-z]+) ([a-zA-Z])+(\\d+(?:-\\d+)+?)";
				Match mymatch = Regex.Match(target, sysmatch, RegexOptions.IgnoreCase);
				IEnumerable<EdsmSystem> candidates = await QueryEdsmSystem(target.Substring(0, target.IndexOf(mymatch.Groups[3].Value, StringComparison.Ordinal)));
				logger.Debug("Candidate count is " + candidates.Count() + " from a subgroup of " + mymatch.Groups[3].Value);
				tc.TrackMetric("CandidateCount", candidates.Count());
				var finalcandidates = candidates.Where(x => x.Coords != null).ToList();
				logger.Debug("FinalCandidates with coords only is size " + finalcandidates.Count);
				if (!finalcandidates.Any())
				{
					logger.Debug("No final candidates, widening search further...");
					candidates = await QueryEdsmSystem(target.Substring(0, target.IndexOf(mymatch.Groups[2].Value, StringComparison.Ordinal)));
					finalcandidates = candidates.Where(x => x.Coords != null).ToList();
					if (!finalcandidates.Any())
					{
						logger.Debug("Still nothing! Querying whole sector.");
						candidates = await QueryEdsmSystem(target.Substring(0, target.IndexOf(mymatch.Groups[1].Value, StringComparison.Ordinal)));
						finalcandidates = candidates.Where(x => x.Coords != null).ToList();
					}
				}

				return finalcandidates;
			}
			catch(Exception ex)
			{
				logger.Fatal("Exception in GetCandidateSystems: " + ex.Message);
				tc.TrackException(ex);
				return new List<EdsmSystem>();
			}
		}

		public async Task<ClientDistance> GetDistanceToClient(string target)
		{
			int logdepth = 0;
			EdsmCoords targetcoords =new EdsmCoords();
			ClientDistance cd = new ClientDistance();
			EdsmCoords sourcecoords = fuelumCoords;
			cd.SourceCertainty = "Fuelum";
			if (myTravelLog!=null)
			{
				foreach (TravelLog mysource in myTravelLog.Reverse())
				{
					if (mysource.System.Coords == null)
					{
						logdepth++;
					}
					else
					{
						logger.Debug("Found TL system to use: " + mysource.System.Name);
						sourcecoords = mysource.System.Coords;
						cd.SourceCertainty = logdepth.ToString();
						break;
					}
				}
			}
			IEnumerable<EdsmSystem> candidates = await QueryEdsmSystem(target);
			cd.TargetCertainty = "Exact";
			
			if (candidates == null || !candidates.Any())
			{
				logger.Debug("EDSM does not know system '" + target + "'. Widening search...");
				candidates = await GetCandidateSystems(target);
				cd.TargetCertainty = "Nearby";
			}
			if (candidates.FirstOrDefault().Coords == null)
			{
				logger.Debug("Known system '" + target + "', but no coords. Widening search...");
				candidates = await GetCandidateSystems(target);
				cd.TargetCertainty = "Region";
			}
			if (candidates == null || !candidates.Any())
			{
				//Still couldn't find something, abort.
				AppendStatus("Couldn't find a candidate system, aborting...");
				return new ClientDistance();
			}

			logger.Debug("We have two sets of coords that we can use to find a distance.");
			logger.Debug("Finding from coords: " + sourcecoords.X + " " + sourcecoords.Y + " " + sourcecoords.Z + " to " + targetcoords.X + " " + targetcoords.Y + " " + targetcoords.Z);
			targetcoords = candidates.FirstOrDefault().Coords;
			double deltaX = sourcecoords.X - targetcoords.X;
			double deltaY = sourcecoords.Y - targetcoords.Y;
			double deltaZ = sourcecoords.Z - targetcoords.Z;
			double distance = Math.Sqrt(deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ);
			logger.Debug("Distance should be " + distance);
			cd.Distance = distance;
			return cd;
		}

		/* Attempts to calculate a distance in lightyears between two given systems.
        * This is done using EDSM coordinates.
        * 
        * Transitioned to be exact source system, searches for nearest for target.
        * For the client distance calculation, call GetDistanceToClient instead.
        */

		public async Task<double> CalculateEdsmDistance(string source, string target)
		{
			try {
				EdsmCoords sourcecoords = new EdsmCoords();
				if (source == target)
				{
					return 0; /* Well, it COULD happen? People have been known to do stupid things. */
				}

				if (source == null)
				{
					/* Dafuq? */
					logger.Fatal("Null value passed as source to CalculateEDSMDistance! Falling back to Fuelum as source.");
					source = "Fuelum";
				}

				if (source.Length < 3)
				{
					AppendStatus("Source system name '" + source + "' too short, searching from Fuelum.");
					source = "Fuelum";
				}

				if (target.Length < 3)
				{
					AppendStatus("Target system name '" + target + "' is too short. Can't perform distance search.");
					return -1;
				}

				IEnumerable<EdsmSystem> candidates = await QueryEdsmSystem(source);
				if (candidates == null || !candidates.Any())
				{
					logger.Debug("Unknown source system.");
					return -1;
				}

				candidates = await QueryEdsmSystem(target);
				if (candidates == null || !candidates.Any())
				{
					logger.Debug("EDSM does not know system '" + target + "'. Widening search...");
					candidates = await GetCandidateSystems(target);
				}

				if (candidates.FirstOrDefault().Coords == null)
				{
					logger.Debug("Known system '" + target + "', but no coords. Widening search...");
					candidates = await GetCandidateSystems(target);
				}

				if (candidates == null || !candidates.Any())
				{
					//Still couldn't find something, abort.
					AppendStatus("Couldn't find a candidate system, aborting...");
					return -1;
				}

				logger.Debug("I got " + candidates.Count() + " systems with coordinates. Sorting by lexical match and picking first.");
				var sorted = candidates.OrderBy(s => LexicalOrder(target, s.Name));
				EdsmCoords targetcoords = sorted.FirstOrDefault().Coords;

				if (targetcoords != null)
				{
					logger.Debug("We have two sets of coords that we can use to find a distance.");
					double deltaX = sourcecoords.X - targetcoords.X;
					double deltaY = sourcecoords.Y - targetcoords.Y;
					double deltaZ = sourcecoords.Z - targetcoords.Z;
					double distance = Math.Sqrt(deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ);
					logger.Debug("Distance should be " + distance);
					return distance;
				}

				AppendStatus("EDSM failed to find coords for system '" + target + "'.");
				return -1;
			}
			catch(Exception ex)
			{
				logger.Fatal("Exception in CalculateEdsmDistance: " + ex.Message);
				tc.TrackException(ex);
				return -1;
			}
		}

		#endregion

		/* Used by CalculatedEDSMDistance to sort candidate lists by closest lexical match.
		*/
		private int LexicalOrder(string query, string name)
		{
			return name == query ? -1 : (name.Contains(query) ? 0 : 1);
		}

		private void MenuItem_Click_1(object sender, RoutedEventArgs e)
		{
			//open the dispatch interface
			DispatchInterface.DispatchMain dlg = new DispatchInterface.DispatchMain();
			dlg.Show();
		}

		private void OverlayMenu_Click(object sender, RoutedEventArgs e)
		{
			if (overlay == null)
			{
				overlay = new Overlay();
				overlay.SetCurrentClient(MyClient);
				overlay.Show();
				IEnumerable<Monitor> monitors = Monitor.AllMonitors;
				if (Settings.Default.OverlayMonitor != "")
				{
					logger.Debug("Overlaymonitor is" + Settings.Default.OverlayMonitor);
					foreach (Monitor mymonitor in monitors)
					{
						if (mymonitor.Name == Settings.Default.OverlayMonitor)
						{
							overlay.Left = mymonitor.Bounds.Right - overlay.Width;
							overlay.Top = mymonitor.Bounds.Top;
							overlay.Topmost = true;
							logger.Debug("Overlay coordinates set to " + overlay.Left + " x " + overlay.Top);
							HotKeyHost hotKeyHost = new HotKeyHost((HwndSource)PresentationSource.FromVisual(Application.Current.MainWindow));
							hotKeyHost.AddHotKey(new CustomHotKey("ToggleOverlay", Key.O, ModifierKeys.Control | ModifierKeys.Alt, true));
							hotKeyHost.AddHotKey(new CustomHotKey("CopyClientSystemname", Key.C, ModifierKeys.Control | ModifierKeys.Alt, true));
							hotKeyHost.HotKeyPressed += HandleHotkeyPress;

						}
					}
				}
				else
				{
					foreach (Monitor mymonitor in monitors)
					{
						logger.Debug("Monitor ID: " + mymonitor.Name);
						if (mymonitor.IsPrimary)
						{
							overlay.Left = mymonitor.Bounds.Right - overlay.Width;
							overlay.Top = mymonitor.Bounds.Top;
						}
					}
					overlay.Topmost = true;
					HotKeyHost hotKeyHost = new HotKeyHost((HwndSource)PresentationSource.FromVisual(Application.Current.MainWindow));
					hotKeyHost.AddHotKey(new CustomHotKey("ToggleOverlay", Key.O, ModifierKeys.Control | ModifierKeys.Alt, true));
					hotKeyHost.AddHotKey(new CustomHotKey("CopyClientSystemname", Key.C, ModifierKeys.Control | ModifierKeys.Alt, true));
					hotKeyHost.HotKeyPressed += HandleHotkeyPress;
				}
			}
			else {
				overlay.Close();
			}
		}

		private void HandleHotkeyPress(object sender, HotKeyEventArgs e)
		{
			logger.Debug("Hotkey pressed: " + Name + e.HotKey.Key);
			if (e.HotKey.Key == Key.O)
			{
				overlay.Visibility = overlay.Visibility == Visibility.Hidden ? Visibility.Visible : Visibility.Hidden;
			}

			if (e.HotKey.Key == Key.C)
			{
				if (myClient.ClientSystem != null)
				{
					Clipboard.SetText(myClient.ClientSystem);
					AppendStatus("Client system copied to clipboard.");
				}
				else
				{
					AppendStatus("No active rescue, copy to clipboard aborted.");
				}
			}
		}

		// ReSharper disable once UnusedMember.Local TODO ??
		[SuppressMessage("ReSharper", "UnusedParameter.Local")]
		private void App_Deactivated(object sender, EventArgs e)
		{
			if (overlay != null)
			{
				overlay.Topmost = true;
			}
		}

		protected virtual void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChangedEventHandler onPropertyChanged = PropertyChanged;
			onPropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		/// 
		/// Rat-Button click handlers
		/// TODO review api messages
		/// 
		private void frButton_Click(object sender, RoutedEventArgs e)
		{
			RatState ratState = GetRatStateForButton(sender, FrButton, FrButton_Copy, FrButton_Copy1);
			TPAMessage frmsg = new TPAMessage {data = new Dictionary<string, string>()};
			if (MyClient?.Rescue != null)
			{
				frmsg.action = "FriendRequest:update";
				frmsg.data.Add("RatID", MyPlayer.RatId.FirstOrDefault());
				frmsg.data.Add("RescueID", MyClient.Rescue.id);
			}

			switch (ratState.FriendRequest)
			{
				case RequestState.NotRecieved:
					ratState.FriendRequest = RequestState.Recieved;
					break;
				case RequestState.Recieved:
					ratState.FriendRequest = RequestState.Accepted;
					AppendStatus("Sending Friend Request acknowledgement.");
					frmsg.data.Add("FriendRequest", "true");
					break;
				case RequestState.Accepted:
					AppendStatus("Cancelling FR status.");
					ratState.FriendRequest = RequestState.NotRecieved;
					frmsg.data.Add("FriendRequest", "false");
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			if (frmsg.action != null && ratState.FriendRequest != RequestState.Recieved)
			{
				apworker.SendTpaMessage(frmsg);
			}
		}

		private void wrButton_Click(object sender, RoutedEventArgs e)
		{
			RatState ratState = GetRatStateForButton(sender, WrButton, WrButton_Copy, WrButton_Copy1);
			TPAMessage frmsg = new TPAMessage {data = new Dictionary<string, string>()};
			if (MyClient?.Rescue != null)
			{
				frmsg.action = "WingRequest:update";
				frmsg.data.Add("RatID", MyPlayer.RatId.FirstOrDefault());
				frmsg.data.Add("RescueID", MyClient.Rescue.id);
			}

			switch (ratState.WingRequest)
			{

				case RequestState.NotRecieved:
					ratState.WingRequest = RequestState.Recieved;
					break;
				case RequestState.Recieved:
					AppendStatus("Sending Wing Request acknowledgement.");
					frmsg.data.Add("WingRequest", "true");
					ratState.WingRequest = RequestState.Accepted;
					break;
				case RequestState.Accepted:
					ratState.WingRequest = RequestState.NotRecieved;
					AppendStatus("Cancelled WR status.");
					frmsg.data.Add("WingRequest", "false");
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
			if (frmsg.action != null && ratState.WingRequest != RequestState.Recieved)
			{
				apworker.SendTpaMessage(frmsg);
			}
		}

		private void sysButton_Click(object sender, RoutedEventArgs e)
		{
			RatState ratState = GetRatStateForButton(sender, SysButton, SysButton_Copy, SysButton_Copy1);
			TPAMessage frmsg = new TPAMessage {data = new Dictionary<string, string>()};
			if (MyClient?.Rescue != null)
			{
				frmsg.action = "SysArrived:update";
				frmsg.data.Add("RatID", MyPlayer.RatId.FirstOrDefault());
				frmsg.data.Add("RescueID", MyClient.Rescue.id);
			}

			if (ratState.InSystem==false)
			{
				AppendStatus("Sending System acknowledgement.");
				frmsg.data.Add("ArrivedSystem", "true");
			}
			else
			{
				AppendStatus("Cancelling System status.");
				frmsg.data.Add("ArrivedSystem", "false");
			}

			if (frmsg.action != null)
			{
				apworker.SendTpaMessage(frmsg);
			}

			ratState.InSystem = !ratState.InSystem;
		}

		private void bcnButton_Click(object sender, RoutedEventArgs e)
		{
			RatState ratState = GetRatStateForButton(sender, BcnButton, BcnButton_Copy, BcnButton_Copy1);
			TPAMessage frmsg = new TPAMessage {data = new Dictionary<string, string>()};
			if (MyClient?.Rescue != null)
			{
				frmsg.action = "BeaconSpotted:update";
				frmsg.data.Add("RatID", MyPlayer.RatId.FirstOrDefault());
				frmsg.data.Add("RescueID", MyClient.Rescue.id);
			}

			if (ratState.Beacon==false)
			{
				AppendStatus("Sending Beacon acknowledgement.");
				frmsg.data.Add("BeaconSpotted", "true");
			}
			else
			{
				AppendStatus("Cancelling Beacon status.");
				frmsg.data.Add("BeaconSpotted", "false");
			}

			if (frmsg.action != null && ratState.FriendRequest != RequestState.Recieved)
			{
				apworker.SendTpaMessage(frmsg);
			}

			ratState.Beacon = !ratState.Beacon;
		}

		private void instButton_Click(object sender, RoutedEventArgs e)
		{
			RatState ratState = GetRatStateForButton(sender, InstButton, InstButton_Copy, InstButton_Copy1);
			TPAMessage frmsg = new TPAMessage {data = new Dictionary<string, string>()};
			if (MyClient?.Rescue != null)
			{
				frmsg.action = "InstanceSuccessful:update";
				frmsg.data.Add("RatID", MyPlayer.RatId.FirstOrDefault());
				frmsg.data.Add("RescueID", MyClient.Rescue.id);
			}

			if (ratState.InInstance==false)
			{
				AppendStatus("Sending Good Instance message.");
				frmsg.data.Add("InstanceSuccessful", "true");
			}
			else
			{
				AppendStatus("Cancelling Good instance message.");
				frmsg.data.Add("InstanceSuccessful", "false");
			}

			if (frmsg.action != null && ratState.FriendRequest != RequestState.Recieved)
			{
				apworker.SendTpaMessage(frmsg);
			}

			ratState.InInstance = !ratState.InInstance;
		}

		private RatState GetRatStateForButton(object sender, Button selfButton, Button rat2Button, Button rat3Button)
		{
			RatState ratState;
			if (Equals(sender, selfButton))
			{
				ratState = MyClient.Self;
			}
			else if (Equals(sender, rat2Button))
			{
				ratState = MyClient.Rat2;
			}
			else if (Equals(sender, rat3Button))
			{
				ratState = MyClient.Rat3;
			}
			else
			{
				ratState = MyClient.Self;
			}

			return ratState;
		}


		private void fueledButton_Click(object sender, RoutedEventArgs e)
		{
			TPAMessage fuelmsg = new TPAMessage {action = "Fueled:update"};
			fuelmsg.data.Add("RatID", MyPlayer.RatId.FirstOrDefault());
			fuelmsg.data.Add("RescueID", MyClient.Rescue.id);

			if (Equals(FueledButton.Background, Brushes.Red))
			{
				AppendStatus("Reporting fueled status, requesting paperwork link...");
				FueledButton.Background = Brushes.Green;
				fuelmsg.data.Add("Fueled", "true");
				/* image.Source = new BitmapImage(RatTracker_WPF.Properties.Resources.yellow_light); */
			}
			else
			{
				AppendStatus("Fueled status now negative.");
				FueledButton.Background = Brushes.Red;
				fuelmsg.data.Add("Fueled", "false");

			}

			apworker.SendTpaMessage(fuelmsg);
		}

		private void Runtests_button_click(object sender, RoutedEventArgs e)
		{
			//TriggerSystemChange("Lave");
			//TriggerSystemChange("Blaa Hypai AI-I b26-1");
			//DateTime testdate = DateTime.Now;
			/*            myTravelLog.Add(new TravelLog{ system=new EDSMSystem(){ name = "Sol" }, lastvisited=testdate});
                        myTravelLog.Add(new TravelLog { system = new EDSMSystem() { name = "Fuelum" }, lastvisited = testdate});
                        myTravelLog.Add(new TravelLog { system = new EDSMSystem() { name= "Leesti" }, lastvisited = testdate}); */
			//AppendStatus("Travellog now contains " + myTravelLog.Count() + " systems. Timestamp of first is " + myTravelLog.First().lastvisited +" name "+myTravelLog.First().system.name);
			//CalculateEDSMDistance("Sol", SystemName.Text);
			OverlayMessage mymessage = new OverlayMessage
			{
				Line1Header = "Nearest station:",
				Line1Content = "Wollheim Vision, Fuelum (0LY)",
				Line2Header = "Pad size:",
				Line2Content = "Large",
				Line3Header = "Capabilities:",
				Line3Content = "Refuel, Rearm, Repair"
			};

			overlay?.Queue_Message(mymessage, 30);
			/*
            EDDBData edworker = new EDDBData();
            string status = await edworker.UpdateEDDBData();
            AppendStatus("EDDB: " + status);

            EDDBSystem eddbSystem = edworker.systems.First(s => s.name == "Fuelum");
            var station = edworker.GetClosestStation(fuelumCoords);
            AppendStatus("Closest system to 'Fuelum' is '" + eddbSystem.name +
                        "', closest station to star with known coordinates (should be 'Wollheim Vision') is '" + station.name + "'.");
            MyPlayer.CurrentSystem = "Fuelum";
            MyPlayer.JumpRange = float.Parse("31.24");
            */
			TPAMessage testmessage = new TPAMessage
			{
				action = "WingRequest:update",
				data = new Dictionary<string, string>
				{
					{"WingRequest", "true"},
					{"RescueID", "abc1234567890test"},
					{"RatID", "bcd1234567890test"}
				}
			};
			apworker.SendTpaMessage(testmessage);
			IDictionary<string, string> logindata = new Dictionary<string, string>();
			logindata.Add(new KeyValuePair<string, string>("open", "true"));
			//logindata.Add(new KeyValuePair<string, string>("password", "password"));
			apworker.SendWs("rescues:read", logindata);
			//InitRescueGrid(); // We do this from post-API initialization now.
			if (MyPlayer.RatId != null)
			{
				AppendStatus("Known RatIDs for self:");
				foreach (string id in MyPlayer.RatId)
				{
					AppendStatus(id);
				}
			}
			/* Start Oauth tests 
			SentinelClientSettings oauthsettings = new SentinelClientSettings(new Uri("http://orthanc.localecho.net:7070/"), "5706205e361a6bef133f7183", "69d425a37f31b04499f8dcece6f2a5c782dc2f0b8b234975","RatTracker://home",new TimeSpan(300000));
			SentinelOAuthClient oauthclient = new SentinelOAuthClient(oauthsettings);
			AccessTokenResponse res=await oauthclient.Authenticate();
			UserAgentClient o2c = new UserAgentClient()

			AppendStatus("Got token: "+res.AccessToken+" + "+res.IdToken+", type "+res.TokenType);
			*/
		}


	/*	private InMemoryTokenManager GetAccessTokenFromOwnAuthSrv()
		{
			var server = new AuthorizationServerDescription();
			server.TokenEndpoint = new Uri("http://orthanc.localecho.net:7070/oauth2/token");
			server.ProtocolVersion = DotNetOpenAuth.OAuth2.ProtocolVersion.V20;

			var client = new UserAgentClient(server, clientIdentifier: "RatTracker");
			client.ClientCredentialApplicator = ClientCredentialApplicator.PostParameter("data!");
			var token = client.ExchangeUserCredentialForToken("kenneaal@gmail.com", "794aayp", new[] { "rattracker://main" });
			return token;
		} */

		// ReSharper disable once UnusedParameter.Global TODO ??
		public void CompleteRescueUpdate(string json)
		{
			logger.Debug("CompleteRescueUpdate was called.");
		}

		private async void button1_Click(object sender, RoutedEventArgs e)
		{
			logger.Debug("Begin TPA Jump Call...");
			myrescue = (Datum) RescueGrid.SelectedItem;
			if (myrescue == null)
			{
				AppendStatus("Null myrescue! Failing.");
				return;
			}
			if (myrescue != null && myrescue.id == null)
			{
				logger.Debug("Rescue ID is null!");
				return;
			}
			if (myrescue.Client == null)
			{
				AppendStatus("Null client.");
				return;
			}

			if (myrescue.System == null)
			{
				AppendStatus("Null system.");
				return;
			}
			logger.Debug("Null tests completed");
			AppendStatus("Tracking rescue. System: " + myrescue.System + " Client: " + myrescue.Client);
			MyClient = new ClientInfo
			{
				ClientName = myrescue.Client,
				Rescue = myrescue,
				ClientSystem = myrescue.System
			};
			logger.Debug("Client info loaded:"+MyClient.ClientName+" in "+MyClient.ClientSystem);
			overlay?.SetCurrentClient(MyClient);
			//ClientDistance distance = await GetDistanceToClient(MyClient.ClientSystem);
			ClientDistance distance = new ClientDistance {Distance = 500};
			AppendStatus("Sending jumps to IRC...");
			logger.Debug("Constructing TPA message...");
			var jumpmessage = new TPAMessage();
			logger.Debug("Setting action.");
			jumpmessage.action = "CallJumps:update";
			jumpmessage.applicationId = "0xDEADBEEF";
			logger.Debug("Set appID");
			logger.Debug("Constructing TPA for "+myrescue.id+" with "+myplayer.RatId.First());
			jumpmessage.data = new Dictionary<string, string>();
			jumpmessage.data["CallJumps"] = "5";
			logger.Debug("Set jumps");
			jumpmessage.data["RescueID"] = myrescue.id;
			logger.Debug("Set rescue ID");
			jumpmessage.data["RatID"] = myplayer.RatId.FirstOrDefault();
			logger.Debug("Set RatID");
			/*{
					//{"CallJumps", Math.Ceiling(distance.Distance/myplayer.JumpRange).ToString()},
					{"CallJumps","5" },
					{"RescueID", myrescue.id},
					{"RatID", myplayer.RatId.FirstOrDefault()},
					{"Lightyears", distance.Distance.ToString()},
					{"SourceCertainty", "Close"},
					{"DestinationCertainty", "Far"}
			};*/
			logger.Debug("Sending TPA message");
			apworker.SendTpaMessage(jumpmessage);
		}


		private async void Button2_Click(object sender, RoutedEventArgs e)
		{
			logger.Debug("Querying EDDB for closest station to " + myplayer.CurrentSystem);
			IEnumerable<EdsmSystem> mysys = await QueryEdsmSystem(myplayer.CurrentSystem);
			if (mysys != null && mysys.Any())
			{
				logger.Debug("Got a mysys with " + mysys.Count() + " elements");
				var station = eddbworker.GetClosestStation(mysys.First().Coords);
				EddbSystem system = eddbworker.GetSystemById(station.system_id);
				AppendStatus("Closest populated system to '"+myplayer.CurrentSystem+"' is '" + system.name+
							"', closest station to star with known coordinates is '" + station.name + "'.");
				double distance = await CalculateEdsmDistance(myplayer.CurrentSystem, mysys.First().Name);
				OverlayMessage mymessage = new OverlayMessage
				{
					Line1Header = "Nearest station:",
					Line1Content = station.name + ", " + system.name + " (" + Math.Round(distance, 2) + "LY)",
					Line2Header = "Pad size:",
					Line2Content = station.max_landing_pad_size,
					Line3Header = "Capabilities:",
					Line3Content = new StringBuilder()
						.AppendIf(station.has_refuel, "Refuel,")
						.AppendIf(station.has_repair, "Repair,")
						.AppendIf(station.has_outfitting, "Outfit").ToString()
				};

				overlay?.Queue_Message(mymessage, 30);
				return;
			}

			AppendStatus("Unable to find a candidate system for location " + MyClient.ClientSystem);
		}

		private void ErrorReportClick(object sender, RoutedEventArgs e)
		{
			ErrorReporter errwnd= new ErrorReporter();
			bool? result = errwnd.ShowDialog();
			if (result == true)
			{
				AppendStatus("Application bug report sent.");
			}
		}
	}
}