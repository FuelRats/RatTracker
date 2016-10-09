using System;
using Newtonsoft.Json;

namespace RatTracker_WPF.Models.CmdrJournal
{
    [JsonObject]
    public class WingAddLog : ICmdrJournalEntry
    {
        /// <summary>
        ///     Name of the CMDR added to the wing.
        /// </summary>
        [JsonProperty]
        public string Name { get; set; }

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