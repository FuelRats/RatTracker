namespace RatTracker_WPF.Models.Api
{
  public class Rescue
  {
    public bool Active { get; set; }
    public bool CodeRed { get; set; }
    public string CmdrName { get; set; }
    public int CreatedAt { get; set; }
    public bool Epic { get; set; }
    public bool Open { get; set; }
    public int updatedAt { get; set; }
    public string Notes { get; set; }
    public string[] Rats { get; set; }
    public bool Successful { get; set; }
    public string System { get; set; }

    // ReSharper disable once InconsistentNaming
    public string id { get; set; }

    public int Score { get; set; }
  }
}