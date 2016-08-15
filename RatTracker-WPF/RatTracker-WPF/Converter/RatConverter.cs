using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using RatTracker_WPF.Models;
using RatTracker_WPF.Models.Api;

namespace RatTracker_WPF.Converter
{
    /// <summary>
    ///     Converts rat ids to rat names using a global rat id to rat cache.
    /// </summary>
    public class RatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            List<string> myrats = value as List<string>;
            if (myrats == null)
            {
                return "I am a null rat.";
            }

            IEnumerable<KeyValuePair<string, Rat>> matchedRats = MainWindow.Rats.Where(x => myrats.Contains(x.Key));
            IEnumerable<string> missingRats = myrats.Except(matchedRats.Select(x => x.Value.id));
            foreach (string missingRat in missingRats)
            {
                Console.WriteLine("Cannot find rat '" + missingRat + "'");
            }

            IEnumerable<string> ratNames = matchedRats.Select(x => x.Value.CmdrName);

            string rats = string.Join(", ", ratNames);
            int index = rats.IndexOf(", ", StringComparison.Ordinal);

            if (index > 0)
            {
                string firstPart = rats.Substring(0, index);
                string secondPart = rats.Substring(index + 2);

                rats = firstPart + " and " + secondPart;
            }

            return rats;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return "I am a negative rat";
        }
    }
}