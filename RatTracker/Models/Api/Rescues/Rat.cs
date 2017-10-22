namespace RatTracker.Models.Api.Rescues
{
  public class Rat : ModelBase
  {
    public string Name { get; set; }

    // missing data (jsonb)
    // missing joined (datetime)
    public Platform Platform { get; set; }

    // missing userid (guid)
    // missing ships (list of ships)
  }
}