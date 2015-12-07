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
        long fileOffset =0L;


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
                watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.Size;
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
                    if (fileOffset == 0L)
                    {
                        if (sr.BaseStream.Length > 30000)
                        {
                            sr.BaseStream.Seek(-30000, SeekOrigin.End); /* First peek into the file, rewind a bit and scan from there. */
                        }
                    }
                    else
                    {
                        sr.BaseStream.Seek(this.fileOffset, SeekOrigin.Begin);
                    }

                    string line;
                    while (sr.Peek() != -1)
                    {
                        line = sr.ReadLine();
                        parseLine(line);
                    }
                }


                appendStatus("I should be reading from the log now, but I'm not asynch!");
            }
            catch (Exception ex)
            {
                return;
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
                button.Background = Brushes.Green;
                /* image.Source = new BitmapImage(RatTracker_WPF.Properties.Resources.yellow_light); */
            }
            else
            {
                button.Content = "Off Duty";
                onDuty = false;
                watcher.EnableRaisingEvents = false;
                statusDisplay.Text += "\nStopped watching for events in netlog.";
                button.Background = Brushes.Red;
            }
        }

        private void Main_Menu_Click(object sender, RoutedEventArgs e)
        {
            /* Fleh? */
        }
    }
}
