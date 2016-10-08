using System;

namespace RatTracker_WPF.Models.CmdrLog
{
    public class WingJoinLog : ICmdrLogEntry
    {
        /// <summary>
        ///     Array of Cmdr names who are also in the wing.
        /// </summary>
        public string[] Others { get; set; }

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