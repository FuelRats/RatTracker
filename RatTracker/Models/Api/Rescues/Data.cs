﻿namespace RatTracker.Models.Api.Rescues
{
  public class Data
  {
    public string LangId { get; set; }

    // missing: status (object)
    public string IrcNick { get; set; }

    public int BoardIndex { get; set; }
    //missing markedfordeletion (object)
  }
}