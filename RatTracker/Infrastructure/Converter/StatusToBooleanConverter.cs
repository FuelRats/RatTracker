using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;
using RatTracker.Models.Apis.FuelRats.Rescues;

namespace RatTracker.Infrastructure.Converter
{
  public class StatusToBooleanConverter : MarkupExtension, IValueConverter
  {
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
      return this;
    }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      switch (value)
      {
        case RescueState state:
          switch (state)
          {
            case RescueState.Open:
              return true;
            case RescueState.Inactive:
              return false;
            case RescueState.Closed:
              return false;
            default:
              throw new ArgumentOutOfRangeException();
          }
        default:
          return true;
      }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}