namespace RatTracker_WPF.Models
{
    public class ClientInfo
    {
        public string ClientName => Rescue.Client.CmdrName;
        public string ClientId { get; set; }
        public string ClientState { get; set; }
        public string ClientIp { get; set; }
        public string SessionId { get; set; }
        public string ClientSystem { get; set; }

        public Datum Rescue { get; set; }

        public RequestState FriendRequest { get; set; }

        public RequestState WingRequest { get; set; }
        public bool Beacon { get; set; }
        public bool InSystem { get; set; }
        public bool InInstance { get; set; }
        public bool Fueled { get; set; }
    }


    /*
    * Redesign ClientInfo class to include a link to the Rescue - probably by referencing the rescue ID.

    * Add flags for FR received/accepted, WR received/acccepted, Beacon, Sys and Instance+ to the ClientInfo class.

    * Link FR, WR, BC, SYS and INST buttons on the UI to data in myClient.
        NOTE THAT SOME OF THESE CAN'T BE BOOLS. 
        FR can be in state "not received", "received", and "accepted".
        
    * It should also have the current ability of clicking the FR/WR/BC/SYS/INST buttons to override the reported state (Back to not recieved, or to accepted).
    */
}