using System.Collections.Generic;

namespace RatTracker.Models.Api
{
  public class User : ModelBase
  {
    // missing data (jsonb)
    public string Email { get; set; }
    public Rat DisplayRat { get; set; }
    // missing nicknames (list of strings)
    public IList<Rat> Rats { get; set; }
    // missing groups (list of groups)
  }
}