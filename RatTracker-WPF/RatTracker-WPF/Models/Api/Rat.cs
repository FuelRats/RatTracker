using System;

namespace RatTracker_WPF.Models.Api
{
  public class Rat
  {
    public string CmdrName { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime updatedAt { get; set; }

    public DateTime Joined { get; set; }

    public string Platform { get; set; }

    // ReSharper disable once InconsistentNaming
    public string id { get; set; }

    public double Score { get; set; }
  }
}