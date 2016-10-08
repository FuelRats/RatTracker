using System;
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
using RatTracker_WPF.Properties;

namespace RatTracker_WPF
{

    #region delegates

    // TODO do these properly and use event args. Passing the data is functional for now.
    // I'll just let absolver hate me later for this. I'LL FIX WHATEVER I BREAK!!1 -clap
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

    public class CmdrJournalParser
    {
        #region Constructor

        public CmdrJournalParser(MainWindow mainWindow)
        {
            var filePath = Settings.Default.CmdrLogPath;

            if (string.IsNullOrWhiteSpace(filePath))
                if (TryGetSavedGamesDir(out filePath))
                {
                    Settings.Default.CmdrLogPath = filePath;
                    Logger.Info($"Found Command Log Path! Setting path to: {filePath}");
                }
                else
                {
                    Logger.Fatal("Could not get path to Commander Log! Could not setup CmdrLogParser.");
                    return;
                }

            // filePath and Settings.Default.CmderLogPath should ALWAYS have the location at this point.

            // TODO add better handling for this. Attempt to re-find the file path again, or notify the user to manually find it.
            if (!Directory.Exists(filePath))
            {
                Logger.Fatal("Commander log file path is invalid. Could not setup CmdrLogParser.");
                return;
            }

            _cmdrLogMonitorThread = new Thread(CmdrLogMonitor) {Name = "CmdrLog Monitor"};

            _watcher = new FileSystemWatcher
            {
                Path = filePath,
                NotifyFilter =
                    NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName |
                    NotifyFilters.DirectoryName | NotifyFilters.Size,
                Filter = "Journal.*.log"
            };
            _watcher.Changed += _watcher_FSChanged;
            _watcher.Created += _watcher_FileCreated;
            _watcher.Deleted += _watcher_FileDeleted;
            _watcher.Renamed += _watcher_FileRenamed;

            _watcher.EnableRaisingEvents = true;

            _currentLogFile = new CmdrLogFile(GetLastModifiedFile(filePath, _watcher.Filter));

            mainWindow.MyPlayer.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(PlayerInfo.OnDuty))
                    StartListenerThread();
                else
                    StopListenerThread();
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

        private CmdrLogFile _currentLogFile;
        private readonly FileSystemWatcher _watcher;
        private volatile bool _terminateThread;
        private volatile bool _newFile = true;
        private static readonly ILog Logger = LogManager.GetLogger(Assembly.GetCallingAssembly().GetName().Name);
        private readonly Thread _cmdrLogMonitorThread;
        private int _lineOffset;

        public bool IsListening => _cmdrLogMonitorThread?.IsAlive ?? false;

        #endregion

        #region EventHandler Methods

        private void _watcher_FSChanged(object sender, FileSystemEventArgs e)
        {
            if (_currentLogFile.FileInfo.FullName == e.FullPath) return;

            _currentLogFile = new CmdrLogFile(e.FullPath);
            _newFile = true;
            Logger.Info($"A different cmdr log file has been updated. Now tracking {_currentLogFile.FileInfo.FullName}.");
        }

        private void _watcher_FileCreated(object sender, FileSystemEventArgs e)
        {
            _currentLogFile = new CmdrLogFile(e.FullPath);
            _newFile = true;
            Logger.Info($"A new CmdrLog file has been made. Now tracking {_currentLogFile.FileInfo.FullName}.");
        }

        private void _watcher_FileDeleted(object sender, FileSystemEventArgs e)
        {
            if (_currentLogFile.FileInfo.FullName != e.FullPath) return;

            _currentLogFile = new CmdrLogFile
            {
                FileInfo = GetLastModifiedFile(Settings.Default.CmdrLogPath, _watcher.Filter)
            };
            _newFile = true;
            Logger.Info(
                $"Currently tracked CmdrLog has been deleted. Now tracking {_currentLogFile.FileInfo.FullName} instead.");
        }

