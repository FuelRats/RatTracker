namespace RatTracker_WPF.Models.App
{
	public class ConnectionInfo : PropertyChangedBase
	{
		private float act1;
		private float act2;
		private string edserver;
		private int flowcontrol;
		private float fragmentationrate;
		private float jitter;
		private float loss;
		private int mtu;
		private NatType natType;
		private string runid;
		private int srtt;
		private bool turnactive;
		private string turnServer;
		private string wanAddress;
        private bool portmapped;

        public bool PortMapped
        {
            get { return portmapped; }
            set
            {
                portmapped = value;
                NotifyPropertyChanged();
            }
        }

		public string WanAddress
		{
			get { return wanAddress; }
			set
			{
				wanAddress = value;
				NotifyPropertyChanged();
			}
		}

		public NatType NatType
		{
			get { return natType; }
			set
			{
				natType = value;
				NotifyPropertyChanged();
			}
		}

		public string TurnServer
		{
			get { return turnServer; }
			set
			{
				turnServer = value;
				NotifyPropertyChanged();
			}
		}

		public string RunId
		{
			get { return runid; }
			set
			{
				runid = value;
				NotifyPropertyChanged();
			}
		}

		public int Mtu
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

		public bool TurnActive
		{
			get { return turnactive; }
			set
			{
				turnactive = value;
				NotifyPropertyChanged();
			}
		}

		public string EdServer
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