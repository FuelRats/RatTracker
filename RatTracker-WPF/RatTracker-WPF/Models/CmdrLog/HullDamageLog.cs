using System;

namespace RatTracker_WPF.Models.CmdrLog
{
    public class HullDamageLog : ICmdrJournalEntry
    {
        /// <summary>
        ///     Current health of the player;
        /// </summary>
        public double Health { get; set; }

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