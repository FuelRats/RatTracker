using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.IO;
using System.Net;
using System.Reflection;
using System.Diagnostics;
using System.Security.Permissions;
using System.Threading;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace RatTracker_WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 

        public class netLogFile
    {
        public string netLogFileName;
        public DateTime lastChanged;
        public long fileOffset;
    }
        public class clientInfo
    {
        public string clientName { get; set; }
        public string clientID { get; set; }
        public string clientState { get; set; }
        public string clientIP { get; set; }
        public string sessionID { get; set; }

    }
    public class Friend
    {
        private static string GetElementValue(XContainer element, string name)
        {
            if ((element == null) || (element.Element(name) == null))
                return String.Empty;
            return element.Element(name).Value;
        }

        public string Name { get; private set; }
        public string Location { get; private set; }
        public string PrivGroup { get; private set; }
        public string CanWing { get; private set; }
        public Friend(XContainer friend)
        {
            Name = GetElementValue(friend, "Name");
            Location = GetElementValue(friend, "lastLocation");
            CanWing = GetElementValue(friend, "inviteToWing");
            PrivGroup = GetElementValue(friend, "privateGroup_id");
        }
    }
    public partial class MainWindow : Window
    {
        public bool stopNetLog;
        bool onDuty=false;
        string logDirectory="G:\\Frontier\\EDLaunch\\Products\\elite-dangerous-64\\Logs";
        FileSystemWatcher watcher;
        FileInfo logFile;
        long fileOffset =0L;
        long fileSize;
        Thread threadLogWatcher;
        string parserState;
        public MainWindow()
        {
            InitializeComponent();
            checkLogDirectory();
        }
        public bool sendAPI(string field, string data)
        {
            /* Once Trezy actually provides us with some useful API stuff, this is where we'll send data. */
            return true;
        }

        public string queryAPI(string field, string data)
        {
            /* Again, waiting for Trezy. For now, return a placeholder field. */
            return "I am a string from the API";
        }

        public bool connectAPI()
        {
            /* Connect to the API here. */
            appendStatus("Connecting to API.");
            return true;
        }
        private void submitPaperwork(string url)
        {
            Process.Start(url);
        }
        private void checkVerboseLogging()
        {
            /* if (CheckStationLogging())
            {
                appendStatus("Elite Dangerous is not logging system names!!! ");
                appendStatus("Add VerboseLogging=\"1\" to <Network> section in your config file and restart your client!");
            } */
        }


        public void appendStatus(string text)
        {
            Console.WriteLine("I just got called for appendStatus!");
            if (statusDisplay.Dispatcher.CheckAccess())
            {
                statusDisplay.Text += "\n" + text;
                statusDisplay.ScrollToEnd();
                statusDisplay.CaretIndex = statusDisplay.Text.Length;
            }
            else
            {
                statusDisplay.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action<string>(appendStatus), text );
            }
            

        }
        private void onRenamed(object source, RenamedEventArgs e)
        {
            /* Stop watching the renamed file, look for new onChanged. */
        }

        public static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }
        private void parseFriendsList(string friendsList)
        {
            /* Sanitize the XML, it can break if over 40 friends long or so. */
            string xmlData;
            xmlData = friendsList.Substring(friendsList.IndexOf("<") + friendsList.Length);
            appendStatus("Raw xmlData: " + xmlData);
            try {
                XDocument xdoc = XDocument.Parse(friendsList);
                appendStatus("Successful XML parse.");
                var rettest = xdoc.Element("OK");
                if(rettest != null)
                    appendStatus("Return code: " + xdoc.Element("OK").Value);
                IEnumerable<XElement> friends = xdoc.Descendants("item");
                foreach (var friend in friends)
                {
                    byte[] byteenc;
                    UnicodeEncoding unicoded = new UnicodeEncoding();
                    /* string preencode = Regex.Replace(friend.Element("name").Value, ".{2}", "\\x$0"); */
                    byteenc = StringToByteArray(friend.Element("name").Value);
                    appendStatus("Friend:" + System.Text.Encoding.UTF8.GetString(byteenc));
                    if (friend.Element("pending").Value == "1")
                    {
                        appendStatus("Pending invite from CMDR " + System.Text.Encoding.UTF8.GetString(byteenc) + "detected!");
                        var disp = Dispatcher;
                        Brush frbrush=null;
                        disp.BeginInvoke(DispatcherPriority.Normal, (Action)(() => { frbrush = frButton.Background; }));
                        if (frbrush != Brushes.Green)
                        { /* Dear gods, you're a cheap hack, aren't you? */
                            disp.BeginInvoke(DispatcherPriority.Normal, (Action)(() => frButton.Background = Brushes.Yellow));
                        }
                    }
                }
                /* Check the OK status field, which can contain useful information on successful FRs. */
                foreach (var element in xdoc.Descendants())
                {
                    if (element.Name == "OK")
                    {
                        appendStatus("Return code: " + xdoc.Element("data").Element("OK").Value);
                        if (xdoc.Element("data").Element("OK").Value.Contains("Invitation accepted"))
                        {
                            appendStatus("Friend request accepted!");
                            var disp = Dispatcher;
                            disp.BeginInvoke(DispatcherPriority.Normal, (Action)(() => frButton.Background = Brushes.Green));
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                appendStatus("XML Parsing exception:" + ex.Message);
            }

        }
        private void parseWingInvite(string wingInvite)
        {
            string xmlData;
            xmlData = wingInvite.Substring(wingInvite.IndexOf("<") + wingInvite.Length);
            appendStatus("Raw xmlData: " + xmlData);
            try
            {
                XDocument xdoc = XDocument.Parse(wingInvite);
                appendStatus("Successful XML parse.");

                IEnumerable<XElement> wing = xdoc.Descendants("commander");
                foreach (var wingdata in wing)
                {
                    byte[] byteenc;
                    UnicodeEncoding unicoded = new UnicodeEncoding();
                    /* string preencode = Regex.Replace(friend.Element("name").Value, ".{2}", "\\x$0"); */
                    byteenc = StringToByteArray(wingdata.Element("name").Value);
                    appendStatus("Wingmember:" + System.Text.Encoding.UTF8.GetString(byteenc));
                }


            }
            catch (Exception ex)
            {
                appendStatus("Error in parseWingInvite: " + ex.Message);
                return;
            }
            return;
        }

        private void checkLogDirectory()
        {
            if(logDirectory==null | logDirectory == "")
            {
                MessageBox.Show("Error: No log directory is specified, please do so before attempting to go on duty.");
            }
            textBox.Text = logDirectory;
            statusDisplay.Text = "Beginning to watch " + logDirectory + " for changes...";
            if (watcher==null)
            {
                watcher = new FileSystemWatcher();
                watcher.Path = logDirectory;
                watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.Size;
                watcher.Filter = "*.log";
                watcher.Changed += new FileSystemEventHandler(onChanged);
                watcher.Created += new FileSystemEventHandler(onChanged);
                watcher.Deleted += new FileSystemEventHandler(onChanged);
                watcher.Renamed += new RenamedEventHandler(onRenamed);
                watcher.EnableRaisingEvents = true;

            }
            DirectoryInfo tempDir=new DirectoryInfo(logDirectory);
            logFile = (from f in tempDir.GetFiles("*.log") orderby f.LastWriteTime descending select f).First();
            appendStatus("Started watching file " + logFile.FullName);
            checkClientConn(logFile.FullName);
            readLogfile(logFile.FullName);
        }
        private void checkClientConn(string lf)
        {
            bool stopSnooping=false;
            appendStatus("Detecting client connectivity...");
            try
            {
                using (StreamReader sr = new StreamReader(new FileStream(lf, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete)))
                {
                    string line;
                    int count = 0;
                    while (stopSnooping != true && sr.Peek() != -1)
                    {
                        count++;
                        line = sr.ReadLine();
                        if(line.Contains("WAN:"))
                        {
                            appendStatus("E:D is configured to listen on "+line);
                        }
                        if(line.Contains("failed to initialise upnp"))
                        {
                            appendStatus("CRITICAL: E:D has failed to establish a upnp port mapping, but E:D is configured to use upnp. Disable upnp in netlog if you have manually mapped ports.");
                        }

                        if(line.Contains("Turn State: Ready"))
                        {
                            appendStatus("Client has a valid TURN connection established.");
                        }
                        if(line.Contains("this machine after STUN reply"))
                        {
                            appendStatus("STUN has mapped us to address.");
                        }
                        if (line.Contains("Sync Established"))
                        {
                            appendStatus("Sync Established.");
                        }
                        if (line.Contains("ConnectToServerActivity:StartRescueServer"))
                        {
                            appendStatus("E:D has established a connection and client is in main menu. Ending early netlog parse.");
                            stopSnooping = true;
                        }

                    }
                    appendStatus("Parsed " + count + " lines to derive client info.");
                }
            }
            catch (Exception ex)
            {
                appendStatus("Exception in checkClientConn:" + ex.Message);
                return;
            }

        }
        private void readLogfile(string lf)
        {
            try
            {
                using (StreamReader sr = new StreamReader(new FileStream(lf, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete)))
                {
                    if (fileOffset == 0L)
                    {
                        appendStatus("First peek...");
                        if (sr.BaseStream.Length > 30000)
                        {
                            sr.BaseStream.Seek(-30000, SeekOrigin.End); /* First peek into the file, rewind a bit and scan from there. */
                            appendStatus("Rewind skipped, reading full log.");
                        }
                    }
                    else
                    {
                        sr.BaseStream.Seek(this.fileOffset, SeekOrigin.Begin);
                        appendStatus("Seek to " + fileOffset);
                    }

                    string line;
                    int count=0;
                    while (sr.Peek() != -1)
                    {
                        count++;
                        line = sr.ReadLine();
                        parseLine(line);
                    }
                    appendStatus("Parsed " + count + " new lines. Old fileOffset was "+fileOffset+" and length was "+logFile.Length);
                }
            }
            catch (Exception ex)
            {
                appendStatus("Exception in readLogFile: " + ex.Message);
                return;
            }
        }
        private void  parseLine(string line)
        {
            string reMatchSystem = ".*?(System).*?\\(((?:[^)]+)).*?\\)";
            Match match = Regex.Match(line, reMatchSystem, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                appendStatus("System change: " + match.Groups[2].Value + ".");
            }
            if (line.Contains(" System:"))
            {
                appendStatus("Second method works too...");
            }
            if (line.Contains("FriendsRequest"))
            {
                parserState = "xml";
                appendStatus("Enter XML parse state. Full line: " + line);
                appendStatus("Received FriendsList update, ready to parse...");
            }
            if (line.Contains("<data>"))
            {
                appendStatus("Line sent to XML parser");
                parseFriendsList(line);
            }
            /* Look, we're doing nothing! */
            if (line.Contains("</data>"))
            {
                appendStatus("Exit XML parsing mode.");
                parserState = "normal";
            }
            if (line.Contains("<FriendWingInvite>"))
            {
                appendStatus("Wing invite detected, parsing...");
                parseWingInvite(line);
                Dispatcher disp = Dispatcher;
                disp.BeginInvoke(DispatcherPriority.Normal, (Action)(() => wrButton.Background = Brushes.Yellow));

            }
            if (line.Contains("JoinSession:WingSession:"))
            {
                appendStatus("Prewing communication underway...");
            }
            if (line.Contains("TalkChannelManager::OpenOutgoingChannelTo"))
            {
                appendStatus("Wing established, opening voice comms.");
                Dispatcher disp = Dispatcher;
                disp.BeginInvoke(DispatcherPriority.Normal, (Action)(() => wrButton.Background = Brushes.Green));

            }
            if(line.Contains("ListenResponse->Listening (SUCCESS: User has responded via local talkchannel)"))
            {
                appendStatus("Voice communications established.");
            }
            if (line.Contains("NormalFlight"))
            {
                appendStatus("Drop to normal space detected.");
            }
            if(line.Contains("CLAIMED ------------vvv"))
            {
                appendStatus("Island claim message detected, parsing members...");
            }
            if (line.Contains("SESJOINED"))
            {
                appendStatus("Session join message seen.");
            }
        }

        private void onChanged(object source, FileSystemEventArgs e)
        {
            logFile = new FileInfo(e.FullPath);
            /* Handle changed events */
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            if (onDuty == false)
            {
                button.Content = "On Duty";
                onDuty = true;
                watcher.EnableRaisingEvents = true;
                statusDisplay.Text += "\nStarted watching for events in netlog.";
                button.Background = Brushes.Green; 
                stopNetLog = false;
                threadLogWatcher = new System.Threading.Thread(new System.Threading.ThreadStart(netLogWatcher));
                threadLogWatcher.Name = "Netlog watcher";
                threadLogWatcher.Start();
                
                /* image.Source = new BitmapImage(RatTracker_WPF.Properties.Resources.yellow_light); */
                appendStatus("I should be reading from the log now.");
                appendStatus("Connecting to RatTracker service...");
                appendStatus("Connected to 10.0.0.35 port 38550!");
                appendStatus("Sending Rat API key...");
                appendStatus("Received handshake. Connected to RTS.");
                appendStatus("NEW ASSIGNMENT: Absolver|Sagittarius A*|0|0|[]");
                clientName.Text = "Absolver";
                systemName.Text = "Sagittarius A*";
            }
            else
            {
                button.Content = "Off Duty";
                onDuty = false;
                watcher.EnableRaisingEvents = false;
                statusDisplay.Text += "\nStopped watching for events in netlog.";
                button.Background = Brushes.Red;
                stopNetLog = true;
            }
        }
        private void netLogWatcher()
        {
            appendStatus("Netlogwatcher started.");
            bool logChanged = false;
            try
            {
                while (!stopNetLog)
                {
                    Thread.Sleep(2000);
                    
                    if (logChanged == false)
                    {
                        FileInfo fi = new FileInfo(logFile.FullName);
                        //appendStatus("Netlog tick with status false. LFL:"+fi.Length+ "Filesize: "+fileSize);
                        if (fi.Length != fileSize)
                        {
                            //appendStatus("Log file size increased.");
                            readLogfile(fi.FullName); /* Maybe a poke on the FS is enough to wake watcher? */
                            fileOffset = fi.Length;
                            fileSize = fi.Length;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("Netlog exception: " + ex.Message);
            }
        }
        private void Main_Menu_Click(object sender, RoutedEventArgs e)
        {
            /* Fleh? */
        }

        private void frButton_Click(object sender, RoutedEventArgs e)
        {
            if (frButton.Background == Brushes.Red)
            {
                frButton.Background = Brushes.Green;
                appendStatus("Sending Friend Request acknowledgement.");
                /* image.Source = new BitmapImage(RatTracker_WPF.Properties.Resources.yellow_light); */
            }
            else
            {
                appendStatus("Cancelling FR status.");
                frButton.Background = Brushes.Red;
            }
        }

        private void wrButton_Click(object sender, RoutedEventArgs e)
        {
            if (wrButton.Background == Brushes.Red)
            {
                appendStatus("Sending Wing Request acknowledgement.");
                wrButton.Background = Brushes.Green;
                /* image.Source = new BitmapImage(RatTracker_WPF.Properties.Resources.yellow_light); */
            }
            else
            {
                appendStatus("Cancelled WR status.");
                wrButton.Background = Brushes.Red;
            }
        }

        private void bcnButton_Click(object sender, RoutedEventArgs e)
        {
            if (bcnButton.Background == Brushes.Red)
            {
                appendStatus("Sending Beacon acknowledgement.");
                bcnButton.Background = Brushes.Green;
                /* image.Source = new BitmapImage(RatTracker_WPF.Properties.Resources.yellow_light); */
            }
            else
            {
                appendStatus("Cancelling Beacon status.");
                bcnButton.Background = Brushes.Red;
            }
        }

        private void instButton_Click(object sender, RoutedEventArgs e)
        {
            if (instButton.Background == Brushes.Red)
            {
                appendStatus("Sending Good Instance message.");
                instButton.Background = Brushes.Green;
                /* image.Source = new BitmapImage(RatTracker_WPF.Properties.Resources.yellow_light); */
            }
            else
            {
                appendStatus("Cancelling good instance message.");
                instButton.Background = Brushes.Red;
            }
        }

        private void fueledButton_Click(object sender, RoutedEventArgs e)
        {
            if (fueledButton.Background == Brushes.Red)
            {
                appendStatus("Reporting fueled status, requesting paperwork link...");
                fueledButton.Background = Brushes.Green;
                /* image.Source = new BitmapImage(RatTracker_WPF.Properties.Resources.yellow_light); */
            }
            else
            {
                appendStatus("Fueled status now negative.");
                fueledButton.Background = Brushes.Red;
            }
        }

        private void currentButton_Click(object sender, RoutedEventArgs e)
        {
            appendStatus("Setting client location to current system: Fuelum");
            systemName.Text = "Fuelum";
        }

        private void updateButton_Click(object sender, RoutedEventArgs e)
        {
            appendStatus("Sending updated client system location: " + systemName.Text);
        }

        private void startButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
