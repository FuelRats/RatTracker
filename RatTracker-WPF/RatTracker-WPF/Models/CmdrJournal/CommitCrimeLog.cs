using System;
using Newtonsoft.Json;

namespace RatTracker_WPF.Models.CmdrJournal
{
    [JsonObject]
    public class CommitCrimeLog : ICmdrJournalEntry
    {
        /// <summary>
        ///     Type of crime committed
        /// </summary>
        [JsonProperty]
        public string CrimeType { get; set; }

        /// <summary>
        ///     If fine was issued, The faction which issued the fine. Returns nul, otherwise.
        /// </summary>
        [JsonProperty]
        public string Faction { get; set; }

        /// <summary>
        ///     If bounty was issued, The name of the npc or Cmdr attacked. Returns null, otherwise.
        /// </summary>
        [JsonProperty]
        public string Victim { get; set; }

        /// <summary>
        ///     If fine was issued, the amount of the fine (in CR). Returns null, otherwise.
        /// </summary>
        [JsonProperty]
        public int Fine { get; set; }

        /// <summary>
        ///     If bounty was issued, the amount of the bounty (in CR).
        /// </summary>
        [JsonProperty]
        public int Bounty { get; set; }

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