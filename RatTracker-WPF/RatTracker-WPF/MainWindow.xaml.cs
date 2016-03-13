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
using RatTracker_WPF.Models.EDDB;
using System.Windows.Data;
using System.Collections.ObjectModel;
using Microsoft.ApplicationInsights.DataContracts;

namespace RatTracker_WPF
{
	/// <summary>
	///     Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window, INotifyPropertyChanged
	{
		#region GlobalVars
		private const bool TestingMode = true; // Use the TestingMode bool to point RT to test API endpoints and non-live queries. Set to false when deployed.
		private const string Unknown = "unknown";
		/* These can not be static readonly. They may be changed by the UI XML pulled from E:D. */
		public static Brush RatStatusColourPositive = Brushes.LightGreen;
		public static Brush RatStatusColourPending = Brushes.Orange;
		public static Brush RatStatusColourNegative = Brushes.Red;
		private static readonly string edsmURL = "http://www.edsm.net/api-v1/";
		private static readonly ILog logger =
			LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		private static EdsmCoords fuelumCoords = new EdsmCoords() {X = 52, Y = -52.65625, Z = 49.8125}; // Static coords to Fuelum, saves a EDSM query

		//private readonly SpVoice voice = new SpVoice();
		private RootObject activeRescues = new RootObject(); // TODO: Rename to a better model name than RootObject
		private APIWorker apworker; // Provides connection to the API
		private string assignedRats; // String representation of assigned rats to current case, bound to the UI 
		public ConnectionInfo conninfo = new ConnectionInfo(); // The rat's connection information
		private double distanceToClient; // Bound to UI element
		private string distanceToClientString; // Bound to UI element
		private long fileOffset; // Current offset in NetLog file
		private long fileSize; // Size of current netlog file
		private string jumpsToClient; // Bound to UI element
		private string logDirectory = Settings.Default.NetLogPath; // TODO: Remove this assignment and pull live from Settings, have the logfile watcher reaquire file if settings are changed.
		private FileInfo logFile; // Pointed to the live netLog file.
		private ClientInfo myClient = new ClientInfo(); // Semi-redundant UI bound data model. Somewhat duplicates myrescue, needs revision.
		private PlayerInfo myplayer = new PlayerInfo(); // Playerinfo, bound to various UI elements
		Datum myrescue; // TODO: See myClient - must be refactored.
		private ICollection<TravelLog> myTravelLog; // Log of recently visited systems.
		private Overlay overlay; // Pointer to UI overlay
		RootObject rescues; // Current rescues. Source for items in rescues datagrid
		private string scState; // Supercruise state.
		public bool stopNetLog; // Used to terminate netlog reader thread.
		private TelemetryClient tc = new TelemetryClient(); 
		private Thread threadLogWatcher; // Holds logwatcher thread.
		private FileSystemWatcher watcher; // FSW for the Netlog directory.
		private EDDBData eddbworker;
		private static object _syncLock = new object();
		#endregion

		/*
		 * Initializes the application. Starts AI telemetry, checks log directory and makes sure AppConfig is configured for proper operation.
		 * Any pre-initialization needs to go here.
		 */
		public MainWindow()
		{
			logger.Info("---Starting RatTracker---");
			tc.Context.Session.Id = Guid.NewGuid().ToString();
			tc.Context.Device.OperatingSystem = Environment.OSVersion.ToString();
			tc.Context.User.Id = Environment.UserName.ToString();
			tc.Context.Component.Version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
			tc.TrackPageView("MainWindow");
			InitializeComponent();
			logger.Debug("Parsing AppConfig...");
			if (ParseEDAppConfig())
				CheckLogDirectory();
			else
				AppendStatus("RatTracker does not have a valid path to your E:D directory. This will probably break RT! Please check your settings.");
			logger.Debug("Initialize API...");
			InitAPI();
			logger.Debug("Initialize EDDB...");
			InitEDDB();
			logger.Debug("Initialize player data...");
			InitPlayer();
			DataContext = this;
			
		}

		public void Reinitialize()
		{
			logger.Debug("Reinitializing application...");
			if (ParseEDAppConfig())
				CheckLogDirectory();
			else
				AppendStatus("RatTracker does not have a valid path to your E:D directory. This will probably break RT! Please check your settings.");
			InitAPI();
			InitEDDB();
			InitPlayer();
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
		private void InitAPI()
		{
			try
			{
				logger.Info("Initializing API connection...");
				if(apworker==null)
					apworker = new APIWorker();
				apworker.InitWs();
				apworker.OpenWs();
				apworker.ws.MessageReceived += websocketClient_MessageReceieved;
			}
			catch (Exception ex)
			{
				logger.Fatal("Exception in InitAPI: " + ex.Message);
			}
		}

		public void InitPlayer()
		{
			if (Settings.Default.JumpRange.GetType() == typeof(float))
				myplayer.JumpRange = Settings.Default.JumpRange;
			else
				myplayer.JumpRange = 30;
			myplayer.CurrentSystem = "Fuelum";
		}

		private async void InitEDDB()
		{
			AppendStatus("Initializing EDDB.");
			if(eddbworker==null)
				eddbworker = new EDDBData();
			string status = await eddbworker.UpdateEDDBData();
			AppendStatus("EDDB: " + status);
		}
		/* Moved WS connection to the apworker, but to actually parse the messages we have to hook the event
         * handler here too.
         */
		#endregion

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
						await disp.BeginInvoke(DispatcherPriority.Normal, (Action)(()=> ItemsSource.Clear()));
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
						if (meta.count > 0)
						{
							AppendStatus("Got user data for " + realdata[0].email);
							MyPlayer.RatID = new List<string>();
							foreach (string id in realdata[0].CMDRs)
							{
								AppendStatus("RatID " + id + " added to identity list.");
								MyPlayer.RatID.Add(id);
							}
							myplayer.RatName = await GetRatName(MyPlayer.RatID.FirstOrDefault());
						}
						break;
					case "rescue:updated":
						Datum updrescue = realdata.ToObject<Datum>();
						Datum myrescue = rescues.Data.Where(r => r._id == updrescue._id).FirstOrDefault();
						if (myrescue == null)
							break;
						if (updrescue.Open == false)
						{
							AppendStatus("Rescue closed: " + updrescue.Client.NickName);
							await disp.BeginInvoke(DispatcherPriority.Normal, (Action)(() => ItemsSource.Remove(myrescue)));
						}
						else
						{
							rescues.Data[rescues.Data.IndexOf(myrescue)] = updrescue;
							await disp.BeginInvoke(DispatcherPriority.Normal, (Action)(() => ItemsSource[ItemsSource.IndexOf(myrescue)] = updrescue));
							logger.Debug("Rescue updated: "+updrescue.Client.NickName);
						}
						break;
					case "rescue:created":
						AppendStatus("New rescue arrived!" + realdata);
						Datum newrescue = realdata.ToObject<Datum>();
						AppendStatus("New rescue client name: " + newrescue.Client.NickName);
						OverlayMessage nr = new OverlayMessage();
						nr.Line1Header = "New rescue:";
						nr.Line1Content = newrescue.Client.NickName;
						nr.Line2Header = "System:";
						nr.Line2Content = newrescue.System;
						nr.Line3Header = "Platform:";
						nr.Line3Content = newrescue.Platform;
						nr.Line4Header = "Press Ctrl-Alt-C to copy system name to clipboard";
						if(overlay!=null)
							await disp.BeginInvoke(DispatcherPriority.Normal, (Action)(()=> overlay.Queue_Message(nr, 30)));
						await disp.BeginInvoke(DispatcherPriority.Normal, (Action)(() => ItemsSource.Add(newrescue))); // Maybe this works?
						/* 
						 * This does not work. Even if we can add the data to the collection, it seems we're not triggering the
						 * right events to make RescueGrid update. Maybe this needs an ObservableCollection?
						 *
						lock (rescues)
						/{
							rescues.Data.Add(newrescue);
						}
						await disp.BeginInvoke(DispatcherPriority.Normal,
							(Action)(() => RescueGrid.ItemsSource = rescues.Data));
						await GetMissingRats(rescues);
						*/
						//await disp.BeginInvoke(DispatcherPriority.Normal, (Action)(()=> InitRescueGrid()));
						break;
					case "stream:subscribe":
						logger.Debug("Subscribed to 3PA stream " + data.ToString());
						break;
					default:
						logger.Info("Unknown API action field: " + meta.action);
						break;
				}
				if (meta.id != null)
					AppendStatus("My connID: " + meta.id);
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
		public void TrackFatalException(Exception ex)
		{
			var exceptionTelemetry = new Microsoft.ApplicationInsights.DataContracts.ExceptionTelemetry(new
				Exception());
			exceptionTelemetry.HandledAt =
				Microsoft.ApplicationInsights.DataContracts.ExceptionHandledAt.
					Unhandled;
			tc.TrackException(exceptionTelemetry);
		}

		private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			TrackFatalException(e.ExceptionObject as Exception);
			tc.Flush();
		}
		/*
		 * Parses E:D's AppConfig and looks for the configuration variables we need to make RT work.
		 * Offers to change them if not set correctly.
		 */
		public bool ParseEDAppConfig()
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
					else
					{
						logger.Debug("Found Windows 10 installation. Setting application paths...");
						Settings.Default.EDPath = edProductDir;
						Settings.Default.NetLogPath = edProductDir + "\\logs";
						Settings.Default.Save();
					}
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
					break; // We don't need to do AppConfig work on that.
				logger.Info("Checking AppConfig in Product directory " + dir);
				try
				{
					logger.Debug("Loading " + dir + @"\AppConfig.xml");
					XDocument appconf = XDocument.Load(dir + @"\AppConfig.xml");
					XElement monitor = appconf.Element("AppConfig").Element("Display").Element("Monitor");

					XElement networknode = appconf.Element("AppConfig").Element("Network");
					if (networknode.Attribute("VerboseLogging") == null)
					{
						// Nothing is set up! This makes testing the attributes difficult, so initialize VerboseLogging at least.
						networknode.SetAttributeValue("VerboseLogging", 0);
						logger.Info("No VerboseLogging configuration at all. Setting temporarily for testing.");
					}
					if (networknode.Attribute("VerboseLogging").Value != "1" || networknode.Attribute("ReportSentLetters") == null ||
						networknode.Attribute("ReportReceivedLetters") == null)
					{
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
								XmlWriterSettings settings = new XmlWriterSettings();
								settings.OmitXmlDeclaration = true;
								settings.Indent = true;
								settings.NewLineOnAttributes = true;
								StringWriter sw = new StringWriter();
								using (XmlWriter xw = XmlWriter.Create(dir + @"\AppConfig.xml", settings))
									appconf.Save(xw);
								logger.Info("Wrote new configuration to " + dir + @"\AppConfig.xml");
								tc.TrackEvent("AppConfigAutofixed");
								return true;
							case MessageBoxResult.No:
								logger.Info("No alterations performed.");
								tc.TrackEvent("AppConfigDenied");
								return false;
						}
						return true;
					}
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
				StatusDisplay.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action<string>(AppendStatus),
					text);
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
			string xmlData;
			int count = 0;
			xmlData = friendsList.Substring(friendsList.IndexOf("<") + friendsList.Length);
			logger.Debug("Raw xmlData: " + xmlData);
			try
			{
				XDocument xdoc = XDocument.Parse(friendsList);
				logger.Debug("Successful XML parse.");
				XElement rettest = xdoc.Element("OK");
				if (rettest != null)
					logger.Debug("Last friendslist action: " + xdoc.Element("OK").Value);
				IEnumerable<XElement> friends = xdoc.Descendants("item");
				foreach (XElement friend in friends)
				{
					byte[] byteenc;
					UnicodeEncoding unicoded = new UnicodeEncoding();
					/* string preencode = Regex.Replace(friend.Element("name").Value, ".{2}", "\\x$0"); */
					byteenc = StringToByteArray(friend.Element("name").Value);
					//appendStatus("Friend:" + System.Text.Encoding.UTF8.GetString(byteenc));
					count++;
					if (friend.Element("pending").Value == "1")
					{
						AppendStatus("Pending invite from CMDR " + Encoding.UTF8.GetString(byteenc) + "detected!");
						if (Encoding.UTF8.GetString(byteenc) == MyClient.ClientName)
						{
							MyClient.Self.FriendRequest = RequestState.Recieved;
							AppendStatus("FR is from our client. Notifying Dispatch.");
							TPAMessage frmsg = new TPAMessage();
							frmsg.action = "FriendRequest:update";
							frmsg.data = new Dictionary<string, string>();
							frmsg.data.Add("FRReceived", "true");
							frmsg.data.Add("RatID", "abcdef1234567890");
							frmsg.data.Add("RescueID", "abcdef1234567890");
							apworker.SendTPAMessage(frmsg);
						}
					}
				}

				/* Check the OK status field, which can contain useful information on successful FRs. */
				foreach (XElement element in xdoc.Descendants())
				{
					if (element.Name == "OK")
					{
						logger.Debug("Return code: " + xdoc.Element("data").Element("OK").Value);
						if (xdoc.Element("data").Element("OK").Value.Contains("Invitation accepted"))
						{
							AppendStatus("Friend request accepted.");
							MyClient.Self.FriendRequest = RequestState.Accepted;
						}
					}
				}

				AppendStatus("Parsed " + count + " friends in FRXML.");
			}
			catch (System.Xml.XmlException ex)
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
					byte[] byteenc;
					byteenc = StringToByteArray(wingdata.Element("name").Value);
					AppendStatus("Wingmember:" + Encoding.UTF8.GetString(byteenc));
					if (myrescue != null)
					{
						if (Encoding.UTF8.GetString(byteenc) == myrescue.Client.CmdrName)
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
							MyClient.Self.WingRequest = RequestState.Accepted;
							TPAMessage wrmsg = new TPAMessage();
							wrmsg.action = "WingRequest:update";
							wrmsg.data = new Dictionary<string, string>();
							wrmsg.data.Add("WRReceived", "true");
							wrmsg.data.Add("RatID", "abcdef1234567890");
							wrmsg.data.Add("RescueID", "abcdef1234567890");
							apworker.SendTPAMessage(wrmsg);
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
			if (apworker != null)
				apworker.DisconnectWs();
			if (tc != null)
				tc.Flush();
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
					watcher = new FileSystemWatcher();
					watcher.Path = Settings.Default.NetLogPath;
					watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName |
											NotifyFilters.DirectoryName | NotifyFilters.Size;
					watcher.Filter = "*.log";
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
				myTravelLog = new List<TravelLog>();
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
				Dispatcher disp = Dispatcher;
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
							logger.Info("My RunID: " + line.Substring(line.IndexOf("is ")));
							ConnInfo.runID = line.Substring(line.IndexOf("is "));
						}
						if (line.Contains("RxRoute"))
						{
							// Yes, this early in the netlog, I figure we can just parse the RxRoute without checking for ID. Don't do this later though.
							string rxpattern =
								"IP4NAT:(\\d{1,3}\\.\\d{1,3}\\.\\d{1,3}\\.\\d{1,3})+:(\\d{1,5}),(\\d{1,3}\\.\\d{1,3}\\.\\d{1,3}\\.\\d{1,3})+:(\\d{1,5}),(\\d{1,3}\\.\\d{1,3}\\.\\d{1,3}\\.\\d{1,3})+:(\\d{1,5}),(\\d{1,3}\\.\\d{1,3}\\.\\d{1,3}\\.\\d{1,3})+:(\\d{1,5}),(\\d),(\\d),(\\d),(\\d{1,4})";
							Match match = Regex.Match(line, rxpattern, RegexOptions.IgnoreCase);
							if (match.Success)
							{
								logger.Info("Route info: WAN:" + match.Groups[1].Value + " port " + match.Groups[2].Value + ", LAN:" +
											match.Groups[3].Value + " port " + match.Groups[4].Value + ", STUN: " + match.Groups[5].Value + ":" +
											match.Groups[6].Value + ", TURN: " + match.Groups[7].Value + ":" + match.Groups[8].Value +
											" MTU: " + match.Groups[12].Value + " NAT type: " + match.Groups[9].Value + " uPnP: " +
											match.Groups[10].Value + " MultiNAT: " + match.Groups[11].Value);
								ConnInfo.WANAddress = match.Groups[1].Value + ":" + match.Groups[2].Value;
								ConnInfo.MTU = Int32.Parse(match.Groups[12].Value);
								tc.TrackMetric("Rat_Detected_MTU", ConnInfo.MTU);
								ConnInfo.NATType = (NATType) Enum.Parse(typeof (NATType), match.Groups[9].Value);
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
								ConnInfo.TURNServer = match.Groups[7].Value + ":" + match.Groups[8].Value;
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
					switch (ConnInfo.NATType)
					{
						case NATType.Blocked:
							AppendStatus(
								"WARNING: E:D reports that your network port appears to be blocked! This will prevent you from instancing with other players!");
							ConnTypeLabel.Content = "Blocked!";
							ConnTypeLabel.Foreground = Brushes.Red;
							tc.TrackMetric("NATBlocked", 1);
							break;
						case NATType.Unknown:
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
						case NATType.Open:
							ConnTypeLabel.Content = "Open";
							tc.TrackMetric("NATOpen", 1);
							break;
						case NATType.FullCone:
							ConnTypeLabel.Content = "Full cone NAT";
							tc.TrackMetric("NATFullCone", 1);
							break;
						case NATType.Failed:
							AppendStatus("WARNING: E:D failed to detect your NAT type. This might be problematic for instancing.");
							ConnTypeLabel.Content = "Failed to detect!";
							tc.TrackMetric("NATFailed", 1);
							break;
						case NATType.SymmetricUDP:
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
						case NATType.Restricted:
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
						case NATType.Symmetric:
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
						if (line == "" || line == null)
							logger.Error("Empty line while attempting to read a log line!");
						else
							ParseLine(line);
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
				if (line == "" || line == null)
				{
					logger.Error("ParseLine was passed a null or empty line. This should not happen!");
					return;
				}
				string reMatchSystem = ".*?(System:).*?\\(((?:[^)]+)).*?\\)";
				Match match = Regex.Match(line, reMatchSystem, RegexOptions.IgnoreCase);
				if (match.Success)
				{
					if (match.Groups[2].Value == myplayer.CurrentSystem)
						return;
					TriggerSystemChange(match.Groups[2].Value);
				}

				string reMatchPlayer = "\\{.+\\} (\\d+) x (\\d+).*\\(\\(([0-9.]+):\\d+\\)\\)Name (.+)$";
				Match frmatch = Regex.Match(line, reMatchPlayer, RegexOptions.IgnoreCase);
				if (frmatch.Success)
				{
					if (scState == "Normalspace" && myrescue!=null)
					{
						AppendStatus("Successful ID match in normal space. Sending good instance.");
						MyClient.Self.InInstance = true;
						TPAMessage instmsg = new TPAMessage();
						instmsg.action = "InstanceSuccessful:update";
						instmsg.data = new Dictionary<string, string>();
						instmsg.data.Add("RatID", myplayer.RatID.ToString());
						instmsg.data.Add("InstanceSuccessful", "true");
						instmsg.data.Add("RescueID", myrescue.id);
						apworker.SendTPAMessage(instmsg);
					}
					AppendStatus("Successful identity match! ID: " + frmatch.Groups[1] + " IP:" + frmatch.Groups[3]);
				}
				string reMatchNAT =
					@"RxRoute:(\d+)+ Comp:(\d)\[IP4NAT:(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})+:(\d{1,5}),(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})+:(\d{1,5}),(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})+:(\d{1,5}),(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})+:(\d{1,5}),(\d),(\d),(\d),(\d{1,4})\]\[Relay:";
				Match natmatch = Regex.Match(line, reMatchNAT, RegexOptions.IgnoreCase);
				if (natmatch.Success)
				{
					logger.Debug("Found NAT datapoint for runID " + natmatch.Groups[1] + ": " + natmatch.Groups[11]);
					NATType clientnat = new NATType();
					clientnat = (NATType) Enum.Parse(typeof (NATType), match.Groups[11].Value.ToString());
					switch (clientnat)
					{
						case NATType.Blocked:
							tc.TrackMetric("ClientNATBlocked", 1);
							break;
						case NATType.Unknown:
							tc.TrackMetric("ClientNATUnknown", 1);
							break;
						case NATType.Open:
							tc.TrackMetric("ClientNATOpen", 1);
							break;
						case NATType.FullCone:
							tc.TrackMetric("ClientNATFullCone", 1);
							break;
						case NATType.Failed:
							tc.TrackMetric("ClientNATFailed", 1);
							break;
						case NATType.SymmetricUDP:
							tc.TrackMetric("ClientNATSymmetricUDP", 1);
							break;
						case NATType.Restricted:
							tc.TrackMetric("ClientNATRestricted", 1);
							break;
						case NATType.Symmetric:
							tc.TrackMetric("ClientNATSymmetric", 1);
							break;
					}
					tc.Flush();
				}
				string reMatchStats =
					"machines=(\\d+)&numturnlinks=(\\d+)&backlogtotal=(\\d+)&backlogmax=(\\d+)&avgsrtt=(\\d+)&loss=([0-9]*(?:\\.[0-9]*)+)&&jit=([0-9]*(?:\\.[0-9]*)+)&act1=([0-9]*(?:\\.[0-9]*)+)&act2=([0-9]*(?:\\.[0-9]*)+)";
				Match statmatch = Regex.Match(line, reMatchStats, RegexOptions.IgnoreCase);
				if (statmatch.Success)
				{
					logger.Info("Updating connection statistics.");
					ConnInfo.Srtt = Int32.Parse(statmatch.Groups[5].Value);
					ConnInfo.Loss = float.Parse(statmatch.Groups[6].Value);
					ConnInfo.Jitter = float.Parse(statmatch.Groups[7].Value);
					ConnInfo.Act1 = float.Parse(statmatch.Groups[8].Value);
					ConnInfo.Act2 = float.Parse(statmatch.Groups[9].Value);
					Dispatcher disp = Dispatcher;
					disp.BeginInvoke(DispatcherPriority.Normal,
						(Action)
							(() =>
								connectionStatus.Text =
									"SRTT: " + conninfo.Srtt.ToString() + " Jitter: " + conninfo.Jitter.ToString() + " Loss: " +
									conninfo.Loss.ToString() + " In: " + conninfo.Act1.ToString() + " Out: " + conninfo.Act2.ToString()));
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
					Dispatcher disp = Dispatcher;
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
					TPAMessage bcnmsg = new TPAMessage();
					bcnmsg.action = "BeaconSpotted:update";
					bcnmsg.data = new Dictionary<string, string>();
					bcnmsg.data.Add("BeaconSpotted", "true");
					bcnmsg.data.Add("RatID", myplayer.RatID.ToString());
					bcnmsg.data.Add("RescueID", myrescue._id);
					apworker.SendTPAMessage(bcnmsg);
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
					UriBuilder content = new UriBuilder(edsmURL + "systems?sysname=" + value + "&coords=1") {Port = -1};
					NameValueCollection query = HttpUtility.ParseQueryString(content.Query);
					content.Query = query.ToString();
					HttpResponseMessage response = await client.GetAsync(content.ToString());
					response.EnsureSuccessStatusCode();
					string responseString = await response.Content.ReadAsStringAsync();
					NameValueCollection temp = new NameValueCollection();
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
						if(myTravelLog==null)
							myTravelLog=new Collection<TravelLog>();
						myTravelLog.Add(new TravelLog() {system = firstsys, lastvisited = DateTime.Now});
						logger.Debug("Added system to TravelLog.");
						// Should we add systems even if they don't exist in EDSM? Maybe submit them?
					}

					if (myrescue != null)
						if (myrescue.System == value)
						{
							AppendStatus("Arrived in client system. Notifying dispatch.");
							TPAMessage sysmsg = new TPAMessage();
							sysmsg.action = "SysArrived:update";
							sysmsg.data = new Dictionary<string, string>();
							sysmsg.data.Add("SysArrived", "true");
							sysmsg.data.Add("RatID", "abcdef1234567890");
							sysmsg.data.Add("RescueID", "def1234567890");
							apworker.SendTPAMessage(sysmsg);
							MyClient.Self.InSystem = true;
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
						double distance = await CalculateEDSMDistance("Fuelum", firstsys.Name);
						distance = Math.Round(distance, 2);
						await
							disp.BeginInvoke(DispatcherPriority.Normal,
								(Action) (() => distanceLabel.Content = distance.ToString() + "LY from Fuelum"));
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
				threadLogWatcher = new Thread(NetLogWatcher);
				threadLogWatcher.Name = "Netlog watcher";
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
				TPAMessage dutymessage = new TPAMessage();
				dutymessage.action = "OnDuty:update";
				dutymessage.data = new Dictionary<string, string>();
				dutymessage.data.Add("OnDuty", MyPlayer.OnDuty.ToString());
				dutymessage.data.Add("RatID", myplayer.RatID.FirstOrDefault().ToString());
				dutymessage.data.Add("currentSystem", MyPlayer.CurrentSystem);
				apworker.SendTPAMessage(dutymessage);
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
			bool logChanged = false;
			try
			{
				while (!stopNetLog)
				{
					Thread.Sleep(2000);

					if (logChanged == false)
					{
						FileInfo fi = new FileInfo(logFile.FullName);
						if (fi.Length != fileSize)
						{
							ReadLogfile(fi.FullName); /* Maybe a poke on the FS is enough to wake watcher? */
							fileOffset = fi.Length;
							fileSize = fi.Length;
						}
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

		private async void updateButton_Click(object sender, RoutedEventArgs e)
		{
			// No more of this testing bull, let's actually send the updated system now.

		}
		private async void InitRescueGrid()
		{
			try {
				Dispatcher disp = Dispatcher;
				logger.Info("Initializing Rescues grid");
				Dictionary<string, string> data = new Dictionary<string, string>();
				data.Add("open", "true");
				rescues = new RootObject();
				if (apworker.ws.State != WebSocketState.Open)
				{
					logger.Info("No available WebSocket connection, falling back to HTML API.");
					string col = await apworker.queryAPI("rescues", data);
					if (col == null)
					{
						logger.Debug("No COL returned from Rescues.");
					}
					else
					{
						logger.Debug("Rescue data received from HTML API.");
					}
				}
				else
				{
					logger.Info("Fetching rescues from WS API.");
					APIQuery rescuequery = new APIQuery();
					rescuequery.action = "rescues:read";
					rescuequery.data = new Dictionary<string, string>();
					rescuequery.data.Add("open", "true");
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
			try {
				if (RescueGrid.SelectedItem == null)
					return;
				Datum myrow = (Datum)RescueGrid.SelectedItem;
				logger.Debug("Client is " + myrow.Client.CmdrName);

				var rats = Rats.Where(r => myrow.Rats.Contains(r.Key)).Select(r => r.Value.CmdrName).ToList();
				var count = rats.Count;

				MyClient = new ClientInfo { Rescue = myrow };
				if (count > 0)
				{
					MyClient.Self.RatName = rats[0];
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
				ClientName.Text = myrow.Client.CmdrName;

				AssignedRats = myrow.Rats.Any()
					? string.Join(", ", rats)
					: string.Empty;
				SystemName.Text = myrow.System;
				ClientDistance distance = await GetDistanceToClient(myrow.System);
				DistanceToClient = Math.Round(distance.distance, 2);
				JumpsToClient = MyPlayer.JumpRange > 0 ? Math.Ceiling(distance.distance / MyPlayer.JumpRange).ToString() : string.Empty;
			}
			catch(Exception ex)
			{
				logger.Fatal("Exception in RescueGrid_SelectionChanged: " + ex.Message);
				tc.TrackException(ex);
			}
		}

		private async Task GetMissingRats(RootObject rescues)
		{
			try {
				IEnumerable<string> ratIdsToGet = new List<string>();
				if (rescues.Data == null)
					return;
				IEnumerable<List<string>> datas = rescues.Data.Select(d => d.Rats);
				ratIdsToGet = datas.Aggregate(ratIdsToGet, (current, list) => current.Concat(list));
				ratIdsToGet = ratIdsToGet.Distinct().Except(Rats.Values.Select(x => x._Id));

				foreach (string ratId in ratIdsToGet)
				{
					string response =
						await apworker.queryAPI("rats", new Dictionary<string, string> { { "_id", ratId }, { "limit", "1" } });
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
			try { 

			
				string response =
					await apworker.queryAPI("rats", new Dictionary<string, string> { { "_id", ratid }, { "limit", "1" } });
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
		private void startButton_Click(object sender, RoutedEventArgs e)
		{
			AppendStatus("Starting case: " + ClientName.Text);
		}

		private void MenuItem_Click(object sender, RoutedEventArgs e)
		{
			wndSettings swindow = new wndSettings();
			Nullable<bool> result = swindow.ShowDialog();
			if(result==true)
			{
				AppendStatus("Reinitializing application due to configuration change...");
				Reinitialize();
				return;
			}
			AppendStatus("No changes made, not reinitializing.");
		}

		#region EDSM
		public async Task<IEnumerable<EdsmSystem>> QueryEDSMSystem(string system)
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
					
					UriBuilder content = new UriBuilder(edsmURL + "systems?sysname=" + system + "&coords=1") {Port = -1};
					AppendStatus("Querying EDSM for " + system);
					logger.Debug("Building query: " + content.ToString());
					NameValueCollection query = HttpUtility.ParseQueryString(content.Query);
					content.Query = query.ToString();
					HttpResponseMessage response = await client.GetAsync(content.ToString());
					response.EnsureSuccessStatusCode();
					string responseString = response.Content.ReadAsStringAsync().Result;
					logger.Debug("Got response: " + responseString);
					if (responseString == "-1")
						return new List<EdsmSystem>() {};
					NameValueCollection temp = new NameValueCollection();
					IEnumerable<EdsmSystem> m = JsonConvert.DeserializeObject<IEnumerable<EdsmSystem>>(responseString);
					return m;
				}
			}
			catch (Exception ex)
			{
				logger.Fatal("Exception in QueryEDSMSystem: " + ex.Message);
				tc.TrackException(ex);
				return new List<EdsmSystem>() {};
			}
		}

		// TODO: Be less stupid and actually support finding systems that AREN'T procedural. Duh.
		public async Task<IEnumerable<EdsmSystem>> GetCandidateSystems(string target)
		{
			logger.Debug("Finding candidate systems for " + target);
			try {
				IEnumerable<EdsmSystem> candidates;
				IEnumerable<EdsmSystem> finalcandidates = new List<EdsmSystem>();
				string sysmatch = "([A-Z][A-Z]-[A-z]+) ([a-zA-Z])+(\\d+(?:-\\d+)+?)";
				Match mymatch = Regex.Match(target, sysmatch, RegexOptions.IgnoreCase);
				candidates = await QueryEDSMSystem(target.Substring(0, target.IndexOf(mymatch.Groups[3].Value)));
				logger.Debug("Candidate count is " + candidates.Count().ToString() + " from a subgroup of " + mymatch.Groups[3].Value);
				tc.TrackMetric("CandidateCount", candidates.Count());
				finalcandidates = candidates.Where(x => x.Coords != null);
				logger.Debug("FinalCandidates with coords only is size " + finalcandidates.Count());
				if (finalcandidates.Count() < 1)
				{
					logger.Debug("No final candidates, widening search further...");
					candidates = await QueryEDSMSystem(target.Substring(0, target.IndexOf(mymatch.Groups[2].Value)));
					finalcandidates = candidates.Where(x => x.Coords != null);
					if (finalcandidates.Count() < 1)
					{
						logger.Debug("Still nothing! Querying whole sector.");
						candidates = await QueryEDSMSystem(target.Substring(0, target.IndexOf(mymatch.Groups[1].Value)));
						finalcandidates = candidates.Where(x => x.Coords != null);
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
			EdsmCoords sourcecoords = new EdsmCoords();
			EdsmCoords targetcoords =new EdsmCoords();
			IEnumerable<EdsmSystem> candidates;
			ClientDistance cd = new ClientDistance();
			sourcecoords = fuelumCoords;
			cd.sourcecertainty = "Fuelum";

			foreach (TravelLog mysource in myTravelLog.Reverse())
			{
				if(mysource.system.Coords== null)
				{
					logdepth++;
				}
				else
				{
					logger.Debug("Found TL system to use: " + mysource.system.Name);
					sourcecoords = mysource.system.Coords;
					cd.sourcecertainty = logdepth.ToString();
					break;
				}
			}
			candidates = await QueryEDSMSystem(target);
			cd.targetcertainty = "Exact";
			
			if (candidates == null || candidates.Count() < 1)
			{
				logger.Debug("EDSM does not know system '" + target + "'. Widening search...");
				candidates = await GetCandidateSystems(target);
				cd.targetcertainty = "Nearby";
			}
			if (candidates.FirstOrDefault().Coords == null)
			{
				logger.Debug("Known system '" + target + "', but no coords. Widening search...");
				candidates = await GetCandidateSystems(target);
				cd.targetcertainty = "Region";
			}
			if (candidates == null || candidates.Count() < 1)
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
			double distance = (double)Math.Sqrt(deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ);
			logger.Debug("Distance should be " + distance.ToString());
			cd.distance = distance;
			return cd;
		}
		/* Attempts to calculate a distance in lightyears between two given systems.
        * This is done using EDSM coordinates.
        * 
        * Transitioned to be exact source system, searches for nearest for target.
        * For the client distance calculation, call GetDistanceToClient instead.
        * 
        * 
		* 
        */

		public async Task<double> CalculateEDSMDistance(string source, string target)
		{
			try {
				EdsmCoords sourcecoords = new EdsmCoords();
				EdsmCoords targetcoords = new EdsmCoords();
				IEnumerable<EdsmSystem> candidates;
				if (source == target)
					return 0; /* Well, it COULD happen? People have been known to do stupid things. */
				if (source == null)
				{
					/* Dafuq? */
					logger.Fatal("Null value passed as source to CalculateEDSMDistance!");
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
				candidates = await QueryEDSMSystem(source);
				if (candidates == null || candidates.Count() < 1)
				{
					logger.Debug("Unknown source system.");
					return -1;
				}
				candidates = await QueryEDSMSystem(target);
				if (candidates == null || candidates.Count() < 1)
				{
					logger.Debug("EDSM does not know system '" + target + "'. Widening search...");
					candidates = await GetCandidateSystems(target);
				}
				if (candidates.FirstOrDefault().Coords == null)
				{
					logger.Debug("Known system '" + target + "', but no coords. Widening search...");
					candidates = await GetCandidateSystems(target);
				}
				if (candidates == null || candidates.Count() < 1)
				{
					//Still couldn't find something, abort.
					AppendStatus("Couldn't find a candidate system, aborting...");
					return -1;
				}
				else
				{
					logger.Debug("I got " + candidates.Count() + " systems with coordinates. Sorting by lexical match and picking first.");
					var sorted = candidates.OrderBy(s => LexicalOrder(target, s.Name));
					targetcoords = sorted.FirstOrDefault().Coords;
				}
				if (sourcecoords != null && targetcoords != null)
				{
					logger.Debug("We have two sets of coords that we can use to find a distance.");
					double deltaX = sourcecoords.X - targetcoords.X;
					double deltaY = sourcecoords.Y - targetcoords.Y;
					double deltaZ = sourcecoords.Z - targetcoords.Z;
					double distance = (double)Math.Sqrt(deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ);
					logger.Debug("Distance should be " + distance.ToString());
					return distance;
				}
				else
				{
					AppendStatus("EDSM failed to find coords for system '" + target + "'.");
					return -1;
				}
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
		int LexicalOrder(string query, string name)
		{
			if (name == query)
				return -1;
			if (name.Contains(query))
				return 0;
			return 1;
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
				if (Properties.Settings.Default.OverlayMonitor != "")
				{
					logger.Debug("Overlaymonitor is" + Properties.Settings.Default.OverlayMonitor);
					foreach (Monitor mymonitor in monitors)
					{
						if (mymonitor.Name == Properties.Settings.Default.OverlayMonitor)
						{
							overlay.Left = mymonitor.Bounds.Right - overlay.Width;
							overlay.Top = mymonitor.Bounds.Top;
							overlay.Topmost = true;
							logger.Debug("Overlay coordinates set to " + overlay.Left + " x " + overlay.Top);
							HotKeyHost hotKeyHost = new HotKeyHost((HwndSource)PresentationSource.FromVisual(Application.Current.MainWindow));
							hotKeyHost.AddHotKey(new CustomHotKey("ToggleOverlay", Key.O, ModifierKeys.Control | ModifierKeys.Alt, true));
							hotKeyHost.HotKeyPressed += handleHotkeyPress;

						}
					}
				}
				else {
					foreach (Monitor mymonitor in monitors)
					{
						logger.Debug("Monitor ID: " + mymonitor.Name);
						if (mymonitor.IsPrimary == true)
						{
							overlay.Left = mymonitor.Bounds.Right - overlay.Width;
							overlay.Top = mymonitor.Bounds.Top;
						}
					}
					overlay.Topmost = true;
					HotKeyHost hotKeyHost = new HotKeyHost((HwndSource)PresentationSource.FromVisual(Application.Current.MainWindow));
					hotKeyHost.AddHotKey(new CustomHotKey("ToggleOverlay", Key.O, ModifierKeys.Control | ModifierKeys.Alt, true));
					hotKeyHost.AddHotKey(new CustomHotKey("CopyClientSystemname", Key.C, ModifierKeys.Control | ModifierKeys.Alt, true));
					hotKeyHost.HotKeyPressed += handleHotkeyPress;
				}
			}
			else {
				overlay.Close();
			}
		}

		private void handleHotkeyPress(object sender, HotKeyEventArgs e)
		{
			logger.Debug("Hotkey pressed: " + Name + e.HotKey.Key.ToString());
			if (e.HotKey.Key == Key.O)
			{
				if (overlay.Visibility == Visibility.Hidden)
					overlay.Visibility = Visibility.Visible;
				else
					overlay.Visibility = Visibility.Hidden;
			}
			if (e.HotKey.Key == Key.C)
			{
				if (myClient.ClientSystem != null)
					System.Windows.Clipboard.SetText(myClient.ClientSystem);
			}
		}

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
			IDictionary<string, string> data = new Dictionary<string, string>();
			RatState ratState = GetRatStateForButton(sender, FrButton, FrButton_Copy, FrButton_Copy1);
			TPAMessage frmsg = new TPAMessage();
			frmsg.data = new Dictionary<string, string>();
			if (MyClient != null && MyClient.Rescue != null)
			{
				frmsg.action = "FriendRequest:update";
				frmsg.data.Add("ratID", MyPlayer.RatID.FirstOrDefault());
				frmsg.data.Add("RescueID", MyClient.Rescue._id);
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
			if (frmsg.action != null && ratState.FriendRequest!=RequestState.Recieved)
				apworker.SendTPAMessage(frmsg);
		}

		private void wrButton_Click(object sender, RoutedEventArgs e)
		{
			IDictionary<string, string> data = new Dictionary<string, string>();
			RatState ratState = GetRatStateForButton(sender, WrButton, WrButton_Copy, WrButton_Copy1);
			TPAMessage frmsg = new TPAMessage();
			frmsg.data = new Dictionary<string, string>();
			if (MyClient != null && MyClient.Rescue != null)
			{
				frmsg.action = "WingRequest:update";
				frmsg.data.Add("ratID", MyPlayer.RatID.FirstOrDefault());
				frmsg.data.Add("RescueID", MyClient.Rescue._id);
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
				apworker.SendTPAMessage(frmsg);
		}

		private void sysButton_Click(object sender, RoutedEventArgs e)
		{
			RatState ratState = GetRatStateForButton(sender, SysButton, SysButton_Copy, SysButton_Copy1);
			IDictionary<string, string> data = new Dictionary<string, string>();
			TPAMessage frmsg = new TPAMessage();
			frmsg.data = new Dictionary<string, string>();
			if (MyClient != null && MyClient.Rescue != null)
			{
				frmsg.action = "SysArrived:update";
				frmsg.data.Add("ratID", MyPlayer.RatID.FirstOrDefault());
				frmsg.data.Add("RescueID", MyClient.Rescue._id);
			}
			if (ratState.InSystem==true)
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
				apworker.SendTPAMessage(frmsg);
			ratState.InSystem = !ratState.InSystem;
		}

		private void bcnButton_Click(object sender, RoutedEventArgs e)
		{
			RatState ratState = GetRatStateForButton(sender, BcnButton, BcnButton_Copy, BcnButton_Copy1);
			IDictionary<string, string> data = new Dictionary<string, string>();
			TPAMessage frmsg = new TPAMessage();
			frmsg.data = new Dictionary<string, string>();
			if (MyClient != null && MyClient.Rescue != null)
			{
				frmsg.action = "BeaconSpotted:update";
				frmsg.data.Add("ratID", MyPlayer.RatID.FirstOrDefault());
				frmsg.data.Add("RescueID", MyClient.Rescue._id);
			}
			if (ratState.Beacon==true)
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
				apworker.SendTPAMessage(frmsg);
			ratState.Beacon = !ratState.Beacon;
		}

		private void instButton_Click(object sender, RoutedEventArgs e)
		{
			RatState ratState = GetRatStateForButton(sender, InstButton, InstButton_Copy, InstButton_Copy1);
			IDictionary<string, string> data = new Dictionary<string, string>();
			TPAMessage frmsg = new TPAMessage();
			frmsg.data = new Dictionary<string, string>();
			if (MyClient != null && MyClient.Rescue != null)
			{
				frmsg.action = "InstanceSuccessful:update";
				frmsg.data.Add("ratID", MyPlayer.RatID.FirstOrDefault());
				frmsg.data.Add("RescueID", MyClient.Rescue._id);
			}
			if (ratState.InInstance==true)
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
				apworker.SendTPAMessage(frmsg);
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
			if (Equals(FueledButton.Background, Brushes.Red))
			{
				AppendStatus("Reporting fueled status, requesting paperwork link...");
				FueledButton.Background = Brushes.Green;
				/* image.Source = new BitmapImage(RatTracker_WPF.Properties.Resources.yellow_light); */
			}
			else
			{
				AppendStatus("Fueled status now negative.");
				FueledButton.Background = Brushes.Red;
			}
		}

		private async void button_Click_1(object sender, RoutedEventArgs e)
		{
			//TriggerSystemChange("Lave");
			//TriggerSystemChange("Blaa Hypai AI-I b26-1");
			//DateTime testdate = DateTime.Now;
			/*            myTravelLog.Add(new TravelLog{ system=new EDSMSystem(){ name = "Sol" }, lastvisited=testdate});
                        myTravelLog.Add(new TravelLog { system = new EDSMSystem() { name = "Fuelum" }, lastvisited = testdate});
                        myTravelLog.Add(new TravelLog { system = new EDSMSystem() { name= "Leesti" }, lastvisited = testdate}); */
			//AppendStatus("Travellog now contains " + myTravelLog.Count() + " systems. Timestamp of first is " + myTravelLog.First().lastvisited +" name "+myTravelLog.First().system.name);
			//CalculateEDSMDistance("Sol", SystemName.Text);
			OverlayMessage mymessage = new OverlayMessage();
			mymessage.Line1Header = "Nearest station:";
			mymessage.Line1Content = "Wollheim Vision, Fuelum (0LY)";
			mymessage.Line2Header = "Pad size:";
			mymessage.Line2Content = "Large";
			mymessage.Line3Header = "Capabilities:";
			mymessage.Line3Content = "Refuel, Rearm, Repair";
			if (overlay != null)
				overlay.Queue_Message(mymessage, 30);
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
			TPAMessage testmessage = new TPAMessage();
			testmessage.action = "WingRequest:update";
			testmessage.data = new Dictionary<string, string>();
			testmessage.data.Add("WingRequest", "true");
			testmessage.data.Add("RescueID", "abc1234567890test");
			testmessage.data.Add("RatID", "bcd1234567890test");
			apworker.SendTPAMessage(testmessage);
			IDictionary<string, string> logindata = new Dictionary<string, string>();
			logindata.Add(new KeyValuePair<string, string>("open", "true"));
			//logindata.Add(new KeyValuePair<string, string>("password", "password"));
			apworker.SendWs("rescues:read", logindata);
			InitRescueGrid();
			AppendStatus("Known RatIDs for self:");
			foreach (string id in MyPlayer.RatID)
			{
				AppendStatus(id);
			}
		}

		public void CompleteRescueUpdate(string json)
		{
			logger.Debug("CompleteRescueUpdate was called.");
		}

		private async void button1_Click(object sender, RoutedEventArgs e)
		{
			myrescue = (Datum) RescueGrid.SelectedItem;
			if (myrescue == null)
				AppendStatus("Null myrescue!");
			if (myrescue.Client.CmdrName == null)
				AppendStatus("Null client.");
			if (myrescue.System == null)
				AppendStatus("Null system.");
			AppendStatus("Tracking rescue. System: " + myrescue.System + " Client: " + myrescue.Client.CmdrName);
			MyClient = new ClientInfo();
			MyClient.ClientName = myrescue.Client.CmdrName;
			MyClient.Rescue = myrescue;
			MyClient.ClientSystem = myrescue.System;
			if (overlay != null)
			{
				overlay.SetCurrentClient(MyClient);
			}
			ClientDistance distance = await GetDistanceToClient(MyClient.ClientSystem);
			AppendStatus("Sending jumps to IRC...");
			TPAMessage jumpmessage = new TPAMessage();
			jumpmessage.action = "CallJumps:update";
			jumpmessage.data = new Dictionary<string, string>();
			jumpmessage.data.Add("CallJumps", Math.Ceiling(distance.distance/myplayer.JumpRange).ToString());
			jumpmessage.data.Add("RescueID", myrescue._id);
			jumpmessage.data.Add("RatID", myplayer.RatID.FirstOrDefault());
			jumpmessage.data.Add("Lightyears", distance.distance.ToString());
			jumpmessage.data.Add("SourceCertainty", distance.sourcecertainty);
			jumpmessage.data.Add("DestinationCertainty", distance.targetcertainty);
			apworker.SendTPAMessage(jumpmessage);
		}

		[Serializable]
		public class CustomHotKey : HotKey
		{
			private string name;

			public CustomHotKey(string name, Key key, ModifierKeys modifiers, bool enabled) : base(key, modifiers, enabled)
			{
				Name = name;
			}

			public string Name
			{
				get { return name; }
				set
				{
					if (value != name)
					{
						name = value;
						OnPropertyChanged(name);
					}
				}
			}
		}

		private async void button2_Click(object sender, RoutedEventArgs e)
		{
			logger.Debug("Querying EDDB for closest station to " + myplayer.CurrentSystem);
			IEnumerable<EdsmSystem> mysys = await QueryEDSMSystem(myplayer.CurrentSystem);
			if (mysys != null && mysys.Count() >0)
			{
				logger.Debug("Got a mysys with " + mysys.Count() + " elements");
				var station = eddbworker.GetClosestStation(mysys.First().Coords);
				EDDBSystem system = eddbworker.GetSystemById(station.system_id);
				AppendStatus("Closest populated system to '"+myplayer.CurrentSystem+"' is '" + system.name+
							"', closest station to star with known coordinates is '" + station.name + "'.");
				double distance = await CalculateEDSMDistance(myplayer.CurrentSystem, mysys.First().Name);
				OverlayMessage mymessage = new OverlayMessage();
				
				mymessage.Line1Header = "Nearest station:";
				mymessage.Line1Content = station.name+", "+system.name+" ("+Math.Round(distance,2)+"LY)";
				mymessage.Line2Header = "Pad size:";
				mymessage.Line2Content = station.max_landing_pad_size;
				mymessage.Line3Header = "Capabilities:";
				mymessage.Line3Content = new StringBuilder()
															.AppendIf(station.has_refuel, "Refuel,")
															.AppendIf(station.has_repair, "Repair,")
															.AppendIf(station.has_outfitting, "Outfit").ToString();
				if (overlay != null)
					overlay.Queue_Message(mymessage, 30);
				return;
			}
			AppendStatus("Unable to find a candidate system for location " + MyClient.ClientSystem);
		}
	}
	public static class StringBuilderExtensions
	{
		public static StringBuilder AppendIf(
			this StringBuilder @this,
			bool? condition,
			string str)
		{
			bool realcondition;
			if (condition == null)
				realcondition = false;
			else
				realcondition = (bool)condition;
			if (@this == null)
			{
				throw new ArgumentNullException("this");
			}

			if (realcondition)
			{
				@this.Append(str);
			}

			return @this;
		}
	}

}