using System;
using System.Globalization;
using System.Windows.Data;

namespace RatTracker_WPF.Infrastructure.Converter
{
  public class ClientConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      var myclient = value as string;
      return myclient ?? "No client data";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      return "I am a negative client";
    }
  }
}