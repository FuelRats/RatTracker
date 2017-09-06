using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace RatTracker_WPF.Converter
{
  public class BooleanGridHeightConverter : MarkupExtension, IValueConverter
  {
    public string TrueValue { get; set; } = "1*";

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      return TrueValue;
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