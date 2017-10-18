using System.Xml.Linq;

namespace RatTracker_WPF.Models.NetLog
{
  public class Friend
  {
    public Friend(XContainer friend)
    {
      Name = GetElementValue(friend, "Name");
      Location = GetElementValue(friend, "lastLocation");
      CanWing = GetElementValue(friend, "inviteToWing");
      PrivGroup = GetElementValue(friend, "privateGroup_id");
    }

    public string Name { get; }
    public string Location { get; }
    public string PrivGroup { get; }
    public string CanWing { get; }

    private static string GetElementValue(XContainer element, string name)
    {
      return element?.Element(name) == null ? string.Empty : element.Element(name).Value;
    }
  }
}