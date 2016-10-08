using System;

namespace RatTracker_WPF.Models.CmdrLog
{
    public class CommitCrimeLog : ICmdrJournalEntry
    {
        /// <summary>
        ///     Type of crime committed
        /// </summary>
        public string CrimeType { get; set; }

        /// <summary>
        ///     If fine was issued, The faction which issued the fine. Returns nul, otherwise.
        /// </summary>
        public string Faction { get; set; }

        /// <summary>
        ///     If bounty was issued, The name of the npc or Cmdr attacked. Returns null, otherwise.
        /// </summary>
        public string Victim { get; set; }

        /// <summary>
        ///     If fine was issued, the amount of the fine (in CR). Returns null, otherwise.
        /// </summary>
        public int Fine { get; set; }

        /// <summary>
        ///     If bounty was issued, the amount of the bounty (in CR).
        /// </summary>
        public int Bounty { get; set; }

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