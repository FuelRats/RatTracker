using System;
using Newtonsoft.Json;

namespace RatTracker_WPF.Models.CmdrJournal
{
    [JsonObject]
    public class DiedLog : ICmdrJournalEntry
    {
        /// <summary>
        ///     If a single killer, Name of the killer.
        /// </summary>
        /// <remarks><see cref="KillersList" /> returns an consistant result. Use <see cref="KillersList" /> instead.</remarks>
        [JsonProperty]
        public string KillerName { get; set; }

        /// <summary>
        ///     If a single killer, Ship type the killer was piloting.
        /// </summary>
        /// <remarks><see cref="KillersList" /> returns an consistant result. Use <see cref="KillersList" /> instead.</remarks>
        [JsonProperty]
        public string KillerShip { get; set; }

        /// <summary>
        ///     If a single killer, Combat ranking of the killer.
        /// </summary>
        /// <remarks><see cref="KillersList" /> returns an consistant result. Use <see cref="KillersList" /> instead.</remarks>
        [JsonProperty]
        public string KillerRank { get; set; }

        /// <summary>
        ///     If multiple killers in a wing, an array of <see cref="Killer" />
        /// </summary>
        /// <remarks><see cref="KillersList" /> returns an consistant result. Use <see cref="KillersList" /> instead.</remarks>
        [JsonProperty]
        public Killer[] Killers { get; set; }

        /// <summary>
        ///     Array of killers involved in the death of the player.
        /// </summary>
        [JsonIgnore]
        public Killer[] KillersList => Killers ?? new[] {new Killer {Name = KillerName ?? "", Ship = KillerShip ?? "", Rank = KillerRank ?? ""}};

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

    [JsonObject]
    public class Killer
    {
        /// <summary>
        ///     Name of the killer
        /// </summary>
        [JsonProperty]
        public string Name { get; set; }

        /// <summary>
        ///     Ship type the killer was piloting
        /// </summary>
        [JsonProperty]
        public string Ship { get; set; }

        /// <summary>
        ///     Combat ranking of the killer.
        /// </summary>
        [JsonProperty]
        public string Rank { get; set; }
    }
}