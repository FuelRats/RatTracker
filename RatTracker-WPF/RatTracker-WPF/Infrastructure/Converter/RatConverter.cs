using System;
using System.Globalization;
using System.Windows.Data;

namespace RatTracker_WPF.Infrastructure.Converter
{
  /// <summary>
  ///   Converts rat ids to rat names using a global rat id to rat cache.
  /// </summary>
  public class RatConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      //var myrats = value as List<string>;
      //if (myrats == null)
      //{
      //  return "I am a null rat.";
      //}

      //var matchedRats = MainWindow.Rats.Where(x => myrats.Contains(x.Key));
      //var missingRats = myrats.Except(matchedRats.Select(x => x.Value.id));
      //foreach (var missingRat in missingRats)
      //{
      //  Console.WriteLine("Cannot find rat '" + missingRat + "'");
      //}

      //var ratNames = matchedRats.Select(x => x.Value.CmdrName);

      //var rats = string.Join(", ", ratNames);
      //var index = rats.IndexOf(", ", StringComparison.Ordinal);

      //if (index > 0)
      //{
      //  var firstPart = rats.Substring(0, index);
      //  var secondPart = rats.Substring(index + 2);

      //  rats = firstPart + " and " + secondPart;
      //}

      //return rats;
      return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      return "I am a negative rat";
    }
  }
}