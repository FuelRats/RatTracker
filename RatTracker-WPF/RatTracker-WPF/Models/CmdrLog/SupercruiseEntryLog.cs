using System;

namespace RatTracker_WPF.Models.CmdrLog
{
    public class SupercruiseEntryLog : ICmdrLogEntry
    {
        /// <summary>
        ///     Name of the starsystem the player is in.
        /// </summary>
        public string StarSystem { get; set; }

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