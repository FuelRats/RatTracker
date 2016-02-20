using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using RatTracker_WPF.Models.Api;
using RatTracker_WPF.Models.App;
using System.Timers;

namespace RatTracker_WPF
{
	/// <summary>
	/// Interaction logic for Overlay.xaml
	/// </summary>
	public partial class Overlay : Window, INotifyPropertyChanged
	{
		private ClientInfo clientInfo;

		public Overlay()
		{
			InitializeComponent();
		}

		public bool IsRescueActive => ClientInfo != null;

		public ClientInfo ClientInfo
		{
			get { return clientInfo; }
			set
			{
				clientInfo = value;
				// ReSharper disable once ExplicitCallerInfoArgument
				NotifyPropertyChanged(nameof(IsRescueActive));
				NotifyPropertyChanged();
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		private void Window_Deactivated(object sender, EventArgs e)
		{
			Window window = (Window) sender;
			window.Topmost = true;
		}

		public async void Queue_Message(OverlayMessage message, int time)
		{
			InfoLine1Header.Content = message.Line1Header;
			InfoLine1Body.Content = message.Line1Content;
			InfoLine2Header.Content = message.Line2Header;
			InfoLine2Body.Content = message.Line2Content;
			InfoLine3Header.Content = message.Line3Header;
			InfoLine3Body.Content = message.Line3Content;
			InfoLine4Header.Content = message.Line4Header;
			InfoLine4Body.Content = message.Line4Content;
            Timer _resetMessageTimer = new Timer(time);
            MessageGrid.Visibility = Visibility.Visible;
		}
        void _resetMessageTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            MessageGrid.Visibility = Visibility.Hidden;
        }
		protected virtual void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChangedEventHandler onPropertyChanged = PropertyChanged;
			onPropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		public void SetCurrentClient(ClientInfo client)
		{
			this.ClientInfo = client;
            if (client.Rat2 == null)
            {
                Rat2Panel.Visibility = Visibility.Hidden;
                Rat2Buttons.Visibility = Visibility.Hidden;
            }
            if (client.Rat3 == null)
            {
                Rat3Panel.Visibility = Visibility.Hidden;
                Rat3Buttons.Visibility = Visibility.Hidden;
            }
		}
	}
}