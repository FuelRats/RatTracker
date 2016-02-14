namespace RatTracker_WPF.Models.App
{
	public class PlayerInfo : PropertyChangedBase
	{
		private string currentSystem;
		private float jumpRange;
		private bool onDuty;
		private bool superCruise;

		public string CurrentSystem
		{
			get { return currentSystem; }
			set
			{
				currentSystem = value;
				NotifyPropertyChanged();
			}
		}

		public bool OnDuty
		{
			get { return onDuty; }
			set
			{
				onDuty = value;
				NotifyPropertyChanged();
			}
		}

		public float JumpRange
		{
			get { return jumpRange; }
			set
			{
				jumpRange = value;
				NotifyPropertyChanged();
			}
		}

		public bool SuperCruise
		{
			get { return superCruise; }
			set
			{
				superCruise = value;
				NotifyPropertyChanged();
			}
		}
	}
}