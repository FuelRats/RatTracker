using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;

namespace RatTracker_WPF.Converter
{
	public class BooleanToColourConverter :MarkupExtension, IValueConverter
	{
		public Brush RatStatusColourPositive { get; set; }= MainWindow.RatStatusColourPositive;
		public Brush RatStatusColourNegative { get; set; } = MainWindow.RatStatusColourNegative;

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			bool state = value as bool? ?? false;
			Brush result = state ? RatStatusColourPositive : RatStatusColourNegative;
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