using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using log4net;
using Newtonsoft.Json;
using RatTracker_WPF.Models.App;
using RatTracker_WPF.Models.CmdrLog;
using RatTracker_WPF.Models.EventArgs;
using RatTracker_WPF.Properties;

namespace RatTracker_WPF {

    #region delegates

    // TODO do these properly and use event args. Passing the data is functional for now.
    // I'll just let absolver hate me later for this. I'LL FIX IT! -clap
    public delegate void CommitCrimeEvent(object sender, CommitCrimeLog eventData);
    public delegate void DiedEvent(object sender, DiedLog eventData);
    public delegate void EscapeInterdictionEvent(object sender, EscapeInterdictionLog eventData);
    public delegate void FsdJumpEvent(object sender, FsdJumpLog eventData);
    public delegate void FuelScoopEvent(object sender, FuelScoopLog eventData);
    public delegate void HullDamageEvent(object sender, HullDamageLog eventData);
    public delegate void InterdictedEvent(object sender, InterdictedLog eventData);
    public delegate void InterdictionEvent(object sender, InterdictionLog eventData);
    public delegate void ReceiveTextEvent(object sender, ReceiveTextLog eventData);
    public delegate void SupercruiseEntryEvent(object sender, SupercruiseEntryLog eventData);
    public delegate void SuperCruiseExitEvent(object sender, SuperCruiseExitLog eventData);
    public delegate void WingAddEvent(object sender, WingAddLog eventData);
    public delegate void WingJoinEvent(object sender, WingJoinLog eventData);
    public delegate void WingLeaveEvent(object sender, WingLeaveLog eventData);

    #endregion

    public class CmdrLogParser {

        #region Constructor

        public CmdrLogParser(MainWindow mainWindow) {
            //Ensure the file path to the cmdr logs is found and stored for later use.
            string filePath = Settings.Default.CmdrLogPath;

            if(string.IsNullOrWhiteSpace(filePath))
                if(TryGetSavedGamesDir(out filePath)) {
                    //store the file path so we don't have to do this again.
                    Settings.Default.CmdrLogPath = filePath;
                    _logger.Info($"Found Command Log Path! Setting path to: {filePath}");
                }
                else {
                    // We can't continue anything without a proper log path. ABORT ABORT!
                    _logger.Fatal("Could not get path to Commander Log! Could not setup CmdrLogParser.");
                    return;
                }

            // filePath and Settings.Default.CmderLogPath should ALWAYS have

            // Make sure the directory actually exists.
            // TODO add better handling for this. Attempt to re-find the file path again, or notify the user to manually find it.
            if(!Directory.Exists(filePath)) {
                _logger.Fatal("Commander log file path is invalid. Could not setup CmdrLogParser.");
                return;
            }

            //Setup the FileSystemWatcher, and subscribe to it's events.
            _watcher = new FileSystemWatcher {
                           Path = filePath,
                           NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.Size,
                           Filter = "Journal.*.log"
                       };
            _watcher.Changed += _watcher_FSChanged;
            _watcher.Created += _watcher_FileCreated;
            _watcher.Deleted += _watcher_FileDeleted;
            _watcher.Renamed += _watcher_FileRenamed;

            _watcher.EnableRaisingEvents = true;

            //Get the most current file in the directory to track for the first time.
            //We want to avoid using this method of getting the file if possible, but it's fine for the first setup.
            _currentLogFile = GetLastModifiedFile(filePath, _watcher.Filter);

            // if the client is on duty now, go ahead and start the listener thread now.
            if(mainWindow.MyPlayer.OnDuty) {
                StartListenerThread();
            }

            mainWindow.MyPlayer.PropertyChanged += (sender, args) => {
                                                       if(args.PropertyName != nameof(PlayerInfo.OnDuty)) return;
                                                       if(mainWindow.MyPlayer.OnDuty) StartListenerThread();
                                                       else StopListenerThread();
                                                   };


        }

        #endregion

        #region Event Declarations

        public event CommitCrimeEvent CommitCrimeEvent;
        public event DiedEvent DiedEvent;
        public event EscapeInterdictionEvent EscapeInterdictionEvent;
        public event FsdJumpEvent FsdJumpEvent;
        public event FuelScoopEvent FuelScoopEvent;
        public event HullDamageEvent HullDamageEvent;
        public event InterdictedEvent InterdictedEvent;
        public event InterdictionEvent InterdictionEvent;
        public event ReceiveTextEvent ReceiveTextEvent;
        public event SupercruiseEntryEvent SupercruiseEntryEvent;
        public event SuperCruiseExitEvent SuperCruiseExitEvent;
        public event WingAddEvent WingAddEvent;
        public event WingJoinEvent WingJoinEvent;
        public event WingLeaveEvent WingLeaveEvent;

        #endregion

        #region Fields

        private FileInfo _currentLogFile;
        private CmdrLogFile _currentLogFileData;
        private readonly ILog _logger = LogManager.GetLogger(Assembly.GetCallingAssembly().GetName().Name);
        private readonly FileSystemWatcher _watcher;
        private volatile bool _terminateThread = false;
        private Thread _cmdrLogMonitorThread;

