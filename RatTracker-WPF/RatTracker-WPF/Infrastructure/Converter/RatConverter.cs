using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using RatTracker_WPF.Models.Api.V2;

namespace RatTracker_WPF.Infrastructure.Converter
{
  /// <summary>
  ///   Converts rat ids to rat names using a global rat id to rat cache.
  /// </summary>
  public class RatConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (!(value is IEnumerable<Rat> rats))
      {
        return "I am a null rat.";
      }
      
      return string.Join(", ", rats.Select(x=>x.Name));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      return "I am a negative rat";
    }
  }
}