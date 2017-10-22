using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace RatTracker.Infrastructure.Converter
{
  public class ClientConverter : MarkupExtension, IValueConverter
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

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
      return this;
    }
  }
}