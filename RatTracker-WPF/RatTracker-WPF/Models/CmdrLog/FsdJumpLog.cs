using System;

namespace RatTracker_WPF.Models.CmdrLog
{
    public class FsdJumpLog : ICmdrJournalEntry
    {
        /// <summary>
        ///     Whether a FSD boost was used.
        /// </summary>
        public bool BoostUsed { get; set; }

        /// <summary>
        ///     Fuel level of the player, post FSD jump.
        /// </summary>
        public double FuelLevel { get; set; }

        /// <summary>
        ///     Fuel used to make the FSD jump.
        /// </summary>
        public double FuelUsed { get; set; }

        /// <summary>
        ///     Distance the player jumped.
        /// </summary>
        public double JumpDist { get; set; }

        /// <summary>
        ///     Array of doubles representing the position of the star on the galactic plane.
        /// </summary>
        /// <remarks>Format: {X, Y, Z}</remarks>
        public double[] StarPos { get; set; }

        /// <summary>
        ///     System's major faction allegiance.
        /// </summary>
        public string Allegiance { get; set; }

        /// <summary>
        ///     Star's body name;
        /// </summary>
        public string Body { get; set; }

        /// <summary>
        ///     System's economy type.
        /// </summary>
        public string Economy { get; set; }

        /// <summary>
        ///     System's controlling minor faction.
        /// </summary>
        public string Faction { get; set; }

        /// <summary>
        ///     System's current economic or political state.
        /// </summary>
        public string FactionState { get; set; }

        /// <summary>
        ///     Controlling government's type.
        /// </summary>
        public string Government { get; set; }

        /// <summary>
        ///     If player is pledged to a power, the system's controlling power.
        /// </summary>
        /// <remarks>Use <see cref="PowersList" /> for consistant single result.</remarks>
        public string Power { get; set; }

        /// <summary>
        ///     If player is pledged to a power, the system's contesting powers.
        /// </summary>
        /// <remarks>Use <see cref="PowersList" /> for consistant single result.</remarks>
        public string[] Powers { get; set; }

        /// <summary>
        ///     if player is pledged to a power, the system's major powers.
        /// </summary>
        public string[] PowersList => Powers ?? new[] {Power};

        /// <summary>
        ///     Security level of the system.
        /// </summary>
        public string Security { get; set; }

        /// <summary>
        ///     Name of the star system.
        /// </summary>
        public string StarSystem { get; set; }

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