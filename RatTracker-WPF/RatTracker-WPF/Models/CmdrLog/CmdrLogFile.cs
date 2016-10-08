using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RatTracker_WPF.Models.CmdrLog
{
    /// <summary>
    ///     Holds file info and known entries for a given Cmdr Journal file.
    /// </summary>
    public class CmdrLogFile
    {
        public CmdrLogFile(string filePath)
        {
            FileInfo = new FileInfo(filePath);
            CmdrLogEntries = new List<ICmdrJournalEntry>();
        }

        public CmdrLogFile(FileInfo fileInfo)
        {
            FileInfo = fileInfo;
            CmdrLogEntries = new List<ICmdrJournalEntry>();
        }

        public CmdrLogFile(string filePath, IEnumerable<ICmdrJournalEntry> cmdrLogEntries)
        {
            FileInfo = new FileInfo(filePath);
            CmdrLogEntries = new List<ICmdrJournalEntry>(cmdrLogEntries);
        }

        public CmdrLogFile(FileInfo fileInfo, IEnumerable<ICmdrJournalEntry> cmdrLogEntries)
        {
            FileInfo = fileInfo;
            CmdrLogEntries = new List<ICmdrJournalEntry>(cmdrLogEntries);
        }

        public FileInfo FileInfo { get; set; }
        public List<ICmdrJournalEntry> CmdrLogEntries { get; set; }

        /// <summary>
        ///     Gets the latest entry of the given event type.
        /// </summary>
        /// <param name="eventType">Name of the event to search for</param>
        /// <returns></returns>
        public ICmdrJournalEntry GetLatestEntry(string eventType)
        {
            return CmdrLogEntries.Last(x => x.Event == eventType);
        }
    }
}