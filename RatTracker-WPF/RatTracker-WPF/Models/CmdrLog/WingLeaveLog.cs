using System;

namespace RatTracker_WPF.Models.CmdrLog
{
    public class WingLeaveLog : ICmdrJournalEntry
    {
        /// <summary>
        ///     Time the event occured
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        ///     Name of the event as seen in the cmdr log.
        /// </summary>
        public string Event { get; set; }
    }
}