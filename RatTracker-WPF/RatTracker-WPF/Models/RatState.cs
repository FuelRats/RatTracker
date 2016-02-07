namespace RatTracker_WPF.Models
{
	public class RatState : PropertyChangedBase
	{
		private bool beacon;
		private RequestState friendRequest;
		private bool fueled;
		private bool inInstance;
		private bool inSystem;
		private RequestState wingRequest;

		public RequestState FriendRequest
		{
			get { return friendRequest; }
			set
			{
				friendRequest = value;
				NotifyPropertyChanged();
			}
		}

		public RequestState WingRequest
		{
			get { return wingRequest; }
			set
			{
				wingRequest = value;
				NotifyPropertyChanged();
			}
		}

		public bool Beacon
		{
			get { return beacon; }
			set
			{
				beacon = value;
				NotifyPropertyChanged();
			}
		}

		public bool InSystem
		{
			get { return inSystem; }
			set
			{
				inSystem = value;
				NotifyPropertyChanged();
			}
		}

		public bool InInstance
		{
			get { return inInstance; }
			set
			{
				inInstance = value;
				NotifyPropertyChanged();
			}
		}

		public bool Fueled
		{
			get { return fueled; }
			set
			{
				fueled = value;
				NotifyPropertyChanged();
			}
		}
	}
}