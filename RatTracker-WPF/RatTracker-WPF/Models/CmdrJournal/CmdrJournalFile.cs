using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RatTracker_WPF.Models.CmdrJournal
{
    /// <summary>
    ///     Holds file info and known entries for a given Cmdr Journal file.
    /// </summary>
    public class CmdrJournalFile
    {
        public CmdrJournalFile(string filePath)
        {
            FileInfo = new FileInfo(filePath);
            CmdrLogEntries = new List<ICmdrJournalEntry>();
        }

        public CmdrJournalFile(FileInfo fileInfo)
        {
            FileInfo = fileInfo;
            CmdrLogEntries = new List<ICmdrJournalEntry>();
        }

        public CmdrJournalFile(string filePath, IEnumerable<ICmdrJournalEntry> cmdrLogEntries)
        {
            FileInfo = new FileInfo(filePath);
            CmdrLogEntries = new List<ICmdrJournalEntry>(cmdrLogEntries);
        }

        public CmdrJournalFile(FileInfo fileInfo, IEnumerable<ICmdrJournalEntry> cmdrLogEntries)
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