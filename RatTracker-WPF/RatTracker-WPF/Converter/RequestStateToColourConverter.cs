using System;
using System.Windows.Media;
using System.Globalization;
using System.Windows.Data;
using RatTracker_WPF.Models;
using RatTracker_WPF.Models.App;

namespace RatTracker_WPF.Converter
{
	public class RequestStateToColourConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			RequestState? requestState = value as RequestState?;

			Brush result;

			switch (requestState)
			{
				case RequestState.NotRecieved:
					result = MainWindow.RatStatusColourNegative;
					break;
				case RequestState.Recieved:
					result = MainWindow.RatStatusColourPending;
					break;
				case RequestState.Accepted:
					result = MainWindow.RatStatusColourPositive;
					break;
				case null:
					result=MainWindow.RatStatusColourNegative;
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
	}
}