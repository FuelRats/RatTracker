using System;
using Newtonsoft.Json;

namespace RatTracker_WPF.Models.CmdrJournal
{
    /// <summary>
    ///     Provides a common interface for all CmdrLog entries.
    /// </summary>
    [JsonObject]
    public interface ICmdrJournalEntry
    {
        /// <summary>
        ///     Time the event occured
        /// </summary>
        [JsonProperty("timestamp")]
        DateTime Timestamp { get; set; }

        /// <summary>
        ///     Name of the event as seen in the cmdr log.
        /// </summary>
        [JsonProperty("event")]
        string Event { get; set; }
    }
}