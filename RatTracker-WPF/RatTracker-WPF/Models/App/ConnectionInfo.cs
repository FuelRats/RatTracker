namespace RatTracker_WPF.Models.App
{
	public class ConnectionInfo : PropertyChangedBase
	{
		public float act1;
		public float act2;
		public string edserver;
		public int flowcontrol;
		public float fragmentationrate;
		public float jitter;
		public float loss;
		public int mtu;
		public NATType natType;
		public string runid;
		public int srtt;
		public bool turnactive;
		public string turnServer;
		public string wanAddress;

		public string WANAddress
		{
			get { return wanAddress; }
			set
			{
				wanAddress = value;
				NotifyPropertyChanged();
			}
		}

		public NATType NATType
		{
			get { return natType; }
			set
			{
				natType = value;
				NotifyPropertyChanged();
			}
		}

		public string TURNServer
		{
			get { return turnServer; }
			set
			{
				turnServer = value;
				NotifyPropertyChanged();
			}
		}

		public string runID
		{
			get { return runid; }
			set
			{
				runid = value;
				NotifyPropertyChanged();
			}
		}

		public int MTU
		{
			get { return mtu; }
			set
			{
				mtu = value;
				NotifyPropertyChanged();
			}
		}

		public float Jitter
		{
			get { return jitter; }
			set
			{
				jitter = value;
				NotifyPropertyChanged();
			}
		}

		public float Loss
		{
			get { return loss; }
			set
			{
				loss = value;
				NotifyPropertyChanged();
			}
		}

		public int Srtt
		{
			get { return srtt; }
			set
			{
				srtt = value;
				NotifyPropertyChanged();
			}
		}

		public float Act1
		{
			get { return act1; }
			set
			{
				act1 = value;
				NotifyPropertyChanged();
			}
		}

		public float Act2
		{
			get { return act2; }
			set
			{
				act2 = value;
				NotifyPropertyChanged();
			}
		}

		public int Flowcontrol
		{
			get { return flowcontrol; }
			set
			{
				flowcontrol = value;
				NotifyPropertyChanged();
			}
		}

		public bool TURNActive
		{
			get { return turnactive; }
			set
			{
				turnactive = value;
				NotifyPropertyChanged();
			}
		}

		public string EDServer
		{
			get { return edserver; }
			set
			{
				edserver = value;
				NotifyPropertyChanged();
			}
		}

		public float FragmentationRate
		{
			get { return fragmentationrate; }
			set
			{
				fragmentationrate = value;
				NotifyPropertyChanged();
			}
		}
	}
}