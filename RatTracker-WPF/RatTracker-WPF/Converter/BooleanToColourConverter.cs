using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using RatTracker_WPF.Models;

namespace RatTracker_WPF.Converter
{
	public class BooleanToColourConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			bool state = value as bool? ?? false;
			Brush result = state ? MainWindow.RatStatusColourPositive : MainWindow.RatStatusColourNegative;
			return result;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return null;
		}
	}
}