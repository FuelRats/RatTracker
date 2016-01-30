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
        }

        private void edDirBrowserButton_Click(object sender, RoutedEventArgs e)
        {
            VistaFolderBrowserDialog vfbd = new VistaFolderBrowserDialog();
            vfbd.Description = "Please select your Elite:Dangerous folder (Containing the launcher executable)";
            vfbd.UseDescriptionForTitle = true;
            if ((bool)vfbd.ShowDialog(this))
                Properties.Settings.Default.EDPath = vfbd.SelectedPath;

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
            Properties.Settings.Default.Save();
            this.Close();
        }
    }
}