        #endregion

        #region EventHandler Methods

        private void _watcher_FSChanged(object sender, FileSystemEventArgs e) {
            if(_currentLogFile.FullName == e.FullPath) return;

            _currentLogFile = new FileInfo(e.FullPath);
            _logger.Info($"A different cmdr log file has been updated. Now tracking {_currentLogFile.FullName}.");
        }

        private void _watcher_FileCreated(object sender, FileSystemEventArgs e) {
            // Looks like a new journal has been created. Let's switch to it. 
            _currentLogFile = new FileInfo(e.FullPath);
            _logger.Info($"A new CmdrLog file has been made. Now tracking {_currentLogFile.FullName}.");
        }

        private void _watcher_FileDeleted(object sender, FileSystemEventArgs e) {
            //We don't care if the deleted file isn't the currently tracked file.
            if(_currentLogFile.FullName != e.FullPath) return;

            //Shoot somthing deleted the file we were watching. Time to find a new one!
            _currentLogFile = GetLastModifiedFile(Settings.Default.CmdrLogPath, _watcher.Filter);
            _logger.Info($"Currently tracked CmdrLog has been deleted. Now tracking {_currentLogFile.FullName} instead.");
        }

        private void _watcher_FileRenamed(object sender, RenamedEventArgs e) {
            //We don't care if the renamed file isn't the file we're currently tracking.
            if(_currentLogFile.FullName != e.OldFullPath) return;

            //Something changed the name of our tracked file. Lets make sure we continue tracking it.
            _currentLogFile = new FileInfo(e.FullPath);
            _logger.Info($"Currently tracked CmdrLog has been renamed. Now tracking {_currentLogFile.FullName} instead.");
        }

        #endregion

        #region Methods

        private void StartListenerThread() {
            if(_cmdrLogMonitorThread != null && _cmdrLogMonitorThread.IsAlive)
                return;

            _terminateThread = false;
            _cmdrLogMonitorThread = new Thread(CmdrLogMonitor) {Name = "CmdrLog Monitor"};
            _cmdrLogMonitorThread.Start();
        }

        private void StopListenerThread() {
            _terminateThread = true;
            if(_cmdrLogMonitorThread.IsAlive)
                _cmdrLogMonitorThread.Join(100);
        }

        private void CmdrLogMonitor() {
            if(!_currentLogFile.Exists) return;

            try {
                using(FileStream fs = new FileStream(_currentLogFile.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using(StreamReader sr = new StreamReader(fs))
                    while(!_terminateThread) {
                        Thread.Sleep(2000);
                        string logLine = sr.ReadLine();
                        if(!string.IsNullOrWhiteSpace(logLine)) ReadJObjectString(logLine);
                    }
            }
            catch(Exception e) {
                _logger.Fatal($"Monitor thread encountered a fatal error. aborting. Error Message: {e.Message}");
                _terminateThread = true;
            }
        }

        private void ReadJObjectString(string jObjectString, bool firstRead = false) {
            string eventType = Regex.Match(jObjectString, "\"event\":\"(.*?)\",", RegexOptions.IgnoreCase).Groups[1].Value ?? "";
            Dictionary<string, string> j = JsonConvert.DeserializeObject<Dictionary<string, string>>(jObjectString);

            //make sure the object contains 
            if(!j.ContainsKey("event")) {
                _logger.Error($"Log entry read does not contain an event type. Log Entry text: {jObjectString}");
                return;
            }

            //just make sure that the log file data isn't null.
            if(_currentLogFileData == null) _currentLogFileData = new CmdrLogFile(_currentLogFile.FullName);

            //Find the event type. at least I'm not using if statements for this crap.
            switch(eventType) {
                case "CommitCrime":
                    CommitCrimeLog commitCrimeObj = JsonConvert.DeserializeObject<CommitCrimeLog>(jObjectString);
                    if(!firstRead) CommitCrimeEvent?.Invoke(this, commitCrimeObj);
                    _currentLogFileData.CmdrLogEntries.Add(commitCrimeObj);
                    break;
                case "Died":
                    DiedLog diedObj = JsonConvert.DeserializeObject<DiedLog>(jObjectString);
                    if(!firstRead) DiedEvent?.Invoke(this, diedObj);
                    _currentLogFileData.CmdrLogEntries.Add(diedObj);
                    break;
                case "EscapeInterdiction":
                    EscapeInterdictionLog escapeInterdictionObj = JsonConvert.DeserializeObject<EscapeInterdictionLog>(jObjectString);
                    if(!firstRead) EscapeInterdictionEvent?.Invoke(this, escapeInterdictionObj);
                    _currentLogFileData.CmdrLogEntries.Add(escapeInterdictionObj);
                    break;
                case "FSDJump":
                    FsdJumpLog fsdJumpObj = JsonConvert.DeserializeObject<FsdJumpLog>(jObjectString);
                    if(!firstRead) FsdJumpEvent?.Invoke(this, fsdJumpObj);
                    _currentLogFileData.CmdrLogEntries.Add(fsdJumpObj);
                    break;
                case "FuelScoop":
                    FuelScoopLog fuelScoopObj = JsonConvert.DeserializeObject<FuelScoopLog>(jObjectString);
                    if(!firstRead) FuelScoopEvent?.Invoke(this, fuelScoopObj);
                    _currentLogFileData.CmdrLogEntries.Add(fuelScoopObj);
                    break;
                case "HullDamage":
                    HullDamageLog hullDamageObj = JsonConvert.DeserializeObject<HullDamageLog>(jObjectString);
                    if(!firstRead) HullDamageEvent?.Invoke(this, hullDamageObj);
                    _currentLogFileData.CmdrLogEntries.Add(hullDamageObj);
                    break;
                case "Interdicted":
                    InterdictedLog interdictedObj = JsonConvert.DeserializeObject<InterdictedLog>(jObjectString);
                    if(!firstRead) InterdictedEvent?.Invoke(this, interdictedObj);
                    _currentLogFileData.CmdrLogEntries.Add(interdictedObj);
                    break;
                case "Interdiction":
                    InterdictionLog interdictionObj = JsonConvert.DeserializeObject<InterdictionLog>(jObjectString);
                    if(!firstRead) InterdictionEvent?.Invoke(this, interdictionObj);
                    _currentLogFileData.CmdrLogEntries.Add(interdictionObj);
                    break;
                case "ReceiveText":
                    ReceiveTextLog receiveTextObj = JsonConvert.DeserializeObject<ReceiveTextLog>(jObjectString);
                    if(!firstRead) ReceiveTextEvent?.Invoke(this, receiveTextObj);
                    _currentLogFileData.CmdrLogEntries.Add(receiveTextObj);
                    break;
                case "SupercruiseEntry":
                    SupercruiseEntryLog supercruiseEntryObj = JsonConvert.DeserializeObject<SupercruiseEntryLog>(jObjectString);
                    if(!firstRead) SupercruiseEntryEvent?.Invoke(this, supercruiseEntryObj);
                    _currentLogFileData.CmdrLogEntries.Add(supercruiseEntryObj);
                    break;
                case "SuperCruiseExit":
                    SuperCruiseExitLog superCruiseExitObj = JsonConvert.DeserializeObject<SuperCruiseExitLog>(jObjectString);
                    if(!firstRead) SuperCruiseExitEvent?.Invoke(this, superCruiseExitObj);
                    _currentLogFileData.CmdrLogEntries.Add(superCruiseExitObj);
                    break;
                case "WingAdd":
                    WingAddLog wingAddObj = JsonConvert.DeserializeObject<WingAddLog>(jObjectString);
                    if(!firstRead) WingAddEvent?.Invoke(this, wingAddObj);
                    _currentLogFileData.CmdrLogEntries.Add(wingAddObj);
                    break;
                case "WingJoin":
                    WingJoinLog wingJoinObj = JsonConvert.DeserializeObject<WingJoinLog>(jObjectString);
                    if(!firstRead) WingJoinEvent?.Invoke(this, wingJoinObj);
                    _currentLogFileData.CmdrLogEntries.Add(wingJoinObj);
                    break;
                case "WingLeave":
                    WingLeaveLog wingLeaveObj = JsonConvert.DeserializeObject<WingLeaveLog>(jObjectString);
                    if(!firstRead) WingLeaveEvent?.Invoke(this, wingLeaveObj);
                    _currentLogFileData.CmdrLogEntries.Add(wingLeaveObj);
                    break;
                default:
                    return;
            }
        }

        #endregion

        #region Static Methods

        /// <summary>
        /// Gets the last modified file in the given file path
        /// </summary>
        private static FileInfo GetLastModifiedFile(string filePath, string filter = "*") { return (from f in new DirectoryInfo(filePath).GetFiles(filter) orderby f.LastWriteTime descending select f).First(); }

        /// <summary>
        /// Tries to get the saved games directory from folder guid const. Credit to jgm on the ED fourms for this.
        /// </summary>
        private static bool TryGetSavedGamesDir(out string dir) {
            dir = "";
            IntPtr path;

            //if nothing is found, return failed.
            if(SHGetKnownFolderPath(new Guid("4C5C32FF-BB9D-43B0-B5B4-2D72E54EAAA4"), 0, new IntPtr(0), out path) < 0) return false;

            dir = Marshal.PtrToStringUni(path) + @"\Frontier Developments\Elite Dangerous";
            return true;
        }

        //turns out that this is how Elite gets the file path. Neat.
        [DllImport("Shell32.dll")]
        private static extern int SHGetKnownFolderPath([MarshalAs(UnmanagedType.LPStruct)] Guid rfid, uint dwFlags, IntPtr hToken, out IntPtr ppszPath);

        #endregion
    }

}