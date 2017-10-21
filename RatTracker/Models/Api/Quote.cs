using System;

namespace RatTracker.Models.Api
{
  public class Quote
  {
    public string Author { get; set; }
    public string LastAuthor { get; set; }
    public string Message { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
  }
}