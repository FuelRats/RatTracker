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

        public string Name { get; private set; }
        public string Location { get; private set; }
        public string PrivGroup { get; private set; }
        public string CanWing { get; private set; }

        private static string GetElementValue(XContainer element, string name)
        {
            return element?.Element(name) == null ? string.Empty : element.Element(name).Value;
        }
    }
}