using System.Windows;
using RatTracker.ViewModels;

namespace RatTracker.Views
{
  /// <summary>
  ///   Interaktionslogik für AssignedRescueView.xaml
  /// </summary>
  public partial class AssignedRescueView
  {
    private AssignedRescueViewModel viewModel;

    public AssignedRescueView()
    {
      InitializeComponent();
      DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
    {
      if (dependencyPropertyChangedEventArgs.NewValue is AssignedRescueViewModel assignedRescueViewModel)
      {
        viewModel = assignedRescueViewModel;
        DataContextChanged -= OnDataContextChanged;
      }
    }

    private void SetClientNameOnClick(object sender, RoutedEventArgs e)
    {
      viewModel?.SetClientName();
    }

    private void SetSystemNameOnClick(object sender, RoutedEventArgs e)
    {
      viewModel?.SetSystemName();
    }
  }
}