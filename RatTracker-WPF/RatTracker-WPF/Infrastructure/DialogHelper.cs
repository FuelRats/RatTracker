using System.Windows;

namespace RatTracker_WPF.Infrastructure
{
  public class DialogHelper
  {
    public static void ShowWarning(string text)
    {
      MessageBox.Show(text, "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
    }
  }
}