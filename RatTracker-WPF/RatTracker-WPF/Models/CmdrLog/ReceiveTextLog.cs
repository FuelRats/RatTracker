using System;

namespace RatTracker_WPF.Models.CmdrLog
{
    // ReSharper disable InconsistentNaming
    public class ReceiveTextLog : ICmdrJournalEntry
    {
        /// <summary>
        ///     Entity sending the message.
        /// </summary>
        /// <remarks>For a consistant readable result, use <see cref="FromText" /></remarks>
        public string From { get; set; }

        /// <summary>
        ///     Under certain cases, the localized name of the entity sending the message.
        /// </summary>
        /// <remarks>For a consistant readable result, use <see cref="FromText" /></remarks>
        public string From_Localised { get; set; }

        /// <summary>
        ///     Name of the sending entity. Returns a consistant displayable value.
        /// </summary>
        public string FromText => From_Localised ?? From;

        /// <summary>
        ///     The message that was recieved
        /// </summary>
        /// <remarks>Use <see cref="MessageText" /> for a consistant displayable value</remarks>
        public string Message { get; set; }

        /// <summary>
        ///     Under certain cases, the localized displayed message that was displayed.
        /// </summary>
        public string Message_Localised { get; set; }

        /// <summary>
        ///     Message sent to the player. Returns a consistant displayable value.
        /// </summary>
        public string MessageText => Message_Localised ?? Message;

        /// <summary>
        ///     Channel the message was sent over.
        /// </summary>
        /// <remarks>channels: wing, local, voicechat, friend, player, npc.</remarks>
        public string Channel { get; set; }

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