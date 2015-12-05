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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.IO;
using System.Security.Permissions;
using System.Threading;
using System.ComponentModel;

namespace RatTracker_WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        bool onDuty=false;
        string logDirectory="G:\\Frontier\\EDLaunch\\Products\\FORC-FDEV-D-1002\\Logs";
        FileSystemWatcher watcher;
        FileInfo logFile;

        public MainWindow()
        {
            InitializeComponent();
            checkLogDirectory();
        }

        public void appendStatus(string text)
        {
            Console.WriteLine("I just got called for appendStatus!");
            if (statusDisplay.Dispatcher.CheckAccess())
            {
                statusDisplay.Text += "\n" + text;
            }
            else
            {
                statusDisplay.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action<string>(appendStatus), text );
            }

        }
        private void onRenamed(object source, RenamedEventArgs e)
        {
            /* Stop watching the renamed file, look for new onChanged. */
        }
        private void checkLogDirectory()
        {
            if(logDirectory==null | logDirectory == "")
            {
                MessageBox.Show("Error: No log directory is specified, please do so before attempting to go on duty.");
            }
            textBox.Text = logDirectory;
            statusDisplay.Text = "Beginning to watch " + logDirectory + " for changes...";
            if (watcher==null)
            {
                watcher = new FileSystemWatcher();
                watcher.Path = logDirectory;
                watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
                watcher.Filter = "*.log";
                watcher.Changed += new FileSystemEventHandler(onChanged);
                watcher.Created += new FileSystemEventHandler(onChanged);
                watcher.Deleted += new FileSystemEventHandler(onChanged);
                watcher.Renamed += new RenamedEventHandler(onRenamed);
                watcher.EnableRaisingEvents = true;

            }
            DirectoryInfo tempDir=new DirectoryInfo(logDirectory);
            logFile = (from f in tempDir.GetFiles("*.log") orderby f.LastWriteTime descending select f).First();
            appendStatus("Started watching file " + logFile.FullName);
            readLogfile(logFile.FullName);
        }

        private void readLogfile(string logFile)
        {
            try
            {
                using (StreamReader sr = new StreamReader(new FileStream(logFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete)))
                {
                    String line = sr.ReadToEnd();
                    appendStatus(line);
                }
            }
            catch (Exception e)
            {
                appendStatus(e.Message); /* Just a change to make the push to github work as well */

            }

        }
        private void onChanged(object source, FileSystemEventArgs e)
        {
            appendStatus("Filechange: " + e.FullPath + " " + e.ChangeType);
            /* Handle changed events */
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            if (onDuty == false)
            {
                button.Content = "On Duty";
                onDuty = true;
                watcher.EnableRaisingEvents = true;
                statusDisplay.Text += "\nStarted watching for events in netlog.";
            }
            else
            {
                button.Content = "Off Duty";
                onDuty = false;
                watcher.EnableRaisingEvents = false;
                statusDisplay.Text += "\nStopped watching for events in netlog.";
            }
        }

        private void Main_Menu_Click(object sender, RoutedEventArgs e)
        {
            /* Fleh? */
        }
    }
}
