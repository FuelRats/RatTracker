using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RatTracker_WPF.Models.App
{
  public class PropertyChangedBase : INotifyPropertyChanged
  {
    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
    {
      var onPropertyChanged = PropertyChanged;
      onPropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
  }
}