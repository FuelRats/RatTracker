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

	    public RatState Self { get; } = new RatState();

	    public RatState Rat2 { get; } = new RatState();

	    public RatState Rat3 { get; } = new RatState();
    }
}