using System;

namespace RatTracker_WPF.Models.CmdrLog
{
    /// <summary>
    ///     Provides a common interface for all CmdrLog entries.
    /// </summary>
    public interface ICmdrLogEntry
    {
        /// <summary>
        ///     Time the event occured
        /// </summary>
        DateTime Timestamp { get; set; }

        /// <summary>
        ///     Name of the event as seen in the cmdr log.
        /// </summary>
        string Event { get; set; }
    }
}