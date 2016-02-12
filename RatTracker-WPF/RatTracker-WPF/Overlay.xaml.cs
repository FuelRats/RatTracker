using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using RatTracker_WPF.Models.Api;
using RatTracker_WPF.Models.App;

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

		public void Queue_Message(OverlayMessage message, int time)
		{
			InfoLine1Header.Content = message.line1header;
			InfoLine1Body.Content = message.line1content;
			InfoLine2Header.Content = message.line2header;
			InfoLine2Body.Content = message.line2content;
			InfoLine3Header.Content = message.line3header;
			InfoLine3Body.Content = message.line3content;
			InfoLine4Header.Content = message.line4header;
			InfoLine4Body.Content = message.line4content;
		}

		protected virtual void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChangedEventHandler onPropertyChanged = PropertyChanged;
			onPropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		public void SetCurrentClient(ClientInfo client)
		{
			this.ClientInfo = client;
		}
	}
}