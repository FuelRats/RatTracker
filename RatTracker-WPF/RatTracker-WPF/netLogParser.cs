using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Threading;
using System.Xml;
using System.Xml.Linq;
using log4net;
using Microsoft.ApplicationInsights;
using RatTracker_WPF.Models.Api;
using RatTracker_WPF.Models.App;
using RatTracker_WPF.EventHandlers;
namespace RatTracker_WPF
{
    public delegate void StatusUpdateEvent(object sender, StatusUpdateArgs args);

    public class StatusUpdate
    {

        public event StatusUpdateEvent StatusUpdateEvent;
        protected void NotifyStatusUpdate(object sender, StatusUpdateArgs args)
        {
             
            StatusUpdateEvent?.Invoke(sender, args);
        }

    }

    public class NetLogParser : StatusUpdate
    {
        private string _scState;
        private string _xmlparselist;
        private static readonly ILog Logger = LogManager.GetLogger(System.Reflection.Assembly.GetCallingAssembly().GetName().Name);
        private readonly TelemetryClient _tc = new TelemetryClient();
        private long _fileOffset;
        private long _fileSize; // Size of current netlog file
        private FileInfo _logFile; // Pointed to the live netLog file.


        public static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                .Where(x => x % 2 == 0)
                .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                .ToArray();
        }

        public void AppendStatus(string message)
        {
            this.NotifyStatusUpdate(this, new StatusUpdateArgs() {StatusMessage= message});
        }

        public void NetLogWatcher()
        {
            AppendStatus("From NetLogParser - Netlogwatcher started.");
            try
            {
                while (!StopNetLog)
                {
                    Thread.Sleep(2000);

                    FileInfo fi = new FileInfo(_logFile.FullName);
                    if (fi.Length == _fileSize) continue;
                    ReadLogfile(fi.FullName); /* Maybe a poke on the FS is enough to wake watcher? */
                    _fileOffset = fi.Length;
                    _fileSize = fi.Length;
                }
            }
            catch (Exception ex)
            {
                Logger.Debug("Netlog exception: " + ex.Message);
                _tc.TrackException(ex);
            }
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
/*                    ConnInfo.Srtt = int.Parse(statmatch.Groups[5].Value);
>>>>>>> master
                    ConnInfo.Loss = float.Parse(statmatch.Groups[6].Value);
                    ConnInfo.Jitter = float.Parse(statmatch.Groups[7].Value);
                    ConnInfo.Act1 = float.Parse(statmatch.Groups[8].Value);
                    ConnInfo.Act2 = float.Parse(statmatch.Groups[9].Value);
<<<<<<< HEAD
                    Dispatcher disp = Dispatcher;
                    disp.BeginInvoke(DispatcherPriority.Normal,
                        (Action)
                            (() =>
                                ConnectionStatus.Text =
                                    "SRTT: " + Conninfo.Srtt + " Jitter: " + Conninfo.Jitter + " Loss: " +
                                    Conninfo.Loss + " In: " + Conninfo.Act1 + " Out: " + Conninfo.Act2));
=======
                    */
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
