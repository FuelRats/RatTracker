namespace RatTracker_WPF.Models.Api.V2
{
  public class Data
  {
    public string LangId { get; set; }

    // missing: status (object)
    public string IrcNick { get; set; }

    public int BoardIndex { get; set; }
    //missing markedfordeletion (object)
  }
}