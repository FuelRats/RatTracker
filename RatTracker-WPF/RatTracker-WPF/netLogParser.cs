using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using System.Xml;
using System.Xml.Linq;
using log4net;
using Microsoft.ApplicationInsights;
using RatTracker_WPF.Models.Api;
using RatTracker_WPF.Models.App;
using RatTracker_WPF.EventHandlers;
using RatTracker_WPF.Models.Edsm;
using RatTracker_WPF.Models.NetLog;
using RatTracker_WPF.Properties;

namespace RatTracker_WPF
{
    public delegate void StatusUpdateEvent(object sender, StatusUpdateArgs args);
    public delegate void FriendRequestUpdateEvent(object sender, FriendRequestArgs args);
    public delegate void InstanceChangeUpdateEvent(object sender, InstanceChangeArgs args);
    public delegate void SystemChangeUpdateEvent(object sender, SystemChangeArgs args);
    public delegate void ConnInfoUpdateEvent(object sender, ConnInfoArgs args);

    public class NetLogParser 
    {
        public event StatusUpdateEvent StatusUpdateEvent;
        public event FriendRequestUpdateEvent FriendRequestUpdateEvent;
        public event InstanceChangeUpdateEvent InstanceChangeUpdateEvent;
        public event SystemChangeUpdateEvent SystemChangeUpdateEvent;
        public event ConnInfoUpdateEvent ConnInfoUpdateEvent;

        private string _scState;
        private string _xmlparselist;
        private static readonly ILog Logger = LogManager.GetLogger(System.Reflection.Assembly.GetCallingAssembly().GetName().Name);
        private readonly TelemetryClient _tc = new TelemetryClient();
        private long _fileOffset;
        private long _fileSize; // Size of current netlog file
        private FileInfo _logFile; // Pointed to the live netLog file.
        private FileSystemWatcher _watcher;
        private ConnectionInfo _conninfo = new ConnectionInfo();
        private string _conntype;
        private Thread _threadLogWatcher;
        private MainWindow _mainwindowref;

        public static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                .Where(x => x % 2 == 0)
                .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                .ToArray();
        }

        public FileSystemWatcher Watcher
        {
            get { return _watcher; }
            set { _watcher = value;  }
        }

        public void AppendStatus(string message)
        {
            StatusUpdateEvent?.Invoke(this, new StatusUpdateArgs() {StatusMessage= message});
        }

        public void TriggerSystemChange(string sysname, EdsmCoords coords)
        {
            SystemChangeUpdateEvent?.Invoke(this, new SystemChangeArgs() {SystemName = sysname, Coords = coords});
        }

        public void TriggerInstanceChange(string island, List<string> instancemembers, string systemname)
        {
            InstanceChangeUpdateEvent?.Invoke(this,
                new InstanceChangeArgs() {IslandName = island, IslandMembers = instancemembers, SystemName = systemname});
        }
        public void TriggerConnInfoUpdate(ConnectionInfo conninfo)
        {
            ConnInfoUpdateEvent?.Invoke(this,
                new ConnInfoArgs() {ConnInfo = conninfo});
        }
        public void CheckLogDirectory()
        {
            Logger.Debug("Checking log directories.");
            try
            {
                if (Thread.CurrentThread.Name == null)
                {
                    Thread.CurrentThread.Name = "NetLogParser";
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
                //_myTravelLog = new Collection<TravelLog>();
            }
            catch (Exception ex)
            {
                Logger.Debug("Exception in CheckLogDirectory! " + ex.Message);
                _tc.TrackException(ex);
            }
        }
        private void OnChanged(object source, FileSystemEventArgs e)
        {
            _logFile = new FileInfo(e.FullPath);
            /* Handle changed events */
        }
        private void OnRenamed(object source, RenamedEventArgs e)
        {
            /* Stop watching the renamed file, look for new onChanged. */
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
                            _conninfo.RunId = line.Substring(line.IndexOf("is ", StringComparison.Ordinal));
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
                                _conninfo.WanAddress = match.Groups[1].Value + ":" + match.Groups[2].Value;
                                _conninfo.Mtu = int.Parse(match.Groups[12].Value);
                                _tc.TrackMetric("Rat_Detected_MTU", _conninfo.Mtu);
                                _conninfo.NatType = (NatType)Enum.Parse(typeof(NatType), match.Groups[9].Value);
                                if (match.Groups[2].Value == match.Groups[4].Value && match.Groups[10].Value == "0")
                                {
                                    Logger.Debug("Probably using static portmapping, source and destination port matches and uPnP disabled.");
                                    _conninfo.PortMapped = true;
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
                                _conninfo.TurnServer = match.Groups[7].Value + ":" + match.Groups[8].Value;
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
                    switch (_conninfo.NatType)
                    {
                        case NatType.Blocked:
                            AppendStatus(
                                "WARNING: E:D reports that your network port appears to be blocked! This will prevent you from instancing with other players!");
                            _conntype = "Blocked!";
                            _tc.TrackMetric("NATBlocked", 1);
                            break;
                        case NatType.Unknown:
                            if (_conninfo.PortMapped != true)
                            {
                                AppendStatus(
                                    "WARNING: E:D is unable to determine the status of your network port. This may be indicative of a condition that may cause instancing problems!");
                                _conntype = "Unknown";
                                _tc.TrackMetric("NATUnknown", 1);
                            }
                            else
                            {
                                AppendStatus("Unable to determine NAT type, but you seem to have a statically mapped port forward.");
                                _tc.TrackMetric("ManualPortMap", 1);
                            }
                            break;
                        case NatType.Open:
                            _conntype = "Open";
                            _tc.TrackMetric("NATOpen", 1);
                            break;
                        case NatType.FullCone:
                            _conntype = "Full cone NAT";
                            _tc.TrackMetric("NATFullCone", 1);
                            break;
                        case NatType.Failed:
                            AppendStatus("WARNING: E:D failed to detect your NAT type. This might be problematic for instancing.");
                            _conntype = "Failed to detect!";
                            _tc.TrackMetric("NATFailed", 1);
                            break;
                        case NatType.SymmetricUdp:
                            if (_conninfo.PortMapped != true)
                            {
                                AppendStatus(
                                    "WARNING: Symmetric NAT detected! Although your NAT allows UDP, this may cause SEVERE problems when instancing!");
                                _conntype = "Symmetric UDP";
                                _tc.TrackMetric("NATSymmetricUDP", 1);
                            }

                            else
                            {
                                AppendStatus("Symmetric UDP NAT with static port mapping detected.");
                                _tc.TrackMetric("ManualPortMap", 1);
                            }

                            break;
                        case NatType.Restricted:
                            if (_conninfo.PortMapped != true)
                            {
                                AppendStatus("WARNING: Port restricted NAT detected. This may cause instancing problems!");
                                _conntype = "Port restricted NAT";
                                _tc.TrackMetric("NATRestricted", 1);
                            }
                            else
                            {
                                AppendStatus("Port restricted NAT with static port mapping detected.");
                                _tc.TrackMetric("ManualPortMap", 1);
                            }
                            break;
                        case NatType.Symmetric:
                            if (_conninfo.PortMapped != true)
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

        public void NetLogWatcher()
        {
            AppendStatus("From NetLogParser - Netlogwatcher started.");
            try
            {
                _mainwindowref.GlobalHeartbeatEvent += RunTick;
            }
            catch (Exception ex)
            {
                Logger.Debug("Netlog exception: " + ex.Message);
                _tc.TrackException(ex);
            }
        }

        public void RunTick(object sender, EventArgs args)
        {
            Logger.Debug("Running tick!");
            FileInfo fi = new FileInfo(_logFile.FullName);
            if (fi.Length == _fileSize) return;
            ReadLogfile(fi.FullName); /* Maybe a poke on the FS is enough to wake watcher? */
            _fileOffset = fi.Length;
            _fileSize = fi.Length;

        }
        public void StartWatcher(ref MainWindow mainwindow)
        {
            if (_watcher==null)
            {
                Logger.Fatal("No watcher in NetLogWatcher StartWatcher!");
                return;
            }
            _mainwindowref = mainwindow;
            _watcher.EnableRaisingEvents = true;
            AppendStatus("Started watching for events in netlog.");
           // Button.Background = Brushes.Green;
            StopNetLog = false;
            _threadLogWatcher = new Thread(NetLogWatcher) { Name = "Netlog watcher" };
            _threadLogWatcher.Start();
        }

        public void StopWatcher()
        {
            _watcher.EnableRaisingEvents = false;
            AppendStatus("\nStopped watching for events in netlog.");
            //Button.Background = Brushes.Red;
            StopNetLog = true;
        }
        public bool StopNetLog { get; set; }


        public void ReadLogfile(string lf)
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
                Logger.Debug("StackTrace: " + ex.StackTrace);
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
                    //TriggerSystemChange(match.Groups[2].Value);
                }
                const string reMatchPlayer = "\\{.+\\} (\\d+) x (\\d+).*\\(\\(([0-9.]+):\\d+\\)\\)Name (.+)$";
                Match frmatch = Regex.Match(line, reMatchPlayer, RegexOptions.IgnoreCase);
                if (frmatch.Success)
                {
                    Logger.Debug("PlayerMatch parsed");

                    AppendStatus("Successful identity match! ID: " + frmatch.Groups[1] + " IP:" + frmatch.Groups[3]);
                }

                const string reMatchNat = @"RxRoute:(\d+)+ Comp:(\d)\[IP4NAT:(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})+:(\d{1,5}),(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})+:(\d{1,5}),(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})+:(\d{1,5}),(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})+:(\d{1,5}),(\d),(\d),(\d),(\d{1,4})\]\[Relay:";
                Match natmatch = Regex.Match(line, reMatchNat, RegexOptions.IgnoreCase);
                if (natmatch.Success)
                {
                    Logger.Debug("Found NAT datapoint for runID " + natmatch.Groups[1] + ": " + natmatch.Groups[11]);
                    NatType clientnat = (NatType)Enum.Parse(typeof(NatType), match.Groups[11].Value);
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
                   _conninfo.Srtt = int.Parse(statmatch.Groups[5].Value);

                    _conninfo.Loss = float.Parse(statmatch.Groups[6].Value);
                    _conninfo.Jitter = float.Parse(statmatch.Groups[7].Value);
                    _conninfo.Act1 = float.Parse(statmatch.Groups[8].Value);
                    _conninfo.Act2 = float.Parse(statmatch.Groups[9].Value);

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
            }
            catch (Exception ex)
            {
                Logger.Debug("Exception in ParseLine: " + ex.Message + "@" + ex.Source + ":" + ex.Data);
                _tc.TrackException(ex);
            }
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
                            /*
>>>>>>> master
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
<<<<<<< HEAD
=======
                            */
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
                    //MyClient.Self.FriendRequest = RequestState.Accepted;
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
                    if (xElement == null) continue;
                    byte[] byteenc = StringToByteArray(xElement.Value);
                    AppendStatus("Wingmember:" + Encoding.UTF8.GetString(byteenc));

                    /* If the friend request matches the client name, store his session ID. */
                    /*
                                        if (_myrescue == null) continue;
                                        if (!string.Equals(Encoding.UTF8.GetString(byteenc), _myrescue.Client, StringComparison.CurrentCultureIgnoreCase)) continue;
                                        AppendStatus("This data matches our current client! Storing information...");
                                        XElement element = wingdata.Element("id");
                                        if (element != null) MyClient.ClientId = element.Value;
                                        AppendStatus("Wingmember IP data:" + xdoc.Element("connectionDetails"));
                                        const string wingIpPattern = "IP4NAT:([0-9.]+):\\d+\\,";
                                        Match wingMatch = Regex.Match(wingInvite, wingIpPattern, RegexOptions.IgnoreCase);
                                        if (wingMatch.Success)
                                        {
                                            AppendStatus("Successful IP data match: " + wingMatch.Groups[1]);
                                            MyClient.ClientIp = wingMatch.Groups[1].Value;
                                        }

                                        /* If the friend request matches the client name, store his session ID. */
                                        /*
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
=======
                    }; */
                }

            }
            catch (Exception ex)
            {
                Logger.Fatal("Error in parseWingInvite: " + ex.Message);
                _tc.TrackException(ex);
            }
        }

    }
}
