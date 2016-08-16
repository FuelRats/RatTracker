using System;
using System.Windows.Input;

namespace RatTracker_WPF
{
	[Serializable]
	public class CustomHotKey : HotKey
	{
		private string name;

		public CustomHotKey(string name, Key key, ModifierKeys modifiers, bool enabled) : base(key, modifiers, enabled)
		{
			Name = name;
		}

		public string Name
		{
			get { return name; }
			set
			{
				if (value != name)
				{
					name = value;
					OnPropertyChanged(name);
				}
			}
		}
	}
}