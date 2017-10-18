using System.Collections.Generic;

namespace RatTracker_WPF.Models.Api.V2
{
  public class Rescue : ModelBase
  {
    public string Client { get; set; }
    public bool CodeRed { get; set; }

    public Data Data { get; set; }

    // missing notes (string)
    public Platform Platform { get; set; }

    public IList<Quote> Quotes { get; set; }
    public RescueState Status { get; set; }
    public string System { get; set; }

    public string Title { get; set; }

    // missing outcome (enum)
    public IList<string> UnidentifiedRats { get; set; }

    public IList<Rat> Rats { get; set; }
    public Rat FirstLimpet { get; set; }
  }
}