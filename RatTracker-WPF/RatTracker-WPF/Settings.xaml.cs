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
using Ookii.Dialogs.Wpf;
using RatTracker_WPF;

namespace RatTracker_WPF
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class wndSettings : Window
    {
		public wndSettings()
        {
            InitializeComponent();
			PopulateMonitors();
        }

		private void PopulateMonitors()
		{
			IEnumerable<Monitor> monitors = Monitor.AllMonitors;
			foreach (Monitor mymonitor in monitors)
			{
				monitorBox.Items.Add(mymonitor.Name);
			}
			if (Properties.Settings.Default.OverlayMonitor != "")
			{
				monitorBox.SelectedItem = Properties.Settings.Default.OverlayMonitor;
			}
		}
        private void edDirBrowserButton_Click(object sender, RoutedEventArgs e)
        {
            VistaFolderBrowserDialog vfbd = new VistaFolderBrowserDialog();
            vfbd.Description = "Please select your Elite:Dangerous folder (Containing the launcher executable)";
            vfbd.UseDescriptionForTitle = true;
            if ((bool)vfbd.ShowDialog(this))
            {
                Properties.Settings.Default.EDPath = vfbd.SelectedPath;
                Properties.Settings.Default.NetLogPath = vfbd.SelectedPath + @"\Products\elite-dangerous-64\Logs";
            }

        }

        private void edNetLogBrowserButton_Click(object sender, RoutedEventArgs e)
        {
            VistaFolderBrowserDialog vfbd = new VistaFolderBrowserDialog();
            vfbd.Description = "Please select your Elite:Dangerous NetLog folder (Containing NetLog files)";
            vfbd.UseDescriptionForTitle = true;
            if ((bool)vfbd.ShowDialog(this))
                Properties.Settings.Default.NetLogPath= vfbd.SelectedPath;
        }

        private void saveButton_Click(object sender, RoutedEventArgs e)
        {
			float myrange;
			if (float.TryParse(textBox1.Text, out myrange))
				Properties.Settings.Default.JumpRange = myrange;
			else
			{
				MessageBoxResult res =
							MessageBox.Show(
								"Your jump range is not valid. Please specify it as a number with up to two decimal points.");
				return;
			}
			Properties.Settings.Default.Save();
			Nullable<bool> result = true;
			this.DialogResult = result;
            this.Close();
        }

		private void monitorBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			Properties.Settings.Default.OverlayMonitor = monitorBox.SelectedItem.ToString();
		}

		private void textBox1_TextChanged(object sender, TextChangedEventArgs e)
		{
		}

      private void ResetOauth_OnClick(object sender, RoutedEventArgs e)
      {
        Properties.Settings.Default.OAuthToken = null;
        Properties.Settings.Default.OAuthCode = null;
      }
    }
}
