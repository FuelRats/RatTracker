using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RatTracker_WPF.Models.CmdrLog;
using System.Runtime.InteropServices;
using log4net;
using log4net.Repository.Hierarchy;
using RatTracker_WPF.Properties;

namespace RatTracker_WPF {
    public class CmdrLogParser {
        private List<ICmdrLogEntry> _cmdrLog;
        private string _cmdrLogFilePath;
        private readonly ILog _logger = LogManager.GetLogger(System.Reflection.Assembly.GetCallingAssembly().GetName().Name);

        public CmdrLogParser() {
            this._cmdrLog = new List<ICmdrLogEntry>();
            this._cmdrLogFilePath = Settings.Default.CmdrLogPath;
            Initialize(); // need to do a first time scan of the current log.
        }

        /// <summary>
        /// fills the cmdr log with the current session's log file.
        /// </summary>
        private void Initialize() {
            //make sure cmdrlogpath has a path in it
            if(string.IsNullOrWhiteSpace(this._cmdrLogFilePath) && !TryGetSavedGamesDir(out this._cmdrLogFilePath)) this._logger.Fatal("Could not get path to Commander Log!");

            if(Directory.Exists(this._cmdrLogFilePath)) {
                //TODO initialize cmdr log.
            }
        }

        /// <summary>
        /// tries to get the saved games directory from folder guid const. Credit to jgm on the ED fourms for this.
        /// </summary>
        private static bool TryGetSavedGamesDir(out string dir) {
            dir = "";
            IntPtr path;
            //if nothing is found, return failed.
            if(SHGetKnownFolderPath(new Guid("4C5C32FF-BB9D-43B0-B5B4-2D72E54EAAA4"), 0, new IntPtr(0), out path) < 0) return false;

            dir = Marshal.PtrToStringUni(path) + @"\Frontier Developments\Elite Dangerous";
            return true;
        }

        [DllImport("Shell32.dll")]
        private static extern int SHGetKnownFolderPath([MarshalAs(UnmanagedType.LPStruct)] Guid rfid, uint dwFlags, IntPtr hToken, out IntPtr ppszPath);
    }
}
