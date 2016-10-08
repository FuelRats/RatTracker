using RatTracker_WPF.Models.CmdrLog;

namespace RatTracker_WPF.Models.EventArgs {

    public class NewCmdrLogFileEventArgs : System.EventArgs {
        public CmdrLogFile logFile { get; set; }
    }

}