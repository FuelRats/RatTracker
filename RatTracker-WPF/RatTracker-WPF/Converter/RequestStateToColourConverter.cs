using System;
using System.Windows.Media;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;
using RatTracker_WPF.Models;
using RatTracker_WPF.Models.App;

namespace RatTracker_WPF.Converter
{
	public class RequestStateToColourConverter : MarkupExtension, IValueConverter
	{
		public Brush RatStatusColourNegative { get; set; } = MainWindow.RatStatusColourNegative;
		public Brush RatStatusColourPending { get; set; } = MainWindow.RatStatusColourPending;
		public Brush RatStatusColourPositive { get; set; } = MainWindow.RatStatusColourPositive;

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			RequestState? requestState = value as RequestState?;

			Brush result;

			switch (requestState)
			{
				case RequestState.NotRecieved:
					result = RatStatusColourNegative;
					break;
				case RequestState.Recieved:
					result = RatStatusColourPending;
					break;
				case RequestState.Accepted:
					result = RatStatusColourPositive;
					break;
				case null:
					result=RatStatusColourNegative;
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			return result;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return null;
		}

		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			return this;
		}
	}
}