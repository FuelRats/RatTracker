using System;

namespace RatTracker_WPF.Models.CmdrLog
{
    public class ReceiveTextLog : ICmdrLogEntry
    {
        public string From { get; set; }
        // ReSharper disable once InconsistentNaming
        public string From_Localised { get; set; }
        public string Message { get; set; }
        // ReSharper disable once InconsistentNaming
        public string Message_Localised { get; set; }
        public string Channel { get; set; }

        //to get the usable text. From and Message can be generic variable names if the messages come from NPCs. Kinda annoying.
        public string FromText => From_Localised ?? From;
        public string MessageText => Message_Localised ?? Message;
        public DateTime Timestamp { get; set; }
        public string Event { get; set; }
    }
}