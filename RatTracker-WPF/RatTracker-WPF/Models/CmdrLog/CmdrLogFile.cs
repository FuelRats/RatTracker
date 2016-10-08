using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RatTracker_WPF.Models.CmdrLog {
    public class CmdrLogFile {
        public FileInfo FileInfo { get; private set; }
        public List<ICmdrLogEntry> CmdrLogEntries { get; set; }

        public CmdrLogFile() { }

        public CmdrLogFile(string filePath) {
            FileInfo = new FileInfo(filePath);
            CmdrLogEntries = new List<ICmdrLogEntry>();
        }

        public CmdrLogFile(string filePath, List<ICmdrLogEntry> cmdrLogEntries) {
            FileInfo = new FileInfo(filePath);
            CmdrLogEntries = new List<ICmdrLogEntry>(cmdrLogEntries);
        }

        /// <summary>
        /// Gets the latest entry of the given event type.
        /// </summary>
        /// <param name="eventType">Name of the event to search for</param>
        /// <returns></returns>
        public ICmdrLogEntry GetLatestEntry(string eventType) { return CmdrLogEntries.Last(x => x.Event == eventType); }

    }
}