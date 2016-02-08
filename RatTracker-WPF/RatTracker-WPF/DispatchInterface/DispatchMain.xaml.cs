using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace RatTracker_WPF.DispatchInterface
{
	/// <summary>
	/// Interaction logic for DispatchMain.xaml
	/// </summary>
	public partial class DispatchMain : Window
	{
		public DispatchMain()
		{
			InitializeComponent();
		}

		private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			Thickness margin = CasesListbox.Margin;
			margin.Right = (Width / 3) * 2;
			CasesListbox.Margin = margin;
		}
	}
}
