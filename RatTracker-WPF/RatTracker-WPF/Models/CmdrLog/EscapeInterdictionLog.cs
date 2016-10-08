using System;

namespace RatTracker_WPF.Models.CmdrLog
{
    public class EscapeInterdictionLog : ICmdrLogEntry
    {
        /// <summary>
        ///     Name of the interdictor
        /// </summary>
        public string Interdictor { get; set; }

        /// <summary>
        ///     Whether the interdictor is a player.
        /// </summary>
        public bool IsPlayer { get; set; }

        /// <summary>
        ///     Time the event occured.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        ///     Event type name.
        /// </summary>
        public string Event { get; set; }
    }
}