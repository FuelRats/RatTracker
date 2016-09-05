using System;
using System.Globalization;
using System.Windows.Data;
using RatTracker_WPF.Models.Api;

namespace RatTracker_WPF.Converter
{
	public class ClientConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			Console.WriteLine("Typeof value is " + value.GetType() + " and is " + value);
			string myclient = value as string;
			return myclient ?? "No client data";
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return "I am a negative client";
		}
	}
}