using System;

namespace RatTracker_WPF.Models.CmdrLog
{
    public class FuelScoopLog : ICmdrJournalEntry
    {
        /// <summary>
        ///     Amount of fuel scooped.
        /// </summary>
        public double Scooped { get; set; }

        /// <summary>
        ///     Total fuel level after scooping.
        /// </summary>
        public double Total { get; set; }

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