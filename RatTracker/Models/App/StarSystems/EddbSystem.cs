using Newtonsoft.Json;

namespace RatTracker.Models.App.StarSystems
{
  public class EddbSystem
  {
    public EddbSystem(long id)
    {
      Id = id;
    }

    [JsonProperty("id")]
    public long Id { get; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("x")]
    public double X { get; set; }

    [JsonProperty("y")]
    public double Y { get; set; }

    [JsonProperty("z")]
    public double Z { get; set; }

    [JsonProperty("population")]
    public long? Population { get; set; }

    [JsonProperty("needs_permit")]
    public bool NeedsPermit { get; set; }

    [JsonProperty("updated_at")]
    public long UpdatedAt { get; set; }

    public string UpperCaseName => Name?.ToUpper() ?? string.Empty;

    public override int GetHashCode()
    {
      return Id.GetHashCode();
    }

    public override bool Equals(object obj)
    {
      if (ReferenceEquals(null, obj)) { return false; }
      if (ReferenceEquals(this, obj)) { return true; }
      return obj.GetType() == GetType() && Equals((EddbSystem) obj);
    }

    private bool Equals(EddbSystem other)
    {
      return Id == other.Id;
    }
  }
}