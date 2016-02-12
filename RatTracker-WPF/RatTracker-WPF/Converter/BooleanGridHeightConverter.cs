using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace RatTracker_WPF.Converter
{
	public class BooleanGridHeightConverter : MarkupExtension, IValueConverter
	{
		public string TrueValue { get; set; }

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			bool state = value as bool? ?? false;

			return "1*";
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