using System;

namespace RatTracker_WPF.Models.CmdrLog
{
    public class InterdictedLog : ICmdrJournalEntry
    {
        /// <summary>
        ///     Whether the player submitted to the interdiction.
        /// </summary>
        public bool Submitted { get; set; }

        /// <summary>
        ///     Name of the interdictor
        /// </summary>
        public string Interdictor { get; set; }

        /// <summary>
        ///     If interdictor is a NPC, the npc's minor faction affiliation.
        /// </summary>
        public string Faction { get; set; }

        /// <summary>
        ///     If interdictor is a NPC working for a power, the npc's major faction affiliation.
        /// </summary>
        public string Power { get; set; }

        /// <summary>
        ///     Whether the interdictor is a player.
        /// </summary>
        public bool IsPlayer { get; set; }

        /// <summary>
        ///     If interdictor is a player, combat rank of the interdicting player.
        /// </summary>
        public int CombatRank { get; set; }

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