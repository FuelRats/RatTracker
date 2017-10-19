using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
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
using Microsoft.ApplicationInsights.DataContracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RatTracker_WPF.Api;
using RatTracker_WPF.Caches;
using RatTracker_WPF.Infrastructure.EventHandlers;
using RatTracker_WPF.Models.Api.V2;
using RatTracker_WPF.Models.Api.V2.OAuth;
using RatTracker_WPF.Models.Api.V2.TPA;
using RatTracker_WPF.Models.App;
using RatTracker_WPF.Models.CmdrJournal;
using RatTracker_WPF.Models.Edsm;
using RatTracker_WPF.Models.NetLog;
using RatTracker_WPF.Properties;
using RatTracker_WPF.ViewModels;

namespace RatTracker_WPF
{
  public delegate void GlobalHeartbeatEvent(object sender, EventArgs args);

  /// <summary>
  ///   Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : INotifyPropertyChanged
  {
    private readonly Cache cache = new Cache();
    private Rescue selectedRescue;

    private PlayerInfo myPlayer = new PlayerInfo(); // Playerinfo, bound to various UI elements
    public PlayerInfo MyPlayer
    {
      get => myPlayer;
      set
      {
        myPlayer = value;
        NotifyPropertyChanged();
      }
    }
    
    public Rescue SelectedRescue
    {
      get => selectedRescue;
      set
      {
        selectedRescue = value; 
        NotifyPropertyChanged();
      }
    }

    public ObservableCollection<Rescue> VisibleRescues { get; } = new ObservableCollection<Rescue>();

    private async void OnRescueClosed(object sender, Rescue rescue)
    {
      AppendStatus("Rescue closed: " + rescue.Client);
      Logger.Debug("Rescue closed: " + rescue.Client);
      if (rescue.Id == AssignedRescueViewModel.Rescue?.Id)
      {
        Logger.Debug("Our active rescue was closed.");
      }
      Logger.Debug("Updating rescue grid.");
      await Dispatcher.InvokeAsync(() => VisibleRescues.Remove(rescue), DispatcherPriority.Normal);
    }

    private async void OnRescueUpdated(object sender, Rescue rescue)
    {
      await Dispatcher.InvokeAsync(() =>
      {
        var updatedRescue = VisibleRescues.SingleOrDefault(x => x.Id == rescue.Id);
        VisibleRescues.Remove(updatedRescue);
        VisibleRescues.Add(rescue);

      }, DispatcherPriority.Normal);
      Logger.Debug("Rescue updated: " + rescue.Client);
    }
    
    private async void OnRescueCreated(object sender, Rescue rescue)
    {
      AppendStatus("New rescue: " + rescue.Client);
      var nr = new OverlayMessage
      {
        Line1Header = "New rescue:",
        Line1Content = rescue.Client,
        Line2Header = "System:",
        Line2Content = rescue.System,
        Line3Header = "Platform:",
        Line3Content = rescue.Platform.ToString().ToUpper(),
        Line4Header = "Press Ctrl-Alt-C to copy system name to clipboard"
      };
      if (_overlay != null)
      {
        await Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() => _overlay.Queue_Message(nr, 30)));
      }

      await Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() => VisibleRescues.Add(rescue)));
    }

    #region GlobalVars

    private const string Unknown = "unknown";

    /* These can not be static readonly. They may be changed by the UI XML pulled from E:D. */
    public static readonly Brush RatStatusColourPositive = Brushes.LightGreen;
    public static readonly Brush RatStatusColourPending = Brushes.Orange;
    public static readonly Brush RatStatusColourNegative = Brushes.Red;
    private static readonly ILog Logger = LogManager.GetLogger(Assembly.GetCallingAssembly().GetName().Name);

    private readonly bool isOauthProcessing;


    // Static coords to Fuelum, saves a EDSM query
    private static readonly Coordinates FuelumCoords = new Coordinates { X = 52, Y = -52.65625, Z = 49.8125 };

    //private readonly SpVoice voice = new SpVoice();
    private ApiWorker apiWorker; // Provides connection to the API

    private string _assignedRats; // String representation of assigned rats to current case, bound to the UI 
    public ConnectionInfo Conninfo = new ConnectionInfo(); // The rat's connection information
    private double _distanceToClient; // Bound to UI element
    private string _distanceToClientString; // Bound to UI element
    private string _jumpsToClient; // Bound to UI element
    private readonly NetLogParser _netlogparser = new NetLogParser();
    private readonly CmdrJournalParser _cmdrJournalParser;

    // ReSharper disable once UnusedMember.Local TODO ??
    private string logDirectory = Settings.Default.NetLogPath;
    // TODO: Remove this assignment and pull live from Settings, have the logfile watcher reaquire file if settings are changed.


    private ICollection<TravelLog> _myTravelLog; // Log of recently visited systems.
    private Overlay _overlay; // Pointer to UI overlay
    public bool StopNetLog; // Used to terminate netlog reader thread.
    private readonly TelemetryClient _tc = new TelemetryClient();
    private Thread _threadLogWatcher; // Holds logwatcher thread.
    private EddbData _eddbworker;
    private bool _heartbeatStopping;

    public event GlobalHeartbeatEvent GlobalHeartbeatEvent;

    public EddbData Eddbworker
    {
      get => _eddbworker;
      set
      {
        _eddbworker = value;
        NotifyPropertyChanged();
      }
    }

    private FireBird _fbworker;

    public FireBird FbWorker
    {
      get => _fbworker;
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

    #region PropertyNotifiers
    
    public ConnectionInfo ConnInfo
    {
      get => Conninfo;
      set
      {
        Conninfo = value;
        NotifyPropertyChanged();
      }
    }


    public string JumpsToClient
    {
      get => string.IsNullOrWhiteSpace(_jumpsToClient) ? Unknown : "~" + _jumpsToClient;
      set
      {
        _jumpsToClient = value;
        NotifyPropertyChanged();
      }
    }

    public double DistanceToClient
    {
      get => _distanceToClient;
      set
      {
        _distanceToClient = value;
        NotifyPropertyChanged();
        DistanceToClientString = string.Empty;
      }
    }

    public string DistanceToClientString
    {
      get => DistanceToClient >= 0
        ? DistanceToClient.ToString(CultureInfo.InvariantCulture)
        : !string.IsNullOrWhiteSpace(_distanceToClientString)
          ? _distanceToClientString
          : Unknown;
      set
      {
        _distanceToClientString = value;
        NotifyPropertyChanged();
      }
    }

    public string AssignedRats
    {
      get => _assignedRats;
      set
      {
        _assignedRats = value;
        NotifyPropertyChanged();
      }
    }

    private bool showOnlyPCCases;

    public bool ShowOnlyPCCases
    {
      get => showOnlyPCCases;
      set
      {
        showOnlyPCCases = value;

        ReloadRescueGrid();
        NotifyPropertyChanged();
      }
    }

    private bool showOnlyActiveCases;

    public bool ShowOnlyActiveCases
    {
      get => showOnlyActiveCases;
      set
      {
        showOnlyActiveCases = value;
        ReloadRescueGrid();
        NotifyPropertyChanged();
      }
    }

    public AssignedRescueViewModel AssignedRescueViewModel
    {
      get => assignedRescueViewModel;
      set
      {
        assignedRescueViewModel = value; 
        NotifyPropertyChanged();
      }
    }

    private void ReloadRescueGrid()
    {
      var rescues = from rescue in cache.GetRescues()
                    where (!ShowOnlyPCCases || rescue.Platform == Platform.Pc)
                          && (!ShowOnlyActiveCases || rescue.Status == RescueState.Open)
                    select rescue;

      Dispatcher.InvokeAsync(() =>
      {
        VisibleRescues.Clear();
        foreach (var rescue in rescues)
        {
          VisibleRescues.Add(rescue);
        }
      });
    }

    public event PropertyChangedEventHandler PropertyChanged;

    #endregion

    #region StartUp

    public MainWindow()
    {
      Logger.Info("---Starting RatTracker---");
      Logger.Info("OAuth stored token is " + Settings.Default.OAuthToken);
      try
      {
        foreach (var arg in Environment.GetCommandLineArgs())
        {
          Logger.Debug("Arg: " + arg);
          if (arg.Contains("rattracker"))
          {
            Logger.Debug("RatTracker was invoked for OAuth code authentication.");
            isOauthProcessing = true;
            var reMatchToken = ".*?code=(.*)?&state=preinit";
            var match = Regex.Match(arg, reMatchToken, RegexOptions.IgnoreCase);
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
          else
          {
            Logger.Debug("Normal startup.");
            _tc.Context.Session.Id = Guid.NewGuid().ToString();
            _tc.Context.Device.OperatingSystem = Environment.OSVersion.ToString();
            _tc.Context.User.Id = Environment.UserName;
            _tc.Context.Component.Version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            _tc.TrackPageView("MainWindow");
            InitializeComponent();
            AssignedRescueViewModel = new AssignedRescueViewModel(apiWorker, cache);
            Loaded += Window_Loaded;
            cache.RescuesReloaded += (sender, args) => ReloadRescueGrid();
            cache.RescueCreated += OnRescueCreated;
            cache.RescueUpdated += OnRescueUpdated;
            cache.RescueClosed += OnRescueClosed;
            _netlogparser.StatusUpdateEvent += DoStatusUpdate;
            Logger.Debug("Parsing AppConfig...");
            if (ParseEdAppConfig())
            {
              _netlogparser.CheckLogDirectory();
            }
            else
            {
              AppendStatus(
                "RatTracker does not have a valid path to your E:D directory. This will probably break RT! Please check your settings.");
            }
            _cmdrJournalParser = new CmdrJournalParser(this);
            _cmdrJournalParser.CommitCrimeEvent += CmdrJournalParser_CommitCrimeEvent;
            _cmdrJournalParser.DiedEvent += CmdrJournalParser_DiedEvent;
            _cmdrJournalParser.EscapeInterdictionEvent += _cmdrJournalParser_EscapeInterdictionEvent;
            _cmdrJournalParser.FsdJumpEvent += CmdrJournalParser_FsdJumpEvent;
            _cmdrJournalParser.HullDamageEvent += CmdrJournalParser_HullDamageEvent;
            _cmdrJournalParser.InterdictedEvent += CmdrJournalParser_InterdictedEvent;
            _cmdrJournalParser.InterdictionEvent += CmdrJournalParser_InterdictionEvent;
            _cmdrJournalParser.ReceiveTextEvent += CmdrJournalParser_ReceiveTextEvent;
            _cmdrJournalParser.SupercruiseEntryEvent += CmdrJournalParser_SupercruiseEntryEvent;
            _cmdrJournalParser.SuperCruiseExitEvent += CmdrJournalParser_SuperCruiseExitEvent;
            _cmdrJournalParser.WingAddEvent += CmdrJournalParser_WingAddEvent;
          }
        }
      }
      catch (Exception ex)
      {
        Logger.Debug($"Exception in token parse: {ex.Message}");
      }
    }

    public void DoInitialize()
    {
      var apiWorker = new BackgroundWorker();
      var eddbWorker = new BackgroundWorker();
      var piWorker = new BackgroundWorker();
      var fbWorker = new BackgroundWorker();
      var hbWorker = new BackgroundWorker();

      if (isOauthProcessing == false)
      {
        apiWorker.DoWork += (s, args) =>
        {
          Logger.Debug("Initialize API...");
          InitApi(true);
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
          _fbworker.FireBirdLoadedEvent += FireBirdLoaded;
        };
        hbWorker.DoWork += delegate
        {
          Logger.Debug("Initalize Heartbeat Thread...");
          InitHeartBeat();
        };
        fbWorker.RunWorkerAsync();
        eddbWorker.RunWorkerAsync();
        apiWorker.RunWorkerAsync();
        piWorker.RunWorkerAsync();
        hbWorker.RunWorkerAsync();
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
    }

    public async void Reinitialize(bool includeToken)
    {
      Logger.Debug("Reinitializing application...");
      if (ParseEdAppConfig())
      {
        _netlogparser.CheckLogDirectory();
      }
      else
      {
        AppendStatus(
          "RatTracker does not have a valid path to your E:D directory. This will probably break RT! Please check your settings.");
      }

      InitApi(true);
      await InitEddb(true);
      await InitPlayer();
      Logger.Debug("Reinitialization complete.");
    }

    private void InitApi(bool includeToken)
    {
      try
      {
        Logger.Info("Initializing API connection...");
        if (apiWorker == null)
        {
          apiWorker = new ApiWorker();
          cache.Init(apiWorker.ResponseHandler);
          apiWorker.ResponseHandler.AddCallback("users:profile", message =>
          {
            MyPlayer.User = JsonApi.Deserialize<User>(message);
          });
        }
        
        apiWorker.InitWs(includeToken);
        apiWorker.OpenWs();

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

    public Task InitPlayer()
    {
      MyPlayer.JumpRange = Settings.Default.JumpRange > 0 ? Settings.Default.JumpRange : 30;
      MyPlayer.CurrentSystem = "Fuelum";
      return Task.FromResult(true);
    }

    private async Task InitEddb(bool reinit)
    {
      if (reinit)
      {
        return;
      }
      AppendStatus("Initializing EDDB.");
      if (Eddbworker == null)
      {
        Eddbworker = new EddbData(ref _fbworker);
      }
      Thread.Sleep(5000);

      var status = await Eddbworker.UpdateEddbData(false);
      AppendStatus("EDDB: " + status);
    }

    /// <summary>
    /// Parses E:D's AppConfig and looks for the configuration variables we need to make RT work.
    /// Offers to change them if not set correctly.
    /// </summary>
    public bool ParseEdAppConfig()
    {
      var edProductDir = Settings.Default.EDPath + "\\Products";
      Logger.Debug("Looking for Product dirs in " + Settings.Default.EDPath + "\\Products");
      try
      {
        if (!Directory.Exists(edProductDir))
        {
          Logger.Fatal("Couldn't find E:D product directory, looking for Windows 10 installation...");
          edProductDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) +
                         "\\Frontier_Developments\\Products"; //Attempt Windows 10 path.
          Logger.Debug("Looking in " + edProductDir);
          if (!Directory.Exists(edProductDir))
          {
            Logger.Fatal(
              "Couldn't find E:D product directory. Aborting AppConfig parse. You must set the path manually in settings.");
            return false;
          }

          Logger.Debug("Found Windows 10 installation. Setting application paths...");
          Settings.Default.EDPath = edProductDir;
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

      foreach (var dir in Directory.GetDirectories(edProductDir))
      {
        if (dir.Contains("COMBAT_TUTORIAL_DEMO"))
        {
          break; // We don't need to do AppConfig work on that.
        }

        Logger.Info("Checking AppConfig in Product directory " + dir);
        try
        {
          Logger.Debug("Loading " + dir + @"\AppConfig.xml");
          var appconf = XDocument.Load(dir + @"\AppConfig.xml");

          var xElement = appconf.Element("AppConfig");
          if (xElement != null)
          {
            var networknode = xElement.Element("Network");
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
                  xAttribute.Value == "1" && networknode.Attribute("ReportSentLetters") != null &&
                  networknode.Attribute("ReportReceivedLetters") != null)
              {
                continue;
              }
            }
            Logger.Error(
              "WARNING: Your Elite:Dangerous AppConfig is not set up correctly to allow RatTracker to work!");
            var result =
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
                var settings = new XmlWriterSettings
                {
                  OmitXmlDeclaration = true,
                  Indent = true,
                  NewLineOnAttributes = true
                };
                using (var xw = XmlWriter.Create(dir + @"\AppConfig.xml", settings))
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

    private async void OAuth_Authorize(string code)
    {
      if (code.Length <= 0)
      {
        return;
      }
      Logger.Debug("A code was passed to connectAPI, attempting token exchange: " + code);
      using (var hc = new HttpClient())
      {
        var myauth = new Auth();
        myauth.Code = code;
        myauth.GrantType = "authorization_code";
        myauth.RedirectUrl = "rattracker://auth";
        var content = new UriBuilder(Path.Combine($"{Settings.Default.APIURL}oauth2/token"))
        {
          Port = Settings.Default.APIPort
        };
        var clientauthheader = Settings.Default.ClientID + ":" + Settings.Default.AppSecret;
        Logger.Debug("Attempting auth with CID: " + Settings.Default.ClientID + " and AS " +
                     Settings.Default.AppSecret);
        hc.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
          Convert.ToBase64String(Encoding.ASCII.GetBytes(
            clientauthheader)));
        var formenc = new FormUrlEncodedContent(new[]
        {
          new KeyValuePair<string, string>("code", code),
          new KeyValuePair<string, string>("grant_type", "authorization_code"),
          new KeyValuePair<string, string>("redirect_uri", "rattracker://auth")
        });
        var response = await hc.PostAsync(content.ToString(), formenc).ConfigureAwait(false);
        var mycontent = response.Content;
        var data = mycontent.ReadAsStringAsync().Result;
        Logger.Debug("OAuth token exchange data: " + data);
        if (data.Contains("access_token"))
        {
          var token = JsonConvert.DeserializeObject<TokenResponse>(data);
          Logger.Debug("Access token received: " + token.AccessToken);
          Settings.Default.OAuthToken = token.AccessToken;
          Settings.Default.Save();
          AppendStatus(
            "OAuth authentication transaction successful, bearer token stored. Please exit RatTracker and start it again.");
          MessageBox.Show(
            "RatTracker has successfully completed OAuth authentication. Please close and restart RatTracker to complete the process.");
          //The fact that I have to cheat this way is FUCKING ANNOYING!
          var rtPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
          File.WriteAllText(rtPath + @"\RatTracker\OAuthToken.tmp", token.AccessToken);
          Logger.Debug("Saved CheatyFile.");
          //_oauthProcessing = false;
          //DoInitialize();  // We can't actually do initialization at this point, because the app gets called by the webbrowser and has a stupid run path.
        }
        else
        {
          Logger.Debug("No access token in data response! Data:" + data);
          AppendStatus("OAuth authentication failed. Please restart RatTracker to retry the operation.");
        }
      }
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
      DoInitialize();
    }

    private void FireBirdLoaded(object sender, FireBirdLoadedArgs args)
    {
      Logger.Debug("Starting EDDB, as FireBird has completed loading.");
    }

    #endregion StartUp

    #region ExceptionHandling

    // ReSharper disable once UnusedMember.Local TODO what to do with this?
    // ReSharper disable once UnusedParameter.Local TODO what to do with this?
    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
      TrackFatalException(e.ExceptionObject as Exception);
      _tc.Flush();
    }

    /// <summary>
    /// Application Insights exception tracking. This SHOULD send off any unhandled fatal exceptions to AI for investigation.
    /// </summary>
    /// <param name="ex"></param>
    // ReSharper disable once UnusedParameter.Global  TODO what to do with this?
    public void TrackFatalException(Exception ex)
    {
      var exceptionTelemetry = new ExceptionTelemetry(new Exception())
      {
        HandledAt = ExceptionHandledAt.Unhandled
      };
      _tc.TrackException(exceptionTelemetry);
    }

    #endregion ExceptionHandling

    #region Logging

    /// <summary>
    /// Appends text to our status display window.
    /// </summary>
    /// <param name="text"></param>
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

    #endregion Logging
    
    protected virtual void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
    {
      var onPropertyChanged = PropertyChanged;
      onPropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void DoStatusUpdate(object sender, StatusUpdateArgs args)
    {
      Logger.Debug("DSU from Netlogwatcher: " + args.StatusMessage);
      AppendStatus(args.StatusMessage);
    }

    #region JournalEvents

    private Thread heartBeatThread;
    private DateTime lastHullDamageEvent;
    private AssignedRescueViewModel assignedRescueViewModel;

    private void CmdrJournalParser_CommitCrimeEvent(object sender, CommitCrimeLog eventData)
    {
      var rat = MyPlayer.GetDisplayRat();
      if (rat != null)
      {
        var msg = new TpaMessage("CommitCrime")
        {
          Data = new JObject
          (
            new JProperty("CrimeType", eventData.CrimeType),
            new JProperty("RatID", rat.Id),
            new JProperty("CurrentSystem", MyPlayer.CurrentSystem),
            new JProperty("RescueID", AssignedRescueViewModel.Rescue.Id)
          )
        };

        if (eventData.Victim != null)
        {
          msg.Data.Add("Victim", eventData.Victim);
        }

        apiWorker.SendTpaMessage(msg);
      }
    }

    private void CmdrJournalParser_DiedEvent(object sender, DiedLog eventData)
    {
      if (AssignedRescueViewModel?.Rescue != null)
      {
        var rat = MyPlayer.GetDisplayRat();
        if (rat != null)
        {
          apiWorker.SendTpaMessage(new TpaMessage("RatDeath")
          {
            Data = new JObject
            (
              new JProperty("Killers", eventData.KillersList?.Select(k => k.Name).ToList()),
              new JProperty("RatID", rat.Id),
              new JProperty("CurrentSystem", MyPlayer.CurrentSystem),
              new JProperty("RescueID", AssignedRescueViewModel.Rescue.Id)
            )
          });
        AppendStatus("Sending death notice.");
        }
      }
    }

    private void _cmdrJournalParser_EscapeInterdictionEvent(object sender, EscapeInterdictionLog eventData)
    {
      if (AssignedRescueViewModel?.Rescue != null)
      {
        var rat = MyPlayer.User.Rats.FirstOrDefault(x => x.Platform == Platform.Pc);
        if (rat != null)
        {
          apiWorker.SendTpaMessage(new TpaMessage("Interdiction", "Update")
          {
            Data = new JObject
            (
              new JProperty("Interdiction", "Escaped"),
              new JProperty("Interdictor", eventData.Interdictor),
              new JProperty("RatID", rat.Id),
              new JProperty("CurrentSystem", MyPlayer.CurrentSystem),
              new JProperty("RescueID", AssignedRescueViewModel.Rescue.Id)
            )
          });
        AppendStatus("Sending Escape Interdiction Noticiation.");
        }
      }
    }

    private void CmdrJournalParser_FsdJumpEvent(object sender, FsdJumpLog eventData)
    {
      TriggerSystemChange(eventData.StarSystem);
    }

    private void CmdrJournalParser_HullDamageEvent(object sender, HullDamageLog eventData)
    {
      if (AssignedRescueViewModel?.Rescue != null)
      {
        if ((eventData.Timestamp - lastHullDamageEvent).TotalMinutes < 1)
        {
          return;
        }

        lastHullDamageEvent = eventData.Timestamp;

        var rat = MyPlayer.GetDisplayRat();
        if (rat != null)
        {
          apiWorker.SendTpaMessage(new TpaMessage("UnderAttack", "Update")
          {
            Data = new JObject
            (
              new JProperty("UnderAttack", "True"),
              new JProperty("RatHealth", eventData.Health),
              new JProperty("RatID", rat),
              new JProperty("CurrentSystem", MyPlayer.CurrentSystem),
              new JProperty("RescueID", AssignedRescueViewModel.Rescue.Id)
            )
          });
          AppendStatus("Sending Under Attack notification.");
        }
      }
    }

    private void CmdrJournalParser_InterdictedEvent(object sender, InterdictedLog eventData)
    {
      if (AssignedRescueViewModel?.Rescue != null)
      {
        var rat = MyPlayer.GetDisplayRat();
        if (rat != null)
        {
          apiWorker.SendTpaMessage(new TpaMessage("Interdiction", "Update")
          {
            Data = new JObject
            (
              new JProperty("Interdiction", "Interdicted"),
              new JProperty("Interdictor", eventData.Interdictor),
              new JProperty("RatID", rat.Id),
              new JProperty("CurrentSystem", MyPlayer.CurrentSystem),
              new JProperty("RescueID", AssignedRescueViewModel.Rescue.Id)
            )
          });
          AppendStatus("Sending interdicted notification.");
        }
      }
    }

    private void CmdrJournalParser_InterdictionEvent(object sender, InterdictionLog eventData)
    {
      if (AssignedRescueViewModel?.Rescue != null)
      {
        var rat = MyPlayer.GetDisplayRat();
        if (rat != null)
        {
          var msg = new TpaMessage("Interdiction", "Update")
          {
            Data = new JObject
            (
              new JProperty("Interdiction", "Interdicting"),
              new JProperty("Interdicted", eventData.Interdicted),
              new JProperty("RatID", rat.Id),
              new JProperty("CurrentSystem", MyPlayer.CurrentSystem),
              new JProperty("RescueID", AssignedRescueViewModel.Rescue.Id)
            )
          };
          apiWorker.SendTpaMessage(msg);
        }
      }
    }

    private void CmdrJournalParser_ReceiveTextEvent(object sender, ReceiveTextLog eventData)
    {
      if (AssignedRescueViewModel?.Rescue != null)
      {
        // We need to clean the string up for cmdr name comparison. The cmdr name is not consistant, and appears as "CMDR cmdrName" or "&cmdrName" depending on how it was recieved.
        if (!eventData.FromText.Replace("CMDR", "").Replace("&", "").Trim()
          .Equals(AssignedRescueViewModel.Rescue.Client, StringComparison.InvariantCultureIgnoreCase))
        {
          return;
        }
        var rat = MyPlayer.GetDisplayRat();
        if (rat != null)
        {
          apiWorker.SendTpaMessage(new TpaMessage("Communication", "Update")
          {
            Data = new JObject
            (
              new JProperty("Communication", "True"),
              new JProperty("RatID", rat.Id),
              new JProperty("RescueID", AssignedRescueViewModel.Rescue.Id)
            )
          });
          AppendStatus("Sending comms+ confirmation.");

          //TODO add in "Comms+" status and update it here.
        }
      }
    }

    private void CmdrJournalParser_SupercruiseEntryEvent(object sender, SupercruiseEntryLog eventData)
    {
      if (AssignedRescueViewModel?.Rescue != null)
      {
        var rat = MyPlayer.GetDisplayRat();
        if (rat != null)
        {
          apiWorker.SendTpaMessage(new TpaMessage("Supercruise", "Update")
          {
            Data = new JObject
            (
              new JProperty("Supercruise", "Entering"),
              new JProperty("RatID", rat.Id),
              new JProperty("RescueID", AssignedRescueViewModel.Rescue.Id)
            )
          });
          AppendStatus("Sending supercrise entry notification.");
        }
      }
    }

    private void CmdrJournalParser_SuperCruiseExitEvent(object sender, SuperCruiseExitLog eventData)
    {
      if (AssignedRescueViewModel?.Rescue != null)
      {
        var rat = MyPlayer.GetDisplayRat();
        if (rat != null)
        {
          apiWorker.SendTpaMessage(new TpaMessage("Supercruise", "Update")
          {
            Data = new JObject
            (
              new JProperty("Supercruise", "Exiting"),
              new JProperty("RatID", rat.Id),
              new JProperty("RescueID", AssignedRescueViewModel.Rescue.Id)
            )
          });
          AppendStatus("Sending supercruise exit noticiation.");
        }
      }
    }

    private void CmdrJournalParser_WingAddEvent(object sender, WingAddLog eventData)
    {
      if (AssignedRescueViewModel?.Rescue != null)
      {
        if (!AssignedRescueViewModel.Rescue.Client.Equals(eventData.Name, StringComparison.InvariantCultureIgnoreCase))
        {
          return;
        }
        var rat = MyPlayer.GetDisplayRat();
        if (rat != null)
        {
          apiWorker.SendTpaMessage(new TpaMessage("WingRequest", "Update")
          {
            Data = new JObject
            (
              new JProperty("WingRequest", "True"),
              new JProperty("RatID", rat.Id),
              new JProperty("RescueID", AssignedRescueViewModel.Rescue.Id)
            )
          });
          AppendStatus("Sending Wing Request acknowledgement.");
          AssignedRescueViewModel.Self.WingRequest = RequestState.Accepted;
        }
      }
    }
    
    private async void TriggerSystemChange(string value)
    {
      var disp = Dispatcher;
      if (value == MyPlayer.CurrentSystem)
      {
        return; // Already know we're in that system, thanks.
      }

      MyPlayer.CurrentSystem = value;
      try
      {
        _tc.TrackEvent("SystemChange");
        IEnumerable<EdsmSystem> m = await QueryEdsmSystem(value);
        var firstsys = m.FirstOrDefault();
        if (firstsys != null && firstsys.Name == value)
        {
          if (firstsys.Coords == default(Coordinates))
          {
            Logger.Debug("Got a match on " + firstsys.Name + ", but it has no coords.");
          }
        }
        else
        {
          Logger.Debug("Got definite match in first pos, disregarding extra hits:" + firstsys.Name + " X:" +
                       firstsys.Coords.X + " Y:" + firstsys.Coords.Y + " Z:" + firstsys.Coords.Z);
        }
        if (_myTravelLog == null)
        {
          _myTravelLog = new Collection<TravelLog>();
        }

        _myTravelLog.Add(new TravelLog { System = firstsys, LastVisited = DateTime.Now });
        await
          disp.BeginInvoke(DispatcherPriority.Normal,
            (Action)(() => SystemNameLabel.Foreground = Brushes.Green));
        Logger.Debug("Getting distance from fuelum to " + firstsys.Name);
        var distance = await CalculateEdsmDistance("Fuelum", firstsys.Name);
        distance = Math.Round(distance, 2);
        await
          disp.BeginInvoke(DispatcherPriority.Normal,
            (Action)(() => DistanceLabel.Content = distance + "LY from Fuelum"));
        Logger.Debug("Added system to TravelLog.");
        if (AssignedRescueViewModel.Rescue?.System == value)
        { var rat = MyPlayer.GetDisplayRat();
          if (rat != null)
          {
            AppendStatus("Arrived in client system. Notifying dispatch.");
            Logger.Info("Sending 3PA sys+ message!");
            var sysmsg = new TpaMessage("SysArrived", "update")
            {
              Data = new JObject
              (
                new JProperty("SysArrived", "true"),
                new JProperty("RatID", rat.Id),
                new JProperty("RescueID", AssignedRescueViewModel.Rescue.Id)
              )
            };
            apiWorker.SendTpaMessage(sysmsg);
          }
          AssignedRescueViewModel.Self.InSystem = true;
        }

        await disp.BeginInvoke(DispatcherPriority.Normal, (Action)(() => SystemNameLabel.Content = firstsys.Name));
        if (firstsys.Coords == default(Coordinates))
        {
          await
            disp.BeginInvoke(DispatcherPriority.Normal,
              (Action)(() => SystemNameLabel.Foreground = Brushes.Red));
        }
        else
        {
          await
            disp.BeginInvoke(DispatcherPriority.Normal,
              (Action)(() => SystemNameLabel.Foreground = Brushes.Orange));
        }
      }
      catch (Exception ex)
      {
        Logger.Fatal("Exception in triggerSystemChange: " + ex.Message);
        _tc.TrackException(ex);
      }
    }


    #endregion JournalEvents

    #region Heartbeat

    private void TerminateHeartBeat()
    {
      Logger.Debug("Terminating Heartbeat.");
      _heartbeatStopping = true;
      //other logic here? if not, we can just set the bool to true to terminate it.
    }

    private void InitHeartBeat()
    {
      Logger.Debug("Starting Heartbeat.");
      heartBeatThread = new Thread(HeartBeat) {Name = "HeartBeatThread"};
      heartBeatThread.Start();
    }

    private void HeartBeat() // just give it a thread and let it do it's thing.
    {
      _heartbeatStopping = false;
      while (!_heartbeatStopping)
      {
        //Logger.Debug("Heartbeat..."); //Let's not do this EVERY heartbeat...
        Thread.Sleep(2000);
        GlobalHeartbeatEvent?.Invoke(this, new EventArgs());
      }
      Logger.Debug("Heartbeat stopped.");
    }

    #endregion Heartbeat
    
    private void MainWindow_Closing(object sender, CancelEventArgs e)
    {
      StopNetLog = true;
      apiWorker?.DisconnectWs();
      _tc?.Flush();
      TerminateHeartBeat();
      Thread.Sleep(1000); // TODO KA WTF?
      Application.Current.Shutdown();
    }

    private void DutyButton_Click(object sender, RoutedEventArgs e)
    {
      if (MyPlayer.OnDuty == false)
      {
        Button.Content = "On Duty";
        MyPlayer.OnDuty = true;
        var mainwin = this;
        _netlogparser.StartWatcher(ref mainwin);
      }
      else
      {
        Button.Content = "Off Duty";
        MyPlayer.OnDuty = false;
        _netlogparser.StopWatcher();
      }
      try
      {
        var rat = MyPlayer.GetDisplayRat();
        if (rat != null)
        {
          var dutymessage = new TpaMessage("OnDuty", "update")
          {
            Data = new JObject
            (
              new JProperty("OnDuty", MyPlayer.OnDuty.ToString()),
              new JProperty("RatID", rat.Id),
              new JProperty("currentSystem", MyPlayer.CurrentSystem)
            )
          };
          // apiWorker.SendTpaMessage(dutymessage); // Disabled while testing, it's spammy.
        }
      }
      catch (Exception ex)
      {
        Logger.Fatal("Exception in sendTPAMessage: " + ex.Message);
        _tc.TrackException(ex);
      }
    }

    private void Main_Menu_Click(object sender, RoutedEventArgs e)
    {
      /* Fleh? */
    }

    private void currentButton_Click(object sender, RoutedEventArgs e)
    {
      AppendStatus("Setting client location to current system: " + MyPlayer.CurrentSystem);
      // SystemName.Text = "Fuelum";
      // TODO: Do actual system name update through Mecha with 3PAM
    }

    private void UpdateButton_Click(object sender, RoutedEventArgs e)
    {
      if (AssignedRescueViewModel == null)
      {
        Logger.Debug("No current rescue, ignoring system update request.");
        return;
      }
      var rat = MyPlayer.GetDisplayRat();
      if (rat != null)
      {
        var systemmessage = new TpaMessage("ClientSystem", "update")
        {
          Data = new JObject
          (
            new JProperty("SystemName", SystemName.Text),
            new JProperty("RatID", rat.Id),
            new JProperty("RescueID", AssignedRescueViewModel.Rescue.Id)
          )
        };
        apiWorker.SendTpaMessage(systemmessage);
      }
    }
    
    // TODO MA KA WTF?
    //private async void RescueGrid_SelectionChanged(object sender, EventArgs e)
    //{
    //  if (RescueGrid.SelectedItem == null)
    //  {
    //    return;
    //  }
    //  /* The shit I do for you, Marenthyu. Update the grid to show the selection, reset labels and
			 //* manually redraw one frame before the thread goes into background work. Yeesh. :P
			 //*/
    //  var myrow = (Datum) RescueGrid.SelectedItem;

    //  var rats = Rats.Where(r => myrow.Rats.Contains(r.Key)).Select(r => r.Value.CmdrName).ToList();
    //  var count = rats.Count;

    //  /* TODO: Fix this. Needs to be smrt about figuring out if your own ratID is assigned. */
    //  AssignedRescue = new RescueInfo {Rescue = myrow};
    //  if (count > 0)
    //  {
    //    AssignedRescue.Self.Rat =
    //      rats[0]; // TODO: Fix this. We have no guarantee that the first listed rat in the rescue is ourself.
    //  }
    //  if (count > 1)
    //  {
    //    AssignedRescue.Rat2.Rat = rats[1];
    //  }
    //  if (count > 2)
    //  {
    //    AssignedRescue.Rat3.Rat = rats[2];
    //  }

    //  var disp = Dispatcher;
    //  await disp.BeginInvoke(DispatcherPriority.Normal, (Action) (() => ClientName.Text = myrow.Client));
    //  //ClientName.Text = myrow.Client;

    //  AssignedRats = myrow.Rats.Any()
    //    ? string.Join(", ", rats)
    //    : string.Empty;
    //  await disp.BeginInvoke(DispatcherPriority.Normal, (Action) (() => SystemName.Text = myrow.System));
    //  DistanceToClient = -1;
    //  DistanceToClientString = "Calculating...";
    //  JumpsToClient = "Calculating...";
    //  var frame = new DispatcherFrame();
    //  await Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(
    //    delegate
    //    {
    //      frame.Continue = false;
    //      return null;
    //    }), null);
    //  Dispatcher.PushFrame(frame);
    //  await RecalculateJumps(myrow.System);
    //}

    private async Task RecalculateJumps(string system)
    {
      try
      {
        var distance = await GetDistanceToClient(system);
        DistanceToClient = Math.Round(distance.Distance, 2);
        Logger.Debug("Setting JTC based on jump distance " + MyPlayer.JumpRange + ": " +
                     Math.Ceiling(distance.Distance / MyPlayer.JumpRange).ToString(CultureInfo.InvariantCulture));
        JumpsToClient = Math.Ceiling(distance.Distance / MyPlayer.JumpRange).ToString(CultureInfo.InvariantCulture);
      }
      catch (Exception ex)
      {
        Logger.Fatal("Exception in RecalculateJumps: " + ex.Message);
      }
    }
    
    private void StartButton_Click(object sender, RoutedEventArgs e)
    {
      AppendStatus("Starting case: " + ClientName.Text);
    }

    private void SettingsMenuItem_Click(object sender, RoutedEventArgs e)
    {
      var swindow = new wndSettings();
      var result = swindow.ShowDialog();
      if (result == true)
      {
        AppendStatus("Reinitializing application due to configuration change...");
        Reinitialize(true);
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

    /* Used by CalculatedEDSMDistance to sort candidate lists by closest lexical match.
		*/
    private int LexicalOrder(string query, string name)
    {
      return name == query ? -1 : (name.Contains(query) ? 0 : 1);
    }
    
    private void OverlayMenu_Click(object sender, RoutedEventArgs e)
    {
      if (_overlay == null)
      {
        _overlay = new Overlay();
        // TODO MA _overlay.SetCurrentClient(AssignedRescueViewModel);
        _overlay.Show();
        var monitors = Monitor.AllMonitors;
        if (Settings.Default.OverlayMonitor != "")
        {
          Logger.Debug("Overlaymonitor is" + Settings.Default.OverlayMonitor);
          foreach (var mymonitor in monitors)
          {
            if (mymonitor.Name == Settings.Default.OverlayMonitor)
            {
              _overlay.Left = mymonitor.Bounds.Right - _overlay.Width - 50;
              _overlay.Top = mymonitor.Bounds.Top;
              _overlay.Topmost = true;
              Logger.Debug("Overlay coordinates set to " + _overlay.Left + " x " + _overlay.Top);
              try
              {
                var hotKeyHost =
                  new HotKeyHost((HwndSource) PresentationSource.FromVisual(Application.Current.MainWindow));
                hotKeyHost.AddHotKey(new CustomHotKey("ToggleOverlay", Key.O, ModifierKeys.Control | ModifierKeys.Alt,
                  true));
                hotKeyHost.AddHotKey(new CustomHotKey("CopyClientSystemname", Key.C,
                  ModifierKeys.Control | ModifierKeys.Alt, true));
                hotKeyHost.AddHotKey(new CustomHotKey("CallJumpsToIRC", Key.J, ModifierKeys.Control | ModifierKeys.Alt,
                  true));
                hotKeyHost.HotKeyPressed += HandleHotkeyPress;
              }
              catch (Exception ex)
              {
                Logger.Debug("Exception while installing hotkeys: " + ex.Message + "@" + ex.Source);
              }
              // TODO: Broken all of a sudden? May require recode.
            }
          }
        }
        else
        {
          foreach (var mymonitor in monitors)
          {
            Logger.Debug("Monitor ID: " + mymonitor.Name);
            if (mymonitor.IsPrimary)
            {
              _overlay.Left = mymonitor.Bounds.Right - _overlay.Width - 50;
              _overlay.Top = mymonitor.Bounds.Top;
            }
          }
          _overlay.Topmost = true;
          try
          {
            var hotKeyHost =
              new HotKeyHost((HwndSource) PresentationSource.FromVisual(Application.Current.MainWindow));
            hotKeyHost.AddHotKey(new CustomHotKey("ToggleOverlay", Key.O, ModifierKeys.Control | ModifierKeys.Alt,
              true));
            hotKeyHost.AddHotKey(new CustomHotKey("CopyClientSystemname", Key.C,
              ModifierKeys.Control | ModifierKeys.Alt, true));
            hotKeyHost.AddHotKey(new CustomHotKey("CallJumpsToIRC", Key.J, ModifierKeys.Control | ModifierKeys.Alt,
              true));
            hotKeyHost.HotKeyPressed += HandleHotkeyPress;
          }
          catch (Exception ex)
          {
            Logger.Debug("Exception while installing hotkeys: " + ex.Message + "@" + ex.Source);
          }
        }
      }
      else
      {
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
        if (AssignedRescueViewModel.SystemName != null)
        {
          Clipboard.SetText(AssignedRescueViewModel.SystemName);
          AppendStatus("Client system copied to clipboard.");
        }
        else
        {
          AppendStatus("No active rescue, copy to clipboard aborted.");
        }
      }
      if (e.HotKey.Key == Key.J)
      {
        AppendStatus("Sending jump count to IRC due to hotkey press.");
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
    
    private RatState GetRatStateForButton(object sender, Button selfButton, Button rat2Button, Button rat3Button)
    {
      RatState ratState;
      if (Equals(sender, selfButton))
      {
        ratState = AssignedRescueViewModel.Self;
      }
      else if (Equals(sender, rat2Button))
      {
        ratState = AssignedRescueViewModel.Rat2;
      }
      else if (Equals(sender, rat3Button))
      {
        ratState = AssignedRescueViewModel.Rat3;
      }
      else
      {
        ratState = AssignedRescueViewModel.Self;
      }

      return ratState;
    }

    private void fueledButton_Click(object sender, RoutedEventArgs e)
    { var rat = MyPlayer.GetDisplayRat();
      if (rat != null)
      {
        if (AssignedRescueViewModel.Rescue == null)
        {
          Logger.Debug("Null rescue or RatID, not doin' nothing.");
          return;
        }

        var fuelmsg = new TpaMessage("fueled:update") {Data = new JObject()};
        fuelmsg.Data.Add("RatID", rat.Id);
        fuelmsg.Data.Add("RescueID", AssignedRescueViewModel.Rescue.Id);

        if (Equals(FueledButton.Background, Brushes.Red))
        {
          AppendStatus("Reporting fueled status, requesting paperwork link...");
          FueledButton.Background = Brushes.Green;
          fuelmsg.Data.Add("Fueled", "true");
          /* image.Source = new BitmapImage(RatTracker_WPF.Properties.Resources.yellow_light); */
        }
        else
        {
          AppendStatus("Fueled status now negative.");
          FueledButton.Background = Brushes.Red;
          fuelmsg.Data.Add("Fueled", "false");
        }

        apiWorker.SendTpaMessage(fuelmsg);
      }
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
      apiWorker.QueryRescues();
    }

    private async void button1_Click(object sender, RoutedEventArgs e)
    {
      Logger.Debug("Begin TPA Jump Call...");
      //assignedRescue = (Datum) RescueGrid.SelectedItem;
      if (AssignedRescueViewModel == null) // TODO: Major cleanup in this null testing.
      {
        AppendStatus("Null myrescue! Failing.");
        return;
      }
      if (AssignedRescueViewModel != null && AssignedRescueViewModel.Rescue.Id == null)
      {
        Logger.Debug("Rescue ID is null!");
        return;
      }
      if (AssignedRescueViewModel.Rescue.Client == null)
      {
        AppendStatus("Null client.");
        return;
      }

      if (AssignedRescueViewModel.Rescue.System == null)
      {
        AppendStatus("Null system.");
        return;
      }

      var rat = MyPlayer.GetDisplayRat();
      if (rat == null)
      {
        Logger.Debug(
          "I have no ratID for myself! That's bad, can't carry on..."); // Enforce a check for who we really are.
        return;
      }
      Logger.Debug("Null tests completed");
      AppendStatus("Tracking rescue. System: " + AssignedRescueViewModel.Rescue.System + " Client: " + AssignedRescueViewModel.Rescue.Client);
      // TODO MA fix
      //AssignedRescueViewModel = new RescueInfo
      //{
      //  ClientName = AssignedRescueViewModel.Rescue.Client,
      //  Rescue = AssignedRescueViewModel.Rescue,
      //  ClientSystem = AssignedRescueViewModel.Rescue.System
      //};
      if (AssignedRescueViewModel.Rescue.Rats != null)
      {
        Logger.Debug("Non-null myrescue rats. Parsing");
        var trackedrats = 0;
        foreach (var ratid in AssignedRescueViewModel.Rescue.Rats)
        {
          Logger.Debug("Processing id " + ratid);
          //if (MyPlayer.RatId.Contains(ratid))
          //{
          //  Logger.Debug("Found own rat in selected rescue, we're assigned");
          //}
          //else
          {
            if (trackedrats > 1)
            {
              Logger.Debug("More than two rats in addition to ourselves, not added.");
            }
            else if (trackedrats == 0)
            {
              Logger.Debug("Rat2 set.");
            }
            else
            {
              Logger.Debug("Rat3 set.");
            }
          }
        }
      }
      Logger.Debug("Client info loaded:" + AssignedRescueViewModel.ClientName + " in " + AssignedRescueViewModel.SystemName);
      // TODO MA _overlay?.SetCurrentClient(AssignedRescueViewModel);
      var distance = await GetDistanceToClient(AssignedRescueViewModel.SystemName);
      //ClientDistance distance = new ClientDistance {Distance = 500};
      AppendStatus("Sending jumps to IRC...");
      Logger.Debug("Constructing TPA message...");
      var jumpmessage = new TpaMessage("CallJumps","update");
      Logger.Debug("Setting action.");
      jumpmessage.Id = "0xDEADBEEF";
      Logger.Debug("Set appID");
      Logger.Debug("Constructing TPA for " + AssignedRescueViewModel.Rescue.Id + " with " + rat.Id);
      jumpmessage.Data = new JObject(
        new JProperty("CallJumps",
          Math.Ceiling(distance.Distance / MyPlayer.JumpRange).ToString(CultureInfo.InvariantCulture)),
        new JProperty("RescueID", AssignedRescueViewModel.Rescue.Id),
        new JProperty("RatID", rat.Id),
        new JProperty("Lightyears", distance.Distance.ToString(CultureInfo.InvariantCulture)),
        new JProperty("SourceCertainty", distance.SourceCertainty),
        new JProperty("DestinationCertainty", distance.TargetCertainty)
      );
      Logger.Debug("Sending TPA message");
      apiWorker.SendTpaMessage(jumpmessage);
    }

    private async void Button2_Click(object sender, RoutedEventArgs e)
    {
      Logger.Debug("Querying EDDB for closest station to " + MyPlayer.CurrentSystem);
      IEnumerable<EdsmSystem> mysys = await QueryEdsmSystem(MyPlayer.CurrentSystem);
      var edsmSystems = mysys.ToArray();
      if (edsmSystems.Any())
      {
        Logger.Debug("Got a mysys with " + edsmSystems.Count() + " elements");
        var station = Eddbworker.GetClosestStation(edsmSystems.First().Coords);
        var system = Eddbworker.GetSystemById(station.system_id);
        AppendStatus("Closest populated system to '" + MyPlayer.CurrentSystem + "' is '" + system.name +
                     "', closest station to star with known coordinates is '" + station.name + "'.");
        var distance = await CalculateEdsmDistance(MyPlayer.CurrentSystem, edsmSystems.First().Name);
        var mymessage = new OverlayMessage
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

      AppendStatus("Unable to find a candidate system for location " + AssignedRescueViewModel.SystemName);
    }

    private void ErrorReportClick(object sender, RoutedEventArgs e)
    {
      var errwnd = new ErrorReporter();
      var result = errwnd.ShowDialog();
      if (result == true)
      {
        AppendStatus("Application bug report sent.");
      }
    }

    #region EDSM

    public async Task<List<EdsmSystem>> QueryEdsmSystem(string system)
    {
      Logger.Debug("Querying EDSM (or rather SQL) for system " + system);
      AppendStatus("Querying database for " + system);
      var m = await _fbworker.GetSystemAsEdsm(system);
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
      try
      {
        var sysmatch = "([A-Z][A-Z]-[A-z]+) ([a-zA-Z])+(\\d+(?:-\\d+)+?)";
        var mymatch = Regex.Match(target, sysmatch, RegexOptions.IgnoreCase);
        IEnumerable<EdsmSystem> candidates =
          await QueryEdsmSystem(target.Substring(0, target.IndexOf(mymatch.Groups[3].Value, StringComparison.Ordinal)));
        Logger.Debug("Candidate count is " + candidates.Count() + " from a subgroup of " + mymatch.Groups[3].Value);
        _tc.TrackMetric("CandidateCount", candidates.Count());
        var finalcandidates = candidates.Where(x => x.Coords != null).ToList();
        Logger.Debug("FinalCandidates with coords only is size " + finalcandidates.Count);
        if (!finalcandidates.Any())
        {
          Logger.Debug("No final candidates, widening search further...");
          candidates =
            await QueryEdsmSystem(
              target.Substring(0, target.IndexOf(mymatch.Groups[2].Value, StringComparison.Ordinal)));
          finalcandidates = candidates.Where(x => x.Coords != null).ToList();
          if (!finalcandidates.Any())
          {
            Logger.Debug("Still nothing! Querying whole sector.");
            candidates =
              await QueryEdsmSystem(target.Substring(0,
                target.IndexOf(mymatch.Groups[1].Value, StringComparison.Ordinal)));
            finalcandidates = candidates.Where(x => x.Coords != null).ToList();
          }
        }
        Logger.Debug("Final count before return from GetCandidateSystems is " + finalcandidates.Count());
        return finalcandidates;
      }
      catch (Exception ex)
      {
        Logger.Fatal("Exception in GetCandidateSystems: " + ex.Message);
        _tc.TrackException(ex);
        return new List<EdsmSystem>();
      }
    }

    public async Task<ClientDistance> GetDistanceToClient(string target)
    {
      var logdepth = 0;
      var targetcoords = new Coordinates();
      var cd = new ClientDistance();
      var sourcecoords = FuelumCoords;
      var sourceSystem = new EdsmSystem();
      sourceSystem.Name = "Fuelum";
      sourceSystem.Coords = FuelumCoords;
      cd.SourceCertainty = "Fuelum";
      if (_myTravelLog != null)
      {
        foreach (var mysource in _myTravelLog.Reverse())
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
            cd.SourceCertainty = logdepth.ToString();
            sourceSystem = mysource.System;
            break;
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
      var firstOrDefault = candidates.FirstOrDefault();
      if (firstOrDefault != null && firstOrDefault.Coords == null)
      {
        Logger.Debug("Known system '" + target + "', but no coords. Widening search...");
        candidates = await GetCandidateSystems(target);
        cd.TargetCertainty = "Region";
      }
      var edsmSystems = candidates as EdsmSystem[] ?? candidates.ToArray();
      if (!edsmSystems.Any())
      {
        //Still couldn't find something, abort.
        AppendStatus("Couldn't find a candidate system, aborting...");
        return new ClientDistance();
      }

      Logger.Debug("We have two sets of coords that we can use to find a distance.");
      var edsmSystem = edsmSystems.FirstOrDefault();
      if (edsmSystem != null)
      {
        targetcoords = edsmSystem.Coords;
      }
      if (sourceSystem?.Name == null)
      {
        Logger.Debug("Err... Source system (or its name) is null, that shouldn't happen at this point. Bailing!");
        return new ClientDistance();
      }
      if (edsmSystem != null)
      {
        Logger.Debug("Finding from coords: " + sourcecoords.X + " " + sourcecoords.Y + " " + sourcecoords.Z + " (" +
                     sourceSystem.Name + ") to " + targetcoords.X + " " + targetcoords.Y + " " + targetcoords.Z + " (" +
                     edsmSystem.Name + ")");
        var deltaX = sourcecoords.X - targetcoords.X;
        var deltaY = sourcecoords.Y - targetcoords.Y;
        var deltaZ = sourcecoords.Z - targetcoords.Z;
        var distance = Math.Sqrt(deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ);
        Logger.Debug("Distance should be " + distance);
        cd.Distance = distance;
      }
      else
      {
        Logger.Debug("System was null in GetDistanceToClient. Returning blank.");
        return new ClientDistance();
      }
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
      try
      {
        var sourcecoords = new Coordinates();
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
        var edsmSystems = candidates.ToArray();
        if (!edsmSystems.Any())
        {
          Logger.Debug("EDSM does not know system '" + target + "'. Widening search...");
          candidates = await GetCandidateSystems(target);
        }

        var firstOrDefault = edsmSystems.FirstOrDefault();
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

        Logger.Debug("I got " + edsmSystems.Count() +
                     " systems with coordinates. Sorting by lexical match and picking first.");
        var sorted = edsmSystems.OrderBy(s => LexicalOrder(target, s.Name));
        var edsmSystem = sorted.FirstOrDefault();
        var targetcoords = edsmSystem?.Coords;

        if (targetcoords != null)
        {
          Logger.Debug("We have two sets of coords that we can use to find a distance.");
          var deltaX = sourcecoords.X - targetcoords.X;
          var deltaY = sourcecoords.Y - targetcoords.Y;
          var deltaZ = sourcecoords.Z - targetcoords.Z;
          var distance = Math.Sqrt(deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ);
          Logger.Debug("Distance should be " + distance);
          return distance;
        }

        AppendStatus("EDSM failed to find coords for system '" + target + "'.");
        return -1;
      }
      catch (Exception ex)
      {
        Logger.Fatal("Exception in CalculateEdsmDistance: " + ex.Message);
        _tc.TrackException(ex);
        return -1;
      }
    }

    #endregion
  }
}