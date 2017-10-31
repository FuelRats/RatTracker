using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using RatTracker.Infrastructure;
using RatTracker.Infrastructure.Events;
using RatTracker.Properties;

namespace RatTracker.Journal
{
  public class JournalReader
  {
    private readonly JournalParser parser;
    private FileSystemWatcher fileSystemWatcher;
    private FileInfo journalFile;
    private Timer timer;

    private long lastPosition;
    private string journalPath;

    public JournalReader(EventBus eventBus, JournalParser parser)
    {
      this.parser = parser;
      eventBus.ApplicationExit += EventBusOnApplicationExit;
      eventBus.SettingsChanged += EventBusOnSettingsChanged;
    }

    public void Initialize()
    {
      journalPath = Settings.Default.JournalDirectory;
      if (string.IsNullOrWhiteSpace(journalPath) || !Directory.Exists(journalPath))
      {
        DialogHelper.ShowWarning("Please set the journal directory in the settings dialog.");
        return;
      }

      if (fileSystemWatcher != null)
      {
        fileSystemWatcher.Created -= FileSystemWatcherOnCreated;
      }

      fileSystemWatcher = new FileSystemWatcher
      {
        Path = journalPath,
        NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.Size,
        Filter = "Journal.*.log"
      };

      fileSystemWatcher.Created += FileSystemWatcherOnCreated;

      timer = new Timer(1000);
      timer.Elapsed += TimerOnElapsed;
      timer.Start();
      fileSystemWatcher.EnableRaisingEvents = true;
    }

    private async void TimerOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
    {
      timer.Stop();

      await Task.Run(() =>
      {
        journalFile = GetLastEditedJournalFile(journalPath, fileSystemWatcher.Filter);
        var fileLength = journalFile.Length;
        var readLength = (int) (fileLength - lastPosition);
        if (readLength < 0)
        {
          readLength = 0;
        }

        using (var fileStream = journalFile.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
          fileStream.Seek(lastPosition, SeekOrigin.Begin);
          var bytes = new byte[readLength];
          var haveRead = 0;
          while (haveRead < readLength)
          {
            haveRead += fileStream.Read(bytes, haveRead, readLength - haveRead);
            fileStream.Seek(lastPosition + haveRead, SeekOrigin.Begin);
          }

          // Convert bytes to string
          var s = Encoding.UTF8.GetString(bytes);
          var lines = s.Split(new[] {Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries);
          foreach (var line in lines)
          {
            parser.Parse(line);
          }
        }

        lastPosition = fileLength;
      });

      timer.Start();
    }

    private FileInfo GetLastEditedJournalFile(string filePath, string filter)
    {
      return new DirectoryInfo(filePath).GetFiles(filter).OrderByDescending(f => f.LastWriteTime).FirstOrDefault();
    }

    private void EventBusOnSettingsChanged(object sender, EventArgs eventArgs)
    {
      Initialize();
    }

    private void EventBusOnApplicationExit(object sender, EventArgs eventArgs)
    {
      fileSystemWatcher.EnableRaisingEvents = false;
      timer.Dispose();
    }

    private void FileSystemWatcherOnCreated(object sender, FileSystemEventArgs args)
    {
      lastPosition = 0;
    }
  }
}