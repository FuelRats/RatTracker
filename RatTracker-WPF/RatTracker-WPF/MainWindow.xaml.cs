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
using System.Globalization;
using System.Net.Http.Headers;
using Microsoft.ApplicationInsights.DataContracts;


namespace RatTracker_WPF
{
	/// <summary>
	///     Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : INotifyPropertyChanged
	{
		#region GlobalVars
		private const bool TestingMode = true; // Use the TestingMode bool to point RT to test API endpoints and non-live queries. Set to false when deployed.
		private const string Unknown = "unknown";
		/* These can not be static readonly. They may be changed by the UI XML pulled from E:D. */
		public static readonly Brush RatStatusColourPositive = Brushes.LightGreen;
		public static readonly Brush RatStatusColourPending = Brushes.Orange;
		public static readonly Brush RatStatusColourNegative = Brushes.Red;
		private const string EdsmUrl = "http://www.edsm.net/api-v1/";
		private bool _oauthProcessing;

		private static readonly ILog Logger = LogManager.GetLogger(System.Reflection.Assembly.GetCallingAssembly().GetName().Name);
		private static readonly EdsmCoords FuelumCoords = new EdsmCoords() {X = 52, Y = -52.65625, Z = 49.8125}; // Static coords to Fuelum, saves a EDSM query

		//private readonly SpVoice voice = new SpVoice();
		private ApiWorker _apworker; // Provides connection to the API
		private string _assignedRats; // String representation of assigned rats to current case, bound to the UI 
		public ConnectionInfo Conninfo = new ConnectionInfo(); // The rat's connection information
		private double _distanceToClient; // Bound to UI element
		private string _distanceToClientString; // Bound to UI element
		private long _fileOffset; // Current offset in NetLog file
		private long _fileSize; // Size of current netlog file
		private string _jumpsToClient; // Bound to UI element
        private string _xmlparselist; // Buffer for XML since it now arrives in chunks.

		// ReSharper disable once UnusedMember.Local TODO ??
		private string _logDirectory = Settings.Default.NetLogPath; // TODO: Remove this assignment and pull live from Settings, have the logfile watcher reaquire file if settings are changed.
		private FileInfo _logFile; // Pointed to the live netLog file.
		private ClientInfo _myClient = new ClientInfo(); // Semi-redundant UI bound data model. Somewhat duplicates myrescue, needs revision.
		private PlayerInfo _myplayer = new PlayerInfo(); // Playerinfo, bound to various UI elements
		private Datum _myrescue; // TODO: See myClient - must be refactored.
		private ICollection<TravelLog> _myTravelLog; // Log of recently visited systems.
		private Overlay _overlay; // Pointer to UI overlay
		private RootObject _rescues; // Current rescues. Source for items in rescues datagrid
		private string _scState; // Supercruise state.
		public bool StopNetLog; // Used to terminate netlog reader thread.
		private readonly TelemetryClient _tc = new TelemetryClient(); 
		private Thread _threadLogWatcher; // Holds logwatcher thread.
		private FileSystemWatcher _watcher; // FSW for the Netlog directory.
		private EddbData _eddbworker;

		public EddbData Eddbworker
		{
			get
			{
				return _eddbworker;
			}
			set
			{
				_eddbworker = value;
				NotifyPropertyChanged();
			}
		}

		private FireBird _fbworker;

        public FireBird FbWorker
        {
            get
            {
                return _fbworker;
            }
            set
            {
                _fbworker = value;
                NotifyPropertyChanged();
            }
        }
		// TODO
#pragma warning disable 649
		private string _oAuthCode;
#pragma warning restore 649
		#endregion
		
		public MainWindow()
		{
			Logger.Info("---Starting RatTracker---");
			try
			{
				foreach (string arg in Environment.GetCommandLineArgs())
				{
					Logger.Debug("Arg: " + arg);
					if (arg.Contains("rattracker"))
					{
						Logger.Debug("RatTracker was invoked for OAuth code authentication.");
						_oauthProcessing = true;
						string reMatchToken = ".*?code=(.*)?&state=preinit";
						Match match = Regex.Match(arg, reMatchToken, RegexOptions.IgnoreCase);
						if (match.Success)
						{
							Logger.Debug("Calling OAuth authentication...");
							OAuth_Authorize(match.Groups[1].ToString());
						}
						else
						{
							Logger.Debug("Failed to match token?!!");
						}
					}
				}
			}
			catch(Exception ex)
			{
				Logger.Debug("Exception in token parse: "+ex.Message);
				return;
			}

			_tc.Context.Session.Id = Guid.NewGuid().ToString();
			_tc.Context.Device.OperatingSystem = Environment.OSVersion.ToString();
			_tc.Context.User.Id = Environment.UserName;
			_tc.Context.Component.Version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
			_tc.TrackPageView("MainWindow");
			InitializeComponent();
			Loaded += Window_Loaded;
			Logger.Debug("Parsing AppConfig...");
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
				Logger.Debug("A code was passed to connectAPI, attempting token exchange.");
				using (HttpClient hc = new HttpClient())
				{
					Auth myauth = new Auth
					{
						code = code,
						grant_type = "authorization_code",
						redirect_url = "rattracker://auth"
					};
					string json = JsonConvert.SerializeObject(myauth);
					Logger.Debug("Passing auth JSON: " + json);
					var content = new UriBuilder(Path.Combine(Settings.Default.APIURL + "oauth2/token"))
					{
						Port = Settings.Default.APIPort
					};
					Logger.Debug("Passing code: " + code);
					hc.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes(
						"4eace3e0-6564-4d41-87d4-bae2e2d2f6df:0f9b77d273f97cdd2341af31c3cbb373be65229192c4c98d")));
					Logger.Debug("Built query string:" + content);
					var formenc = new FormUrlEncodedContent(new[]
					{
						new KeyValuePair<string,string>("code",code),
						new KeyValuePair<string,string>("grant_type","authorization_code"),
						new KeyValuePair<string,string>("redirect_uri","rattracker://auth")
					});
					HttpResponseMessage response = await hc.PostAsync(content.ToString(), formenc).ConfigureAwait(false);
					Logger.Debug("AsyncPost sent with ConfigureAwait false.");
					HttpContent mycontent = response.Content;
					string data = mycontent.ReadAsStringAsync().Result;
					Logger.Debug("Got data: " + data);
					if (data.Contains("access_token"))
					{
						Logger.Debug("In access token true.");
						var token = JsonConvert.DeserializeObject<TokenResponse>(data);
						Logger.Debug("Access token received: " + token.access_token);
						Settings.Default.OAuthToken = token.access_token;
						Settings.Default.Save();
						Logger.Debug("Access token saved.");
						AppendStatus("OAuth authentication transaction successful, bearer token stored. Initializing RatTracker.");
						_oauthProcessing = false;
						DoInitialize();
					}
					else
					{
						Logger.Debug("No access token in data response!");
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
			BackgroundWorker fbWorker = new BackgroundWorker();

			if (_oauthProcessing == false)
			{
				apiWorker.DoWork += (s, args) =>
				{
					Logger.Debug("Initialize API...");
					InitApi(false);
                };
				eddbWorker.DoWork += async delegate
				{
					Logger.Debug("Initialize EDDB...");
					await InitEddb(false);
				};
				piWorker.DoWork += async delegate
				{
					Logger.Debug("Initialize player data...");
					await InitPlayer();
				};
				fbWorker.DoWork += async delegate
				{
					Logger.Debug("Initialize Firebird SQL...");
					await InitFirebird();

				};
				fbWorker.RunWorkerAsync();
				eddbWorker.RunWorkerAsync();
				apiWorker.RunWorkerAsync();
				piWorker.RunWorkerAsync();
				
			}
			else
			{
				Logger.Debug("Skipping initialization, OAuth is processing.");
			}
		}

		public async Task InitFirebird()
		{
			if (Eddbworker == null)
			{
				AppendStatus("Firebird is waiting for EDDB to finish initialization.");
				Thread.Sleep(5000);
				await InitFirebird();
				return;
			}
			AppendStatus("EDDB has loaded, initializing FBWorker.");
			FbWorker = new FireBird();
			FbWorker.InitDB();
			// Wait a bit for Firebird to load up...
			Thread.Sleep(5000);
			Eddbworker.Setworker(ref _fbworker);
            FbWorker.SetEDDB(ref _eddbworker);
            InitRescueGrid();
			return;
		}
		public async void Reinitialize()
		{
			Logger.Debug("Reinitializing application...");
			if (ParseEdAppConfig())
			{
				CheckLogDirectory();
			}
			else
			{
				AppendStatus("RatTracker does not have a valid path to your E:D directory. This will probably break RT! Please check your settings.");
			}

			InitApi(true); 
			await InitEddb(true);
			await InitPlayer();
			Logger.Debug("Reinitialization complete.");
		}

		#region PropertyNotifiers
		public ObservableCollection<Datum> ItemsSource { get; } = new ObservableCollection<Datum>();
		public static ConcurrentDictionary<string, Rat> Rats { get; } = new ConcurrentDictionary<string, Rat>();
		public ConnectionInfo ConnInfo
		{
			get { return Conninfo; }
			set
			{
				Conninfo = value;
				NotifyPropertyChanged();
			}
		}

		public ClientInfo MyClient
		{
			get { return _myClient; }
			set
			{
				_myClient = value;
				NotifyPropertyChanged();
			}
		}

		public string JumpsToClient
		{
			get { return string.IsNullOrWhiteSpace(_jumpsToClient) ? Unknown : "~" + _jumpsToClient; }
			set
			{
				_jumpsToClient = value;
				NotifyPropertyChanged();
			}
		}

		public double DistanceToClient
		{
			get { return _distanceToClient; }
			set
			{
				_distanceToClient = value;
				NotifyPropertyChanged();
				DistanceToClientString = string.Empty;
			}
		}

		public string DistanceToClientString
		{
			get
			{
				return DistanceToClient >= 0
					? DistanceToClient.ToString(CultureInfo.InvariantCulture)
					: !string.IsNullOrWhiteSpace(_distanceToClientString) ? _distanceToClientString : Unknown;
			}
			set
			{
				_distanceToClientString = value;
				NotifyPropertyChanged();
			}
		}

		public string AssignedRats
		{
			get { return _assignedRats; }
			set
			{
				_assignedRats = value;
				NotifyPropertyChanged();
			}
		}

		public PlayerInfo MyPlayer
		{
			get { return _myplayer; }
			set
			{
				_myplayer = value;
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
				Logger.Info("Initializing API connection...");
				if(_apworker==null)
					_apworker = new ApiWorker();
				_apworker.InitWs();
				_apworker.OpenWs();
				if (!reinitialize)
				{
					_apworker.Ws.MessageReceived += websocketClient_MessageReceieved;
					_apworker.Ws.Opened += websocketClient_Opened;
				}

				if (_oAuthCode != null)
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
				Logger.Fatal("Exception in InitAPI: " + ex.Message);
			}
		}

		private void websocketClient_Opened(object sender, EventArgs e)
		{
			InitRescueGrid();
		}

		public Task InitPlayer()
		{
			_myplayer.JumpRange = Settings.Default.JumpRange > 0 ? Settings.Default.JumpRange : 30;
			_myplayer.CurrentSystem = "Fuelum";
			return Task.FromResult(true);
		}

		private async Task InitEddb(bool reinit)
		{
			if (reinit == true)
				return;
			AppendStatus("Initializing EDDB.");
			if (Eddbworker == null)
			{
				Eddbworker = new EddbData(ref _fbworker);
			}
			Thread.Sleep(3000);

			string status = await Eddbworker.UpdateEddbData(false);
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
				Logger.Debug("Meta data from API: " + meta);
				switch ((string)meta.action)
				{
					case "welcome":
						Logger.Info("API MOTD: " + data.data);
						break;
					case "assignment":
						Logger.Debug("Got a new assignment datafield: " + data.data);
						break;
					case "rescues:read":
						if (realdata == null)
						{
							Logger.Error("Null list of rescues received from rescues:read!");
							break;
						}
						Logger.Debug("Got a list of rescues: " + realdata);
						_rescues = JsonConvert.DeserializeObject<RootObject>(e.Message);
						await disp.BeginInvoke(DispatcherPriority.Normal, (Action)(() => ItemsSource.Clear()));
						await disp.BeginInvoke(DispatcherPriority.Normal, (Action)(() => _rescues.Data.ForEach(datum => ItemsSource.Add(datum))));
						//await disp.BeginInvoke(DispatcherPriority.Normal,
						//	(Action)(() => RescueGrid.ItemsSource = rescues.Data));
						await GetMissingRats(_rescues);
						break;
					case "message:send":
						/* We got a message broadcast on our channel. */
						AppendStatus("Test 3PA data from WS receieved: " + realdata);
						break;
					case "users:read":
						Logger.Info("Parsing login information..." + meta.count + " elements");
                        if (!realdata)
                        {
                            Logger.Debug("Null realdata during login! Data element: "+data.ToString());
                            AppendStatus("RatTracker failed to get your user data from the API. This makes RatTracker unable to verify your identity for jump calls and messages.");
                            break;
                        }
						//Logger.Debug("Raw: " + realdata[0]);

						AppendStatus("Got user data for " + realdata[0].email);
						MyPlayer.RatId = new List<string>();
						foreach (dynamic cmdrdata in realdata[0].CMDRs)
						{
							AppendStatus("RatID " + cmdrdata + " added to identity list.");
							MyPlayer.RatId.Add(cmdrdata.ToString());
						}
						_myplayer.RatName = await GetRatName(MyPlayer.RatId.FirstOrDefault()); // This will have to be redone when we go WS, as we can't load the variable then.
						break;
					case "rats:read":
						Logger.Info("Received rat identification: " + meta.count + " elements");
						Logger.Debug("Raw: " + realdata[0]);
						break;

					case "rescue:updated":
						Datum updrescue = realdata.ToObject<Datum>();
						if (updrescue == null)
						{
							Logger.Debug("null rescue update object, breaking...");
							break;
						}
						Logger.Debug("Updrescue _ID is " + updrescue.id);
						Datum myRescue = _rescues.Data.FirstOrDefault(r => r.id == updrescue.id);
						if (myRescue == null)
						{
							Logger.Debug("Myrescue is null in updaterescue, reinitialize grid.");
							APIQuery rescuequery = new APIQuery
							{
								action = "rescues:read",
								data = new Dictionary<string, string> {{"open", "true"}}
							};
							_apworker.SendQuery(rescuequery);
							break;
						}
						if (updrescue.Open == false)
						{
							AppendStatus("Rescue closed: " + updrescue.Client);
							Logger.Debug("Rescue closed: " + updrescue.Client);
							await disp.BeginInvoke(DispatcherPriority.Normal, (Action)(() => ItemsSource.Remove(myRescue)));
						}
						else
						{
							_rescues.Data[_rescues.Data.IndexOf(myRescue)] = updrescue;
							await disp.BeginInvoke(DispatcherPriority.Normal, (Action)(() => ItemsSource[ItemsSource.IndexOf(myRescue)] = updrescue));
							Logger.Debug("Rescue updated: " + updrescue.Client);
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
						if (_overlay != null)
						{
							await disp.BeginInvoke(DispatcherPriority.Normal, (Action)(() => _overlay.Queue_Message(nr, 30)));
						}

						await disp.BeginInvoke(DispatcherPriority.Normal, (Action)(() => ItemsSource.Add(newrescue))); 
						break;
					case "stream:subscribe":
						Logger.Debug("Subscribed to 3PA stream " + data.ToString());
						break;
					case "stream:broadcast":
						Logger.Debug("3PA broadcast message received:" + data.ToString());
						break;
                    case "authorization":
                        Logger.Debug("Authorization callback: " + realdata.ToString());
                        AppendStatus("Got user data for " + realdata.email);
                        MyPlayer.RatId = new List<string>();
                        foreach (dynamic cmdrdata in realdata.rats)
                        {
                            AppendStatus("RatID " + cmdrdata.id + " added to identity list.");
                            MyPlayer.RatId.Add(cmdrdata.id.ToString());
                        }
                        _myplayer.RatName = await GetRatName(MyPlayer.RatId.FirstOrDefault()); // This will have to be redone when we go WS, as we can't load the variable then.
                        break;

                        break;
					default:
						Logger.Info("Unknown API action field: " + meta.action);
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
				Logger.Fatal("Exception in WSClient_MessageReceived: " + ex.Message);
				_tc.TrackException(ex);
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
			_tc.TrackException(exceptionTelemetry);
		}

		// ReSharper disable once UnusedMember.Local TODO what to do with this?
		// ReSharper disable once UnusedParameter.Local TODO what to do with this?
		private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			TrackFatalException(e.ExceptionObject as Exception);
			_tc.Flush();
		}
		/*
		 * Parses E:D's AppConfig and looks for the configuration variables we need to make RT work.
		 * Offers to change them if not set correctly.
		 */
		public bool ParseEdAppConfig()
		{
			string edProductDir = Settings.Default.EDPath + "\\Products";
			Logger.Debug("Looking for Product dirs in " + Settings.Default.EDPath + "\\Products");
			try {
                if (!Directory.Exists(edProductDir))
                {
                    Logger.Fatal("Couldn't find E:D product directory, looking for Windows 10 installation...");
                    edProductDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Frontier_Developments\\Products"; //Attempt Windows 10 path.
                    Logger.Debug("Looking in " + edProductDir);
                    if (!Directory.Exists(edProductDir))
                    {
                        Logger.Fatal("Couldn't find E:D product directory. Aborting AppConfig parse. You must set the path manually in settings.");
                        return false;
                    }

                    Logger.Debug("Found Windows 10 installation. Setting application paths...");
                    Settings.Default.EDPath = edProductDir;
                    //Settings.Default.NetLogPath = edProductDir + "\\logs";
                    Settings.Default.Save();
                }
                else
                {
                    if (!Directory.Exists(Settings.Default.NetLogPath))
                    {
                        Logger.Fatal("E:D netlog directory is not set to a valid path!");
                        AppendStatus("Your E:D netlog directory is set to an invalid path!");
                    }
                    Logger.Info("Netlog path is set to " + Settings.Default.NetLogPath);
                }
			}
			catch (Exception ex)
			{
				Logger.Fatal("Error during edProductDir check?!" + ex.Message);
				_tc.TrackException(ex);
			}

			foreach (string dir in Directory.GetDirectories(edProductDir))
			{
				if (dir.Contains("COMBAT_TUTORIAL_DEMO"))
				{
					break; // We don't need to do AppConfig work on that.
				}

				Logger.Info("Checking AppConfig in Product directory " + dir);
				try
				{
					Logger.Debug("Loading " + dir + @"\AppConfig.xml");
					XDocument appconf = XDocument.Load(dir + @"\AppConfig.xml");

					XElement xElement = appconf.Element("AppConfig");
					if (xElement != null)
					{
						XElement networknode = xElement.Element("Network");
						if (networknode != null && networknode.Attribute("VerboseLogging") == null)
						{
							// Nothing is set up! This makes testing the attributes difficult, so initialize VerboseLogging at least.
							networknode.SetAttributeValue("VerboseLogging", 0);
							Logger.Info("No VerboseLogging configuration at all. Setting temporarily for testing.");
						}

						if (networknode != null)
						{
							var xAttribute = networknode.Attribute("VerboseLogging");
							if (xAttribute == null ||
							    (xAttribute.Value == "1" && networknode.Attribute("ReportSentLetters") != null &&
							     networknode.Attribute("ReportReceivedLetters") != null)) continue;
						}
						Logger.Error("WARNING: Your Elite:Dangerous AppConfig is not set up correctly to allow RatTracker to work!");
						MessageBoxResult result =
							MessageBox.Show(
								"Your AppConfig in " + dir +
								" is not configured correctly to allow RatTracker to perform its function. Would you like to alter the configuration to enable Verbose Logging? Your old AppConfig will be backed up.",
								"Incorrect AppConfig", MessageBoxButton.YesNo, MessageBoxImage.Exclamation);
						_tc.TrackEvent("AppConfigNotCorrectlySetUp");
						switch (result)
						{
							case MessageBoxResult.Yes:
								File.Copy(dir + @"\AppConfig.xml", dir + @"\AppConfig-BeforeRatTracker.xml", true);

								if (networknode != null)
								{
									networknode.SetAttributeValue("VerboseLogging", "1");
									networknode.SetAttributeValue("ReportSentLetters", 1);
									networknode.SetAttributeValue("ReportReceivedLetters", 1);
								}
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

								Logger.Info("Wrote new configuration to " + dir + @"\AppConfig.xml");
								_tc.TrackEvent("AppConfigAutofixed");
								return true;
							case MessageBoxResult.No:
								Logger.Info("No alterations performed.");
								_tc.TrackEvent("AppConfigDenied");
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
					}

					return true;
				}
				catch (Exception ex)
				{
					Logger.Fatal("Exception in AppConfigReader!", ex);
					_tc.TrackException(ex);
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
            //Logger.Debug("Before xml extract:" + friendsList);
			string xmlData = friendsList.Substring(friendsList.IndexOf("<", StringComparison.Ordinal) + friendsList.Length);
			Logger.Debug("Parsing XML buffer.");
			try
			{
				XDocument xdoc = XDocument.Parse(friendsList);
				Logger.Debug("Successful XML parse.");
				XElement rettest = xdoc.Element("OK");
				if (rettest != null)
				{
					XElement xElement = xdoc.Element("OK");
					if (xElement != null) Logger.Debug("Last friendslist action: " + xElement.Value);
				}

				IEnumerable<XElement> friends = xdoc.Descendants("item");
				foreach (XElement friend in friends)
				{
					/* string preencode = Regex.Replace(friend.Element("name").Value, ".{2}", "\\x$0"); */
					XElement xElement = friend.Element("name");
					if (xElement != null)
					{
						byte[] byteenc = StringToByteArray(xElement.Value);
						//appendStatus("Friend:" + System.Text.Encoding.UTF8.GetString(byteenc));
						count++;
						XElement element = friend.Element("pending");
						if (element != null && element.Value == "1")
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
								_apworker.SendTpaMessage(frmsg);
							}
						}
					}
				}

				/* Check the OK status field, which can contain useful information on successful FRs. */
				foreach (XElement element in xdoc.Descendants())
				{
					if (element.Name != "OK") continue;
					XElement xElement1 = xdoc.Element("data");
					if (xElement1 != null)
					{
						XElement element1 = xElement1.Element("OK");
						if (element1 != null) Logger.Debug("Return code: " + element1.Value);
					}
					var xElement = xdoc.Element("data");
					if (xElement != null)
					{
						var o = xElement.Element("OK");
						if (o != null && (!o.Value.Contains("Invitation accepted"))) continue;
					}
					AppendStatus("Friend request accepted.");
					MyClient.Self.FriendRequest = RequestState.Accepted;
				}

				//AppendStatus("Parsed " + count + " friends in FRXML."); //Spammy!
			}
			catch (XmlException ex)
			{
				Logger.Fatal("XML Parsing exception - Probably netlog overflow. " + ex.Message);
			}
			catch (Exception ex)
			{
				Logger.Fatal("XML Parsing exception:" + ex.Message);
				_tc.TrackException(ex);
			}
		}

		private void ParseWingInvite(string wingInvite)
		{
			Logger.Debug("Raw xmlData: " + wingInvite);
			try
			{
				XDocument xdoc = XDocument.Parse(wingInvite);
				Logger.Debug("Successful XML parse.");
				//voice.Speak("Wing invite detected.");
				IEnumerable<XElement> wing = xdoc.Descendants("commander");
				foreach (XElement wingdata in wing)
				{
					XElement xElement = wingdata.Element("name");
					if (xElement != null)
					{
						byte[] byteenc = StringToByteArray(xElement.Value);
						AppendStatus("Wingmember:" + Encoding.UTF8.GetString(byteenc));
						if (_myrescue != null)
						{
							if (Encoding.UTF8.GetString(byteenc).ToLower() == _myrescue.Client.ToLower())
							{
								AppendStatus("This data matches our current client! Storing information...");
								XElement element = wingdata.Element("id");
								if (element != null) MyClient.ClientId = element.Value;
								AppendStatus("Wingmember IP data:" + xdoc.Element("connectionDetails"));
								string wingIPPattern = "IP4NAT:([0-9.]+):\\d+\\,";
								Match wingMatch = Regex.Match(wingInvite, wingIPPattern, RegexOptions.IgnoreCase);
								if (wingMatch.Success)
								{
									AppendStatus("Successful IP data match: " + wingMatch.Groups[1]);
									MyClient.ClientIp = wingMatch.Groups[1].Value;
								}

								/* If the friend request matches the client name, store his session ID. */
								XElement o = wingdata.Element("commander_id");
								if (o != null) MyClient.ClientId = o.Value;
								XElement xElement1 = wingdata.Element("session_runid");
								if (xElement1 != null) MyClient.SessionId = xElement1.Value;
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
								_apworker.SendTpaMessage(wrmsg);
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				Logger.Fatal("Error in parseWingInvite: " + ex.Message);
				_tc.TrackException(ex);
			}
		}

		private void MainWindow_Closing(object sender, CancelEventArgs e)
		{
			StopNetLog = true;
			_apworker?.DisconnectWs();
			_tc?.Flush();
			Thread.Sleep(1000);
			Application.Current.Shutdown();
		}

		private void CheckLogDirectory()
		{
			Logger.Debug("Checking log directories.");
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
				if (_watcher == null)
				{
					_watcher = new FileSystemWatcher
					{
						Path = Settings.Default.NetLogPath,
						NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName |
										NotifyFilters.DirectoryName | NotifyFilters.Size,
						Filter = "*.log"
					};
					_watcher.Changed += OnChanged;
					_watcher.Created += OnChanged;
					_watcher.Deleted += OnChanged;
					_watcher.Renamed += OnRenamed;
					_watcher.EnableRaisingEvents = true;
				}

				DirectoryInfo tempDir = new DirectoryInfo(Settings.Default.NetLogPath);
				_logFile = (from f in tempDir.GetFiles("netLog*.log") orderby f.LastWriteTime descending select f).First();
				AppendStatus("Started watching file " + _logFile.FullName);
				Logger.Debug("Watching file: " + _logFile.FullName);
				CheckClientConn(_logFile.FullName);
				ReadLogfile(_logFile.FullName);
				_myTravelLog = new Collection<TravelLog>();
			}
			catch (Exception ex)
			{
				Logger.Debug("Exception in CheckLogDirectory! " + ex.Message);
				_tc.TrackException(ex);
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
						if (line != null && line.Contains("Local machine is"))
						{
							Logger.Info("My RunID: " + line.Substring(line.IndexOf("is ", StringComparison.Ordinal)));
							ConnInfo.RunId = line.Substring(line.IndexOf("is ", StringComparison.Ordinal));
						}
						if (line != null && line.Contains("RxRoute"))
						{
							// Yes, this early in the netlog, I figure we can just parse the RxRoute without checking for ID. Don't do this later though.
							const string rxpattern = "IP4NAT:(\\d{1,3}\\.\\d{1,3}\\.\\d{1,3}\\.\\d{1,3})+:(\\d{1,5}),(\\d{1,3}\\.\\d{1,3}\\.\\d{1,3}\\.\\d{1,3})+:(\\d{1,5}),(\\d{1,3}\\.\\d{1,3}\\.\\d{1,3}\\.\\d{1,3})+:(\\d{1,5}),(\\d{1,3}\\.\\d{1,3}\\.\\d{1,3}\\.\\d{1,3})+:(\\d{1,5}),(\\d),(\\d),(\\d),(\\d{1,4})";
							Match match = Regex.Match(line, rxpattern, RegexOptions.IgnoreCase);
							if (match.Success)
							{
								Logger.Info("Route info: WAN:" + match.Groups[1].Value + " port " + match.Groups[2].Value + ", LAN:" +
											match.Groups[3].Value + " port " + match.Groups[4].Value + ", STUN: " + match.Groups[5].Value + ":" +
											match.Groups[6].Value + ", TURN: " + match.Groups[7].Value + ":" + match.Groups[8].Value +
											" MTU: " + match.Groups[12].Value + " NAT type: " + match.Groups[9].Value + " uPnP: " +
											match.Groups[10].Value + " MultiNAT: " + match.Groups[11].Value);
								ConnInfo.WanAddress = match.Groups[1].Value + ":" + match.Groups[2].Value;
								ConnInfo.Mtu = int.Parse(match.Groups[12].Value);
								_tc.TrackMetric("Rat_Detected_MTU", ConnInfo.Mtu);
								ConnInfo.NatType = (NatType) Enum.Parse(typeof (NatType), match.Groups[9].Value);
								if (match.Groups[2].Value == match.Groups[4].Value && match.Groups[10].Value == "0")
								{
									Logger.Debug("Probably using static portmapping, source and destination port matches and uPnP disabled.");
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
						if (line != null && line.Contains("failed to initialise upnp"))
						{
							AppendStatus(
								"CRITICAL: E:D has failed to establish a upnp port mapping, but E:D is configured to use upnp. Disable upnp in netlog if you have a router that can't do UPnP, and forward ports manually.");
						}
						if (line != null && line.Contains("Sync Established"))
						{
							AppendStatus("Sync Established.");
						}

						if (line != null && line.Contains("ConnectToServerActivity:StartRescueServer"))
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
							_tc.TrackMetric("NATBlocked", 1);
							break;
						case NatType.Unknown:
							if (ConnInfo.PortMapped != true)
							{
								AppendStatus(
									"WARNING: E:D is unable to determine the status of your network port. This may be indicative of a condition that may cause instancing problems!");
								ConnTypeLabel.Content = "Unknown";
								_tc.TrackMetric("NATUnknown", 1);
							}
							else
							{
								AppendStatus("Unable to determine NAT type, but you seem to have a statically mapped port forward.");
								_tc.TrackMetric("ManualPortMap", 1);
							}
							break;
						case NatType.Open:
							ConnTypeLabel.Content = "Open";
							_tc.TrackMetric("NATOpen", 1);
							break;
						case NatType.FullCone:
							ConnTypeLabel.Content = "Full cone NAT";
							_tc.TrackMetric("NATFullCone", 1);
							break;
						case NatType.Failed:
							AppendStatus("WARNING: E:D failed to detect your NAT type. This might be problematic for instancing.");
							ConnTypeLabel.Content = "Failed to detect!";
							_tc.TrackMetric("NATFailed", 1);
							break;
						case NatType.SymmetricUdp:
							if (ConnInfo.PortMapped != true)
							{
								AppendStatus(
									"WARNING: Symmetric NAT detected! Although your NAT allows UDP, this may cause SEVERE problems when instancing!");
								ConnTypeLabel.Content = "Symmetric UDP";
								_tc.TrackMetric("NATSymmetricUDP", 1);
							}

							else
							{
								AppendStatus("Symmetric UDP NAT with static port mapping detected.");
								_tc.TrackMetric("ManualPortMap", 1);
							}

							break;
						case NatType.Restricted:
							if (ConnInfo.PortMapped != true)
							{
								AppendStatus("WARNING: Port restricted NAT detected. This may cause instancing problems!");
								ConnTypeLabel.Content = "Port restricted NAT";
								_tc.TrackMetric("NATRestricted", 1);
							}
							else
							{
								AppendStatus("Port restricted NAT with static port mapping detected.");
								_tc.TrackMetric("ManualPortMap", 1);
							}
							break;
						case NatType.Symmetric:
							if (ConnInfo.PortMapped != true)
							{
								AppendStatus(
									"WARNING: Symmetric NAT detected. This is usually VERY BAD for instancing. If you do not have a manual port mapping set up, you should consider changing your network configuration.");
								_tc.TrackMetric("NATSymmetric", 1);
							}
							else
							{
								AppendStatus("Symmetric NAT with static port mapping detected.");
								_tc.TrackMetric("ManualPortMap", 1);
							}
							break;
					}
					if (stopSnooping == false)
					{
						AppendStatus(
							"Client connectivity detection complete.");
					}
					_tc.Flush();
				}
			}
			catch (Exception ex)
			{
				Logger.Fatal("Exception in checkClientConn:" + ex.Message);
				_tc.TrackException(ex);
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
					if (_fileOffset == 0L)
					{
						Logger.Debug("First peek...");
						if (sr.BaseStream.Length > 5000)
						{
							sr.BaseStream.Seek(-5000, SeekOrigin.End);
							/* First peek into the file, rewind a bit and scan from there. */
						}
					}
					else
					{
						sr.BaseStream.Seek(_fileOffset, SeekOrigin.Begin);
					}

					while (sr.Peek() != -1)
					{
						string line = sr.ReadLine();
						if (string.IsNullOrEmpty(line))
						{
							Logger.Error("Empty line while attempting to read a log line!");
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
				Logger.Fatal("Exception in readLogFile: ", ex);
				Logger.Debug("StackTrace: "+ ex.StackTrace);
				_tc.TrackException(ex);
			}
		}

		private void ParseLine(string line)
		{
			try
			{
				if (string.IsNullOrEmpty(line))
				{
					Logger.Error("ParseLine was passed a null or empty line. This should not happen!");
					return;
				}
				// string reMatchSystem = ".*?(System:).*?\\(((?:[^)]+)).*?\\)"; // Pre-1.6/2.1 style
				const string reMatchSystem = ".*?(System:)\"(.*)?\".*?\\(((?:[^)]+)).*?\\)";
				Match match = Regex.Match(line, reMatchSystem, RegexOptions.IgnoreCase);
				if (match.Success)
				{
					if (match.Groups[2].Value == _myplayer.CurrentSystem)
						return;
					TriggerSystemChange(match.Groups[2].Value);
				}
				const string reMatchPlayer = "\\{.+\\} (\\d+) x (\\d+).*\\(\\(([0-9.]+):\\d+\\)\\)Name (.+)$";
				Match frmatch = Regex.Match(line, reMatchPlayer, RegexOptions.IgnoreCase);
				if (frmatch.Success)
				{
                    Logger.Debug("PlayerMatch parsed");

                    if (_scState == "Normalspace" && _myrescue!=null)
					{
						AppendStatus("Successful ID match in normal space. Sending good instance.");
						MyClient.Self.InInstance = true;
						TPAMessage instmsg = new TPAMessage
						{
							action = "InstanceSuccessful:update",
							data = new Dictionary<string, string>
							{
								{"RatID", _myplayer.RatId.ToString()},
								{"InstanceSuccessful", "true"},
								{"RescueID", _myrescue.id}
							}
						};
						_apworker.SendTpaMessage(instmsg);
					}
					AppendStatus("Successful identity match! ID: " + frmatch.Groups[1] + " IP:" + frmatch.Groups[3]);
				}

				const string reMatchNat = @"RxRoute:(\d+)+ Comp:(\d)\[IP4NAT:(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})+:(\d{1,5}),(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})+:(\d{1,5}),(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})+:(\d{1,5}),(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})+:(\d{1,5}),(\d),(\d),(\d),(\d{1,4})\]\[Relay:";
				Match natmatch = Regex.Match(line, reMatchNat, RegexOptions.IgnoreCase);
				if (natmatch.Success)
				{
					Logger.Debug("Found NAT datapoint for runID " + natmatch.Groups[1] + ": " + natmatch.Groups[11]);
					NatType clientnat = (NatType) Enum.Parse(typeof (NatType), match.Groups[11].Value);
					switch (clientnat)
					{
						case NatType.Blocked:
							_tc.TrackMetric("ClientNATBlocked", 1);
							break;
						case NatType.Unknown:
							_tc.TrackMetric("ClientNATUnknown", 1);
							break;
						case NatType.Open:
							_tc.TrackMetric("ClientNATOpen", 1);
							break;
						case NatType.FullCone:
							_tc.TrackMetric("ClientNATFullCone", 1);
							break;
						case NatType.Failed:
							_tc.TrackMetric("ClientNATFailed", 1);
							break;
						case NatType.SymmetricUdp:
							_tc.TrackMetric("ClientNATSymmetricUDP", 1);
							break;
						case NatType.Restricted:
							_tc.TrackMetric("ClientNATRestricted", 1);
							break;
						case NatType.Symmetric:
							_tc.TrackMetric("ClientNATSymmetric", 1);
							break;
					}
					_tc.Flush();
				}

				const string reMatchStats = "machines=(\\d+)&numturnlinks=(\\d+)&backlogtotal=(\\d+)&backlogmax=(\\d+)&avgsrtt=(\\d+)&loss=([0-9]*(?:\\.[0-9]*)+)&&jit=([0-9]*(?:\\.[0-9]*)+)&act1=([0-9]*(?:\\.[0-9]*)+)&act2=([0-9]*(?:\\.[0-9]*)+)";
				Match statmatch = Regex.Match(line, reMatchStats, RegexOptions.IgnoreCase);
				if (statmatch.Success)
				{
					Logger.Info("Updating connection statistics.");
					ConnInfo.Srtt = int.Parse(statmatch.Groups[5].Value);
					ConnInfo.Loss = float.Parse(statmatch.Groups[6].Value);
					ConnInfo.Jitter = float.Parse(statmatch.Groups[7].Value);
					ConnInfo.Act1 = float.Parse(statmatch.Groups[8].Value);
					ConnInfo.Act2 = float.Parse(statmatch.Groups[9].Value);
					Dispatcher disp = Dispatcher;
					disp.BeginInvoke(DispatcherPriority.Normal,
						(Action)
							(() =>
								ConnectionStatus.Text =
									"SRTT: " + Conninfo.Srtt + " Jitter: " + Conninfo.Jitter + " Loss: " +
									Conninfo.Loss + " In: " + Conninfo.Act1 + " Out: " + Conninfo.Act2));
				}
                if (line.Contains("</data>"))
                {
                    Logger.Debug("End of FriendsXML, send buffer to friendsparser.");
                    _xmlparselist += line;
                    ParseFriendsList(_xmlparselist);
                    return;
                }
                if (line.Contains("<item>"))
                {
                    Logger.Debug("Appending xml item to parselist.");
                    _xmlparselist += line;
                    return;
                }
				if (line.Contains("<data>"))
				{
					Logger.Debug("Startline for FriendsXML, initialize XML buffer");
                    _xmlparselist = "";
                    _xmlparselist += line;
                    return;
				}
 				if (line.Contains("<FriendWingInvite>"))
				{
					Logger.Debug("Wing invite detected, parsing...");
					ParseWingInvite(line);
				}

				if (line.Contains("JoinSession:WingSession:") && line.Contains(MyClient.ClientIp))
				{
					Logger.Debug("Prewing communication underway...");
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

				if (line.Contains("NormalFlight") && _scState == "Supercruise")
				{
					_scState = "Normalspace";
					Logger.Debug("Drop to normal space detected.");
					//voice.Speak("Dropping to normal space.");
				}

				if (line.Contains("Supercruise") && _scState == "Normalspace")
				{
					_scState = "Supercruise";
					Logger.Debug("Entering supercruise.");
					//voice.Speak("Entering supercruise.");
				}
				if (line.Contains("JoinSession:BeaconSession") && line.Contains(MyClient.ClientIp))
				{
					AppendStatus("Client's Beacon in sight.");
					MyClient.Self.Beacon = true;
					if (_myrescue != null)
					{
						TPAMessage bcnmsg = new TPAMessage
						{
							action = "BeaconSpotted:update",
							data = new Dictionary<string, string>
							{
								{"BeaconSpotted", "true"},
								{"RatID", _myplayer.RatId.ToString()},
								{"RescueID", _myrescue.id}
							}
						};
						_apworker.SendTpaMessage(bcnmsg);
					}
				}
			}
			catch (Exception ex)
			{
				Logger.Debug("Exception in ParseLine: " + ex.Message + "@"+ex.Source +":"+ ex.Data);
				_tc.TrackException(ex);
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
				_tc.TrackEvent("SystemChange");
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
					if (firstsys != null && firstsys.Name == value)
					{
						if (firstsys.Coords == default(EdsmCoords))
							Logger.Debug("Got a match on " + firstsys.Name + " but it has no coords.");
						else
							Logger.Debug("Got definite match in first pos, disregarding extra hits:" + firstsys.Name + " X:" +
										firstsys.Coords.X + " Y:" + firstsys.Coords.Y + " Z:" + firstsys.Coords.Z);
						//AppendStatus("Got M:" + firstsys.name + " X:" + firstsys.coords.x + " Y:" + firstsys.coords.y + " Z:" + firstsys.coords.z);
						if (_myTravelLog == null)
						{
							_myTravelLog=new Collection<TravelLog>();
						}

						_myTravelLog.Add(new TravelLog() {System = firstsys, LastVisited = DateTime.Now});
						Logger.Debug("Added system to TravelLog.");
						// Should we add systems even if they don't exist in EDSM? Maybe submit them?
					}

					if (_myrescue != null)
					{
						if (_myrescue.System == value)
						{
							AppendStatus("Arrived in client system. Notifying dispatch.");
                            Logger.Info("Sending 3PA sys+ message!");
							TPAMessage sysmsg = new TPAMessage
							{
								action = "SysArrived:update",
								data = new Dictionary<string, string>
								{
									{"SysArrived", "true"},
									{"RatID", _myplayer.RatId.FirstOrDefault()},
									{"RescueID", _myrescue.id}
								}
							};
							_apworker.SendTpaMessage(sysmsg);
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
						if (firstsys != null)
						{
							Logger.Debug("Getting distance from fuelum to " + firstsys.Name);
							double distance = await CalculateEdsmDistance("Fuelum", firstsys.Name);
							distance = Math.Round(distance, 2);
							await
								disp.BeginInvoke(DispatcherPriority.Normal,
									(Action) (() => DistanceLabel.Content = distance + "LY from Fuelum"));
						}
					}
				}
			}
			catch (Exception ex)
			{
				Logger.Fatal("Exception in triggerSystemChange: " + ex.Message);
				_tc.TrackException(ex);
			}
		}

		private void OnChanged(object source, FileSystemEventArgs e)
		{
			_logFile = new FileInfo(e.FullPath);
			/* Handle changed events */
		}

		private void DutyButton_Click(object sender, RoutedEventArgs e)
		{
			if (MyPlayer.OnDuty == false)
			{
				Button.Content = "On Duty";
				MyPlayer.OnDuty = true;
				_watcher.EnableRaisingEvents = true;
				AppendStatus("Started watching for events in netlog.");
				Button.Background = Brushes.Green;
				StopNetLog = false;
				_threadLogWatcher = new Thread(NetLogWatcher) {Name = "Netlog watcher"};
				_threadLogWatcher.Start();
			}
			else
			{
				Button.Content = "Off Duty";
				MyPlayer.OnDuty = false;
				_watcher.EnableRaisingEvents = false;
				AppendStatus("\nStopped watching for events in netlog.");
				Button.Background = Brushes.Red;
				StopNetLog = true;
			}
			try {
				TPAMessage dutymessage = new TPAMessage
				{
					action = "OnDuty:update",
					data = new Dictionary<string, string>
					{
						{"OnDuty", MyPlayer.OnDuty.ToString()},
						{"RatID", _myplayer.RatId.FirstOrDefault()},
						{"currentSystem", MyPlayer.CurrentSystem}
					}
				};
				// _apworker.SendTpaMessage(dutymessage); // Disabled while testing, it's spammy.
			}
			catch (Exception ex)
			{
				Logger.Fatal("Exception in sendTPAMessage: " + ex.Message);
				_tc.TrackException(ex);
			}
		}

		private void NetLogWatcher()
		{
			AppendStatus("Netlogwatcher started.");
			try
			{
				while (!StopNetLog)
				{
					Thread.Sleep(2000);

					FileInfo fi = new FileInfo(_logFile.FullName);
					if (fi.Length != _fileSize)
					{
						ReadLogfile(fi.FullName); /* Maybe a poke on the FS is enough to wake watcher? */
						_fileOffset = fi.Length;
						_fileSize = fi.Length;
					}
				}
			}
			catch (Exception ex)
			{
				Logger.Debug("Netlog exception: " + ex.Message);
				_tc.TrackException(ex);
			}
		}

		private void Main_Menu_Click(object sender, RoutedEventArgs e)
		{
			/* Fleh? */
		}

		private void currentButton_Click(object sender, RoutedEventArgs e)
		{
			AppendStatus("Setting client location to current system: "+_myplayer.CurrentSystem);
			// SystemName.Text = "Fuelum";
			// Do actual system name update through Mecha with 3PAM
		}

		private void UpdateButton_Click(object sender, RoutedEventArgs e)
		{
			// No more of this testing bull, let's actually send the updated system now.
			if (MyClient==null)
			{
				Logger.Debug("No current rescue, ignoring system update request.");
				return;
			}

			TPAMessage systemmessage = new TPAMessage
			{
				action = "ClientSystem:update",
				data = new Dictionary<string, string>
				{
					{"SystemName", SystemName.Text},
					{"RatID", _myplayer.RatId.FirstOrDefault()},
					{"RescueID", MyClient.Rescue.id}
				}
			};
			_apworker.SendTpaMessage(systemmessage);
		}
		private async void InitRescueGrid()
		{
			try {
				Logger.Info("Initializing Rescues grid");
				Dictionary<string, string> data = new Dictionary<string, string> {{"open", "true"}};
				_rescues = new RootObject();
				if (_apworker.Ws.State != WebSocketState.Open)
				{
					Logger.Info("No available WebSocket connection, falling back to HTML API.");
					string col = await _apworker.QueryApi("rescues", data);
					Logger.Debug(col == null ? "No COL returned from Rescues." : "Rescue data received from HTML API.");
				}
				else
				{
					Logger.Info("Fetching rescues from WS API.");
                    RescueGrid.AutoGenerateColumns = false;
                    APIQuery rescuequery = new APIQuery
					{
						action = "rescues:read",
						data = new Dictionary<string, string> {{"open", "true"}}
					};
					_apworker.SendQuery(rescuequery);
				}

				//await disp.BeginInvoke(DispatcherPriority.Normal, (Action)(() => ItemsSource.Clear()));
				//await disp.BeginInvoke(DispatcherPriority.Normal, (Action)(() => rescues.Data.ForEach(datum => ItemsSource.Add(datum))));
				//await GetMissingRats(rescues);
			}
			catch(Exception ex)
			{
				Logger.Fatal("Exception in InitRescueGrid: " + ex.Message);
				_tc.TrackException(ex);
			}
		}

		private async void RescueGrid_SelectionChanged(object sender, EventArgs e)
		{
			if (RescueGrid.SelectedItem == null)
				return;
			/*			BackgroundWorker bgworker = new BackgroundWorker() {WorkerReportsProgress = true};
						bgworker.DoWork += (s, e2) => {
							RecalculateJumps();
						};
						bgworker.RunWorkerAsync();
						*/
			/* The shit I do for you, Marenthyu. Update the grid to show the selection, reset labels and
			 * manually redraw one frame before the thread goes into background work. Yeesh. :P
			 */
			Datum myrow = (Datum)RescueGrid.SelectedItem;
			//Logger.Debug("Client is " + myrow.Client);

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

			Dispatcher disp = Dispatcher;
			await disp.BeginInvoke(DispatcherPriority.Normal, (Action)(() => ClientName.Text = myrow.Client));
			//ClientName.Text = myrow.Client;

			AssignedRats = myrow.Rats.Any()
				? string.Join(", ", rats)
				: string.Empty;
			await disp.BeginInvoke(DispatcherPriority.Normal, (Action)(() => SystemName.Text = myrow.System));
			DistanceToClient = -1;
			DistanceToClientString = "Calculating...";
			JumpsToClient = "Calculating...";
			DispatcherFrame frame = new DispatcherFrame();
			await Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(delegate (object parameter) {
				frame.Continue = false;
				return null;
			}), null);
			Dispatcher.PushFrame(frame);
			await RecalculateJumps(myrow.System);
		}

		private async Task RecalculateJumps(string system)
		{
			try
			{
				ClientDistance distance = await GetDistanceToClient(system);
				DistanceToClient = Math.Round(distance.Distance, 2);
                Logger.Debug("Setting JTC based on jump distance " + MyPlayer.JumpRange + ": " + Math.Ceiling(distance.Distance / MyPlayer.JumpRange).ToString());
                JumpsToClient = Math.Ceiling(distance.Distance / MyPlayer.JumpRange).ToString(CultureInfo.InvariantCulture);
			}
			catch (Exception ex)
			{
				Logger.Fatal("Exception in RecalculateJumps: " + ex.Message);
			}
			return;
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
					string response = await _apworker.QueryApi("rats", new Dictionary<string, string> { { "id", ratId }, { "limit", "1" } });
					JObject jsonRepsonse = JObject.Parse(response);
					List<JToken> tokens = jsonRepsonse["data"].Children().ToList();
					Rat rat = JsonConvert.DeserializeObject<Rat>(tokens[0].ToString());
					Rats.TryAdd(ratId, rat);

					Logger.Debug("Got name for " + ratId + ": " + rat.CmdrName);
				}
			}
			catch(Exception ex)
			{
				Logger.Fatal("Exception in GetMissingRats: " + ex.Message);
				_tc.TrackException(ex);
			}
		}

		private async Task<string> GetRatName(string ratid)
		{
			try
			{ 
				string response = await _apworker.QueryApi("rats", new Dictionary<string, string> { { "id", ratid }, { "limit", "1" } });
				JObject jsonRepsonse = JObject.Parse(response);
				List<JToken> tokens = jsonRepsonse["data"].Children().ToList();
				Rat rat = JsonConvert.DeserializeObject<Rat>(tokens[0].ToString());
				Logger.Debug("Got name for " + ratid + ": " + rat.CmdrName);
				return rat.CmdrName;
			}
			catch(Exception ex)
			{
				Logger.Fatal("Exception in GetRatName: " + ex.Message);
				_tc.TrackException(ex);
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
        private async void RefreshEDDBData_Click(object sender, RoutedEventArgs e)
        {
            AppendStatus("Forcing full EDDB database refresh. This will take a while.");
            _fbworker.DropDB();
            AppendStatus("Starting EDDB database refresh.");
            await _eddbworker.UpdateEddbData(true);
        }
        #region EDSM
        public async Task<List<EdsmSystem>> QueryEdsmSystem(string system)
		{
			Logger.Debug("Querying EDSM (or rather SQL) for system " + system);
			AppendStatus("Querying database for " + system);
			List<EdsmSystem> m = _fbworker.GetSystemAsEdsm(system);
			return m;
/*			if (system.Length < 3)
									{
										//This would pretty much download the entire EDSM database. Refuse to do it.
										Logger.Fatal("Too short EDSM query passed to QueryEDSMSystem: " + system);
										return new List<EdsmSystem>();
									}
									try
									{
										_tc.TrackEvent("EDSMQuery");
										using (HttpClient client = new HttpClient())
										{
											UriBuilder content = new UriBuilder(EdsmUrl + "systems?sysname=" + system + "&coords=1") {Port = -1};
											AppendStatus("Querying EDSM for " + system);
											Logger.Debug("Building query: " + content);
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
										Logger.Fatal("Exception in QueryEDSMSystem: " + ex.Message);
										_tc.TrackException(ex);
										return new List<EdsmSystem>();
									}
									*/

		}

		// TODO: Be less stupid and actually support finding systems that AREN'T procedural. Duh.
		public async Task<IEnumerable<EdsmSystem>> GetCandidateSystems(string target)
		{
			Logger.Debug("(GetCandidateSystems) Finding candidate systems for " + target);
			try {
				string sysmatch = "([A-Z][A-Z]-[A-z]+) ([a-zA-Z])+(\\d+(?:-\\d+)+?)";
				Match mymatch = Regex.Match(target, sysmatch, RegexOptions.IgnoreCase);
				IEnumerable<EdsmSystem> candidates = await QueryEdsmSystem(target.Substring(0, target.IndexOf(mymatch.Groups[3].Value, StringComparison.Ordinal)));
				Logger.Debug("Candidate count is " + candidates.Count() + " from a subgroup of " + mymatch.Groups[3].Value);
				_tc.TrackMetric("CandidateCount", candidates.Count());
				var finalcandidates = candidates.Where(x => x.Coords != null).ToList();
				Logger.Debug("FinalCandidates with coords only is size " + finalcandidates.Count);
				if (!finalcandidates.Any())
				{
					Logger.Debug("No final candidates, widening search further...");
					candidates = await QueryEdsmSystem(target.Substring(0, target.IndexOf(mymatch.Groups[2].Value, StringComparison.Ordinal)));
					finalcandidates = candidates.Where(x => x.Coords != null).ToList();
					if (!finalcandidates.Any())
					{
						Logger.Debug("Still nothing! Querying whole sector.");
						candidates = await QueryEdsmSystem(target.Substring(0, target.IndexOf(mymatch.Groups[1].Value, StringComparison.Ordinal)));
						finalcandidates = candidates.Where(x => x.Coords != null).ToList();
					}
				}
                Logger.Debug("Final count before return from GetCandidateSystems is " + finalcandidates.Count());
				return finalcandidates;
			}
			catch(Exception ex)
			{
				Logger.Fatal("Exception in GetCandidateSystems: " + ex.Message);
				_tc.TrackException(ex);
				return new List<EdsmSystem>();
			}
		}

		public async Task<ClientDistance> GetDistanceToClient(string target)
		{
			int logdepth = 0;
			EdsmCoords targetcoords =new EdsmCoords();
			ClientDistance cd = new ClientDistance();
			EdsmCoords sourcecoords = FuelumCoords;
            EdsmSystem sourceSystem = new EdsmSystem();
            sourceSystem.Name = "Fuelum";
            sourceSystem.Coords = FuelumCoords;
			cd.SourceCertainty = "Fuelum";
			if (_myTravelLog!=null)
			{
				foreach (TravelLog mysource in _myTravelLog.Reverse())
				{
					if (mysource.System.Coords == null)
					{
						logdepth++;
					}
					else
					{
						Logger.Debug("Found TL system to use: " + mysource.System.Name);
						sourcecoords = mysource.System.Coords;
						if (logdepth == 0)
						{
							cd.SourceCertainty = "Exact";
                            sourceSystem = mysource.System;
                            break;
						}
						else
						{
							cd.SourceCertainty = logdepth.ToString();
                            sourceSystem = mysource.System;
                            break;
						}
					}
				}
			}
			IEnumerable<EdsmSystem> candidates = await QueryEdsmSystem(target);
			cd.TargetCertainty = "Exact";

			if (!candidates.Any())
			{
				Logger.Debug("EDSM does not know system '" + target + "'. Widening search...");
				candidates = await GetCandidateSystems(target);
				cd.TargetCertainty = "Nearby";
			}
			EdsmSystem firstOrDefault = candidates.FirstOrDefault();
			if (firstOrDefault != null && firstOrDefault.Coords == null)
			{
				Logger.Debug("Known system '" + target + "', but no coords. Widening search...");
				candidates = await GetCandidateSystems(target);
				cd.TargetCertainty = "Region";
			}
			if (candidates == null || !candidates.Any())
			{
				//Still couldn't find something, abort.
				AppendStatus("Couldn't find a candidate system, aborting...");
				return new ClientDistance();
			}

			Logger.Debug("We have two sets of coords that we can use to find a distance.");
            EdsmSystem edsmSystem = candidates.FirstOrDefault();
            if (edsmSystem != null) targetcoords = edsmSystem.Coords;
            if(sourceSystem == null || sourceSystem.Name==null)
            {
                Logger.Debug("Err... Source system (or its name) is null, that shouldn't happen at this point. Bailing!");
                return new ClientDistance();
            }
            Logger.Debug("Finding from coords: " + sourcecoords.X + " " + sourcecoords.Y + " " + sourcecoords.Z + " ("+sourceSystem.Name+") to " + targetcoords.X + " " + targetcoords.Y + " " + targetcoords.Z+" ("+edsmSystem.Name+")");
			double deltaX = sourcecoords.X - targetcoords.X;
			double deltaY = sourcecoords.Y - targetcoords.Y;
			double deltaZ = sourcecoords.Z - targetcoords.Z;
			double distance = Math.Sqrt(deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ);
			Logger.Debug("Distance should be " + distance);
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
					Logger.Fatal("Null value passed as source to CalculateEDSMDistance! Falling back to Fuelum as source.");
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
					Logger.Debug("Unknown source system.");
					return -1;
				}

				candidates = await QueryEdsmSystem(target);
				var edsmSystems = candidates as EdsmSystem[] ?? candidates.ToArray();
				if (!edsmSystems.Any())
				{
					Logger.Debug("EDSM does not know system '" + target + "'. Widening search...");
					candidates = await GetCandidateSystems(target);
				}

				EdsmSystem firstOrDefault = edsmSystems.FirstOrDefault();
				if (firstOrDefault != null && firstOrDefault.Coords == null)
				{
					Logger.Debug("Known system '" + target + "', but no coords. Widening search...");
					candidates = await GetCandidateSystems(target);
				}

				if (candidates == null || !edsmSystems.Any())
				{
					//Still couldn't find something, abort.
					AppendStatus("Couldn't find a candidate system, aborting...");
					return -1;
				}

				Logger.Debug("I got " + edsmSystems.Count() + " systems with coordinates. Sorting by lexical match and picking first.");
				var sorted = edsmSystems.OrderBy(s => LexicalOrder(target, s.Name));
				EdsmSystem edsmSystem = sorted.FirstOrDefault();
				EdsmCoords targetcoords = edsmSystem?.Coords;

				if (targetcoords != null)
				{
					Logger.Debug("We have two sets of coords that we can use to find a distance.");
					double deltaX = sourcecoords.X - targetcoords.X;
					double deltaY = sourcecoords.Y - targetcoords.Y;
					double deltaZ = sourcecoords.Z - targetcoords.Z;
					double distance = Math.Sqrt(deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ);
					Logger.Debug("Distance should be " + distance);
					return distance;
				}

				AppendStatus("EDSM failed to find coords for system '" + target + "'.");
				return -1;
			}
			catch(Exception ex)
			{
				Logger.Fatal("Exception in CalculateEdsmDistance: " + ex.Message);
				_tc.TrackException(ex);
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
			if (_overlay == null)
			{
				_overlay = new Overlay();
				_overlay.SetCurrentClient(MyClient);
				_overlay.Show();
				IEnumerable<Monitor> monitors = Monitor.AllMonitors;
				if (Settings.Default.OverlayMonitor != "")
				{
					Logger.Debug("Overlaymonitor is" + Settings.Default.OverlayMonitor);
					foreach (Monitor mymonitor in monitors)
					{
						if (mymonitor.Name == Settings.Default.OverlayMonitor)
						{
							_overlay.Left = mymonitor.Bounds.Right - _overlay.Width;
							_overlay.Top = mymonitor.Bounds.Top;
							_overlay.Topmost = true;
							Logger.Debug("Overlay coordinates set to " + _overlay.Left + " x " + _overlay.Top);
							HotKeyHost hotKeyHost = new HotKeyHost((HwndSource)PresentationSource.FromVisual(Application.Current.MainWindow));
							//hotKeyHost.AddHotKey(new CustomHotKey("ToggleOverlay", Key.O, ModifierKeys.Control | ModifierKeys.Alt , true));
							//hotKeyHost.AddHotKey(new CustomHotKey("CopyClientSystemname", Key.C, ModifierKeys.Control | ModifierKeys.Alt , true));
							//hotKeyHost.HotKeyPressed += HandleHotkeyPress;
                            // Broken all of a sudden? May require recode.
						}
					}
				}
				else
				{
					foreach (Monitor mymonitor in monitors)
					{
						Logger.Debug("Monitor ID: " + mymonitor.Name);
						if (mymonitor.IsPrimary)
						{
							_overlay.Left = mymonitor.Bounds.Right - _overlay.Width;
							_overlay.Top = mymonitor.Bounds.Top;
						}
					}
					_overlay.Topmost = true;
					HotKeyHost hotKeyHost = new HotKeyHost((HwndSource)PresentationSource.FromVisual(Application.Current.MainWindow));
					hotKeyHost.AddHotKey(new CustomHotKey("ToggleOverlay", Key.O, ModifierKeys.Control | ModifierKeys.Alt, true));
					hotKeyHost.AddHotKey(new CustomHotKey("CopyClientSystemname", Key.C, ModifierKeys.Control | ModifierKeys.Alt, true));
					hotKeyHost.HotKeyPressed += HandleHotkeyPress;
				}
			}
			else {
				_overlay.Close();
			}
		}

		private void HandleHotkeyPress(object sender, HotKeyEventArgs e)
		{
			Logger.Debug("Hotkey pressed: " + Name + e.HotKey.Key);
			if (e.HotKey.Key == Key.O)
			{
				_overlay.Visibility = _overlay.Visibility == Visibility.Hidden ? Visibility.Visible : Visibility.Hidden;
			}

			if (e.HotKey.Key == Key.C)
			{
				if (_myClient.ClientSystem != null)
				{
					Clipboard.SetText(_myClient.ClientSystem);
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
			if (_overlay != null)
			{
				_overlay.Topmost = true;
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
			RatState ratState = GetRatStateForButton(sender, FrButton, FrButtonCopy, FrButtonCopy1);
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
				_apworker.SendTpaMessage(frmsg);
			}
		}

		private void wrButton_Click(object sender, RoutedEventArgs e)
		{
			RatState ratState = GetRatStateForButton(sender, WrButton, WrButtonCopy, WrButtonCopy1);
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
				_apworker.SendTpaMessage(frmsg);
			}
		}

		private void sysButton_Click(object sender, RoutedEventArgs e)
		{
			RatState ratState = GetRatStateForButton(sender, SysButton, SysButtonCopy, SysButtonCopy1);
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
				_apworker.SendTpaMessage(frmsg);
			}

			ratState.InSystem = !ratState.InSystem;
		}

		private void bcnButton_Click(object sender, RoutedEventArgs e)
		{
			RatState ratState = GetRatStateForButton(sender, BcnButton, BcnButtonCopy, BcnButtonCopy1);
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
				_apworker.SendTpaMessage(frmsg);
			}

			ratState.Beacon = !ratState.Beacon;
		}

		private void instButton_Click(object sender, RoutedEventArgs e)
		{
			RatState ratState = GetRatStateForButton(sender, InstButton, InstButtonCopy, InstButtonCopy1);
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
				_apworker.SendTpaMessage(frmsg);
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
            if (MyClient.Rescue == null || MyPlayer.RatId.FirstOrDefault()==null)
            {
                Logger.Debug("Null rescue or RatID, not doin' nothing.");
                return;
            }

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

			_apworker.SendTpaMessage(fuelmsg);
		}

		private void Runtests_button_click(object sender, RoutedEventArgs e)
		{
            /*			OverlayMessage mymessage = new OverlayMessage
                        {
                            Line1Header = "Nearest station:",
                            Line1Content = "Wollheim Vision, Fuelum (0LY)",
                            Line2Header = "Pad size:",
                            Line2Content = "Large",
                            Line3Header = "Capabilities:",
                            Line3Content = "Refuel, Rearm, Repair"
                        };

                        _overlay?.Queue_Message(mymessage, 30);
                        //InitRescueGrid(); // We do this from post-API initialization now.
            if (MyPlayer.RatId != null)
			{
				AppendStatus("Known RatIDs for self:");
				foreach (string id in MyPlayer.RatId)
				{
					AppendStatus(id);
				}
			}
            else
            {
                AppendStatus("My RatID was null, I'll be mecha for this session.");
                MyPlayer.RatId = new List<string>();
                MyPlayer.RatId.Add("b8655683-bafe-42f9-9e19-529036719a79");
            }

            */
            Logger.Debug("Forcing RescueGrid Update");
            IDictionary<string, string> logindata = new Dictionary<string, string>();
            logindata.Add(new KeyValuePair<string, string>("open", "true"));
            //logindata.Add(new KeyValuePair<string, string>("password", "password"));
            _apworker.SendWs("rescues:read", logindata);

		}



		// ReSharper disable once UnusedParameter.Global TODO ??
		public void CompleteRescueUpdate(string json)
		{
			Logger.Debug("CompleteRescueUpdate was called.");
		}

		private async void button1_Click(object sender, RoutedEventArgs e)
		{
			Logger.Debug("Begin TPA Jump Call...");
			_myrescue = (Datum) RescueGrid.SelectedItem;
			if (_myrescue == null)
			{
				AppendStatus("Null myrescue! Failing.");
				return;
			}
			if (_myrescue != null && _myrescue.id == null)
			{
				Logger.Debug("Rescue ID is null!");
				return;
			}
			if (_myrescue.Client == null)
			{
				AppendStatus("Null client.");
				return;
			}

			if (_myrescue.System == null)
			{
				AppendStatus("Null system.");
				return;
			}
			Logger.Debug("Null tests completed");
			AppendStatus("Tracking rescue. System: " + _myrescue.System + " Client: " + _myrescue.Client);
			MyClient = new ClientInfo
			{
				ClientName = _myrescue.Client,
				Rescue = _myrescue,
				ClientSystem = _myrescue.System
			};
			Logger.Debug("Client info loaded:"+MyClient.ClientName+" in "+MyClient.ClientSystem);
			_overlay?.SetCurrentClient(MyClient);
			ClientDistance distance = await GetDistanceToClient(MyClient.ClientSystem);
			//ClientDistance distance = new ClientDistance {Distance = 500};
			AppendStatus("Sending jumps to IRC...");
			Logger.Debug("Constructing TPA message...");
			var jumpmessage = new TPAMessage();
			Logger.Debug("Setting action.");
			jumpmessage.action = "CallJumps:update";
			jumpmessage.applicationId = "0xDEADBEEF";
			Logger.Debug("Set appID");
			Logger.Debug("Constructing TPA for "+_myrescue.id+" with "+_myplayer.RatId.First());
			jumpmessage.data = new Dictionary<string, string> {
					{"CallJumps", Math.Ceiling(distance.Distance/_myplayer.JumpRange).ToString(CultureInfo.InvariantCulture)},
					{"RescueID", _myrescue.id},
					{"RatID", _myplayer.RatId.FirstOrDefault()},
					{"Lightyears", distance.Distance.ToString(CultureInfo.InvariantCulture)},
					{"SourceCertainty", distance.SourceCertainty},
					{"DestinationCertainty", distance.TargetCertainty}
			};
			Logger.Debug("Sending TPA message");
			_apworker.SendTpaMessage(jumpmessage);
		}


		private async void Button2_Click(object sender, RoutedEventArgs e)
		{
			Logger.Debug("Querying EDDB for closest station to " + _myplayer.CurrentSystem);
			IEnumerable<EdsmSystem> mysys = await QueryEdsmSystem(_myplayer.CurrentSystem);
			var edsmSystems = mysys as EdsmSystem[] ?? mysys.ToArray();
			if (edsmSystems.Any())
			{
				Logger.Debug("Got a mysys with " + edsmSystems.Count() + " elements");
				var station = Eddbworker.GetClosestStation(edsmSystems.First().Coords);
				EddbSystem system = Eddbworker.GetSystemById(station.system_id);
				AppendStatus("Closest populated system to '"+_myplayer.CurrentSystem+"' is '" + system.name+
							"', closest station to star with known coordinates is '" + station.name + "'.");
				double distance = await CalculateEdsmDistance(_myplayer.CurrentSystem, edsmSystems.First().Name);
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

				_overlay?.Queue_Message(mymessage, 30);
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