        private void _watcher_FileRenamed(object sender, RenamedEventArgs e)
        {
            if (_currentLogFile.FileInfo.FullName != e.OldFullPath) return;

            _currentLogFile = new CmdrLogFile(e.FullPath);
            Logger.Info(
                $"Currently tracked CmdrLog has been renamed. Now tracking {_currentLogFile.FileInfo.FullName} instead.");
        }

        #endregion

        #region Methods

        private void StartListenerThread()
        {
            if (_cmdrLogMonitorThread.IsAlive) return;

            _terminateThread = false;
            _cmdrLogMonitorThread?.Start();
        }

        private void StopListenerThread()
        {
            if (!_cmdrLogMonitorThread.IsAlive) return;

            _terminateThread = true;
            _cmdrLogMonitorThread?.Join(100);
        }

        private void CmdrLogMonitor()
        {
            while (!_terminateThread)
            {
                var fi = new FileInfo(_currentLogFile.FileInfo.FullName);
                if (!File.Exists(fi.FullName))
                {
                    Logger.Fatal("current log file does not exist!");
                    continue;
                }

                if (_newFile)
                {
                    var existingLines = File.ReadAllLines(fi.FullName);
                    _lineOffset = existingLines.Length;
                    foreach (var line in existingLines) ReadJObjectString(line, true);

                    _newFile = false;
                    continue;
                }

                var newLines = File.ReadLines(fi.FullName).Skip(_lineOffset).ToArray();

                if (newLines.Length > 0)
                {
                    _lineOffset += newLines.Length;
                    foreach (var line in newLines) ReadJObjectString(line);
                }

                Thread.Sleep(2000);
            }
        }

        private void ReadJObjectString(string jObjectString, bool suppressEvents = false)
        {
            var eventType =
                Regex.Match(jObjectString, "\"event\":\"(.*?)\",", RegexOptions.IgnoreCase).Groups[1].Value ?? "";


            switch (eventType)
            {
                case "CommitCrime":
                    var commitCrimeObj = JsonConvert.DeserializeObject<CommitCrimeLog>(jObjectString);
                    if (!suppressEvents) CommitCrimeEvent?.Invoke(this, commitCrimeObj);
                    _currentLogFile.CmdrLogEntries.Add(commitCrimeObj);
                    break;
                case "Died":
                    var diedObj = JsonConvert.DeserializeObject<DiedLog>(jObjectString);
                    if (!suppressEvents) DiedEvent?.Invoke(this, diedObj);
                    _currentLogFile.CmdrLogEntries.Add(diedObj);
                    break;
                case "EscapeInterdiction":
                    var escapeInterdictionObj = JsonConvert.DeserializeObject<EscapeInterdictionLog>(jObjectString);
                    if (!suppressEvents) EscapeInterdictionEvent?.Invoke(this, escapeInterdictionObj);
                    _currentLogFile.CmdrLogEntries.Add(escapeInterdictionObj);
                    break;
                case "FSDJump":
                    var fsdJumpObj = JsonConvert.DeserializeObject<FsdJumpLog>(jObjectString);
                    if (!suppressEvents) FsdJumpEvent?.Invoke(this, fsdJumpObj);
                    _currentLogFile.CmdrLogEntries.Add(fsdJumpObj);
                    break;
                case "FuelScoop":
                    var fuelScoopObj = JsonConvert.DeserializeObject<FuelScoopLog>(jObjectString);
                    if (!suppressEvents) FuelScoopEvent?.Invoke(this, fuelScoopObj);
                    _currentLogFile.CmdrLogEntries.Add(fuelScoopObj);
                    break;
                case "HullDamage":
                    var hullDamageObj = JsonConvert.DeserializeObject<HullDamageLog>(jObjectString);
                    if (!suppressEvents) HullDamageEvent?.Invoke(this, hullDamageObj);
                    _currentLogFile.CmdrLogEntries.Add(hullDamageObj);
                    break;
                case "Interdicted":
                    var interdictedObj = JsonConvert.DeserializeObject<InterdictedLog>(jObjectString);
                    if (!suppressEvents) InterdictedEvent?.Invoke(this, interdictedObj);
                    _currentLogFile.CmdrLogEntries.Add(interdictedObj);
                    break;
                case "Interdiction":
                    var interdictionObj = JsonConvert.DeserializeObject<InterdictionLog>(jObjectString);
                    if (!suppressEvents) InterdictionEvent?.Invoke(this, interdictionObj);
                    _currentLogFile.CmdrLogEntries.Add(interdictionObj);
                    break;
                case "ReceiveText":
                    var receiveTextObj = JsonConvert.DeserializeObject<ReceiveTextLog>(jObjectString);
                    if (!suppressEvents) ReceiveTextEvent?.Invoke(this, receiveTextObj);
                    _currentLogFile.CmdrLogEntries.Add(receiveTextObj);
                    break;
                case "SupercruiseEntry":
                    var supercruiseEntryObj = JsonConvert.DeserializeObject<SupercruiseEntryLog>(jObjectString);
                    if (!suppressEvents) SupercruiseEntryEvent?.Invoke(this, supercruiseEntryObj);
                    _currentLogFile.CmdrLogEntries.Add(supercruiseEntryObj);
                    break;
                case "SuperCruiseExit":
                    var superCruiseExitObj = JsonConvert.DeserializeObject<SuperCruiseExitLog>(jObjectString);
                    if (!suppressEvents) SuperCruiseExitEvent?.Invoke(this, superCruiseExitObj);
                    _currentLogFile.CmdrLogEntries.Add(superCruiseExitObj);
                    break;
                case "WingAdd":
                    var wingAddObj = JsonConvert.DeserializeObject<WingAddLog>(jObjectString);
                    if (!suppressEvents) WingAddEvent?.Invoke(this, wingAddObj);
                    _currentLogFile.CmdrLogEntries.Add(wingAddObj);
                    break;
                case "WingJoin":
                    var wingJoinObj = JsonConvert.DeserializeObject<WingJoinLog>(jObjectString);
                    if (!suppressEvents) WingJoinEvent?.Invoke(this, wingJoinObj);
                    _currentLogFile.CmdrLogEntries.Add(wingJoinObj);
                    break;
                case "WingLeave":
                    var wingLeaveObj = JsonConvert.DeserializeObject<WingLeaveLog>(jObjectString);
                    if (!suppressEvents) WingLeaveEvent?.Invoke(this, wingLeaveObj);
                    _currentLogFile.CmdrLogEntries.Add(wingLeaveObj);
                    break;
                default:
                    return;
            }
        }

        #endregion

        #region Static Methods

        /// <summary>
        ///     Gets the last modified file in the given file path
        /// </summary>
        private static FileInfo GetLastModifiedFile(string filePath, string filter = "*")
        {
            return
                (from f in new DirectoryInfo(filePath).GetFiles(filter) orderby f.LastWriteTime descending select f)
                    .First();
        }

        /// <summary>
        ///     Tries to get the saved games directory from folder guid const. Credit to jgm on the ED fourms for this.
        /// </summary>
        private static bool TryGetSavedGamesDir(out string dir)
        {
            dir = "";
            IntPtr path;

            //if nothing is found, return failed.
            if (SHGetKnownFolderPath(new Guid("4C5C32FF-BB9D-43B0-B5B4-2D72E54EAAA4"), 0, new IntPtr(0), out path) < 0)
                return false;

            dir = Path.Combine(Marshal.PtrToStringUni(path), "Frontier Developments", "Elite Dangerous");

            return Directory.Exists(dir);
        }

        //turns out that this is how Elite gets the file path. Neat.
        [DllImport("Shell32.dll")]
        private static extern int SHGetKnownFolderPath([MarshalAs(UnmanagedType.LPStruct)] Guid rfid, uint dwFlags,
            IntPtr hToken, out IntPtr ppszPath);

        #endregion
    }
}