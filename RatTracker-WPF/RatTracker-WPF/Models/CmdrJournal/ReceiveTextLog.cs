using System;
using Newtonsoft.Json;

namespace RatTracker_WPF.Models.CmdrJournal
{
  // ReSharper disable InconsistentNaming
  [JsonObject]
  public class ReceiveTextLog : ICmdrJournalEntry
  {
    /// <summary>
    ///   Entity sending the message.
    /// </summary>
    /// <remarks>For a consistant readable result, use <see cref="FromText" /></remarks>
    [JsonProperty]
    public string From { get; set; }

    /// <summary>
    ///   Under certain cases, the localized name of the entity sending the message.
    /// </summary>
    /// <remarks>For a consistant readable result, use <see cref="FromText" /></remarks>
    [JsonProperty]
    public string From_Localised { get; set; }

    /// <summary>
    ///   Name of the sending entity. Returns a consistant displayable value.
    /// </summary>
    [JsonIgnore]
    public string FromText => From_Localised ?? From;

    /// <summary>
    ///   The message that was recieved
    /// </summary>
    /// <remarks>Use <see cref="MessageText" /> for a consistant displayable value</remarks>
    [JsonProperty]
    public string Message { get; set; }

    /// <summary>
    ///   Under certain cases, the localized displayed message that was displayed.
    /// </summary>
    /// <remarks>Use <see cref="MessageText" /> for a consistant displayable value</remarks>
    [JsonProperty]
    public string Message_Localised { get; set; }

    /// <summary>
    ///   Message sent to the player. Returns a consistant displayable value.
    /// </summary>
    [JsonIgnore]
    public string MessageText => Message_Localised ?? Message;

    /// <summary>
    ///   Channel the message was sent over.
    /// </summary>
    /// <remarks>channels: wing, local, voicechat, friend, player, npc.</remarks>
    [JsonProperty]
    public string Channel { get; set; }

    /// <summary>
    ///   Time the event occured.
    /// </summary>
    [JsonProperty("timestamp")]
    public DateTime Timestamp { get; set; }

    /// <summary>
    ///   Event type name.
    /// </summary>
    [JsonProperty("event")]
    public string Event { get; set; }
  }
}