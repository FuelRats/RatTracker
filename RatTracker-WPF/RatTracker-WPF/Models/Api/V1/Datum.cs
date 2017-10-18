using System.Collections.Generic;

namespace RatTracker_WPF.Models.Api.V1
{
  public class Datum
  {
    public bool Active { get; set; }
    public bool Archive { get; set; }
    public string Client { get; set; }
    public bool CodeRed { get; set; }
    public string CreatedAt { get; set; }
    public bool? Epic { get; set; }
    public string LastModified { get; set; }
    public bool Open { get; set; }
    public string Notes { get; set; }
    public string Platform { get; set; }
    public List<string> Quotes { get; set; }
    public List<string> Rats { get; set; }
    public List<string> unidentifiedRats { get; set; }
    public bool? Successful { get; set; }
    public string System { get; set; }

    // ReSharper disable once InconsistentNaming
    public string id { get; set; }

    public float Score { get; set; }
    public string firstLimpet { get; set; }
    public string Title { get; set; }
  }
}