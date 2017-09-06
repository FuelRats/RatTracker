using System;

namespace RatTracker_WPF.Models.Api.V2
{
  public abstract class ModelBase
  {
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
  }
}