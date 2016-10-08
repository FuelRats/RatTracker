using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RatTracker_WPF.Models.CmdrLog
{
    public class CmdrLogFile
    {
        public CmdrLogFile()
        {
        }

        public CmdrLogFile(string filePath)
        {
            FileInfo = new FileInfo(filePath);
            CmdrLogEntries = new List<ICmdrLogEntry>();
        }

        public CmdrLogFile(string filePath, IEnumerable<ICmdrLogEntry> cmdrLogEntries)
        {
            FileInfo = new FileInfo(filePath);
            CmdrLogEntries = new List<ICmdrLogEntry>(cmdrLogEntries);
        }

        public FileInfo FileInfo { get; set; }
        public List<ICmdrLogEntry> CmdrLogEntries { get; set; }

        /// <summary>
        ///     Gets the latest entry of the given event type.
        /// </summary>
        /// <param name="eventType">Name of the event to search for</param>
        /// <returns></returns>
        public ICmdrLogEntry GetLatestEntry(string eventType)
        {
            return CmdrLogEntries.Last(x => x.Event == eventType);
        }
    }
}