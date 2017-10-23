namespace RatTracker_WPF.Models.Eddb
{
  public class EddbSystem
  {
    public int id { get; set; }
    public string name { get; set; }
    public double x { get; set; }
    public double y { get; set; }
    public double z { get; set; }
    public long? population { get; set; }
    public string needs_permit { get; set; }
    public int? updated_at { get; set; }
    public string simbad_ref { get; set; }
  }
}