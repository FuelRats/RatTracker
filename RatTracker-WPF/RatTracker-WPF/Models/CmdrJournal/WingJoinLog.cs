using System;
using Newtonsoft.Json;

namespace RatTracker_WPF.Models.CmdrJournal
{
    [JsonObject]
    public class WingJoinLog : ICmdrJournalEntry
    {
        /// <summary>
        ///     Array of Cmdr names who are also in the wing.
        /// </summary>
        [JsonProperty]
        public string[] Others { get; set; }

        /// <summary>
        ///     Time the event occured.
        /// </summary>
        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; }

        /// <summary>
        ///     Event type name.
        /// </summary>
        [JsonProperty("event")]
        public string Event { get; set; }
    }
}