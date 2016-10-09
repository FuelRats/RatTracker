using System;
using Newtonsoft.Json;

namespace RatTracker_WPF.Models.CmdrJournal
{
    [JsonObject]
    public class InterdictedLog : ICmdrJournalEntry
    {
        /// <summary>
        ///     Whether the player submitted to the interdiction.
        /// </summary>
        [JsonProperty]
        public bool Submitted { get; set; }

        /// <summary>
        ///     Name of the interdictor
        /// </summary>
        [JsonProperty]
        public string Interdictor { get; set; }

        /// <summary>
        ///     If interdictor is a NPC, the npc's minor faction affiliation.
        /// </summary>
        [JsonProperty]
        public string Faction { get; set; }

        /// <summary>
        ///     If interdictor is a NPC working for a power, the npc's major faction affiliation.
        /// </summary>
        [JsonProperty]
        public string Power { get; set; }

        /// <summary>
        ///     Whether the interdictor is a player.
        /// </summary>
        [JsonProperty]
        public bool IsPlayer { get; set; }

        /// <summary>
        ///     If interdictor is a player, combat rank of the interdicting player.
        /// </summary>
        [JsonProperty]
        public int CombatRank { get; set; }

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