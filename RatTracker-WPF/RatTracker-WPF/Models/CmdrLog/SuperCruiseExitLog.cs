using System;

namespace RatTracker_WPF.Models.CmdrLog
{
    public class SuperCruiseExitLog : ICmdrLogEntry
    {
        /// <summary>
        ///     The name of the closest stellar body.
        /// </summary>
        public string Body { get; set; }

        /// <summary>
        ///     type of the closest stellar body.
        /// </summary>
        public string BodyType { get; set; }

        /// <summary>
        ///     Name of the starsystem the player is in.
        /// </summary>
        public string StarSystem { get; set; }

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