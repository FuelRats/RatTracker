using System;

namespace RatTracker_WPF.Models.CmdrLog
{
    public class InterdictionLog : ICmdrLogEntry
    {
        /// <summary>
        ///     Whether the interdiction was succuessful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        ///     Interdicted pilot's name.
        /// </summary>
        public string Interdicted { get; set; }

        /// <summary>
        ///     Whether the interdicted pilot is a player.
        /// </summary>
        public bool IsPlayer { get; set; }

        /// <summary>
        ///     If interdicted pilot is a player, Combat rank of the interdicted player.
        /// </summary>
        public int CombatRank { get; set; }

        /// <summary>
        ///     If interdicted pilot is a NPC, the npc's minor faction affiliation.
        /// </summary>
        public string Faction { get; set; }

        /// <summary>
        ///     If interdicted pilot is a NPC working for a power, the npc's major faction affiliation.
        /// </summary>
        public string Power { get; set; }

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