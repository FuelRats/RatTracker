using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    private FileStream fileStream;
    private StreamReader streamReader;

    public JournalReader(EventBus eventBus, JournalParser parser)
    {
      this.parser = parser;
      eventBus.ApplicationExit += EventBusOnApplicationExit;
      eventBus.SettingsChanged += EventBusOnSettingsChanged;
    }

    public void Initialize()
    {
      var journalPath = Settings.Default.JournalDirectory;
      if (string.IsNullOrWhiteSpace(journalPath) || !Directory.Exists(journalPath))
      {
        DialogHelper.ShowWarning("Please set the journal directory in the settings dialog.");
        return;
      }

      if (fileSystemWatcher != null)
      {
        fileSystemWatcher.Changed -= FileSystemWatcherOnChanged;
        fileSystemWatcher.Created -= FileSystemWatcherOnCreated;
      }

      fileSystemWatcher = new FileSystemWatcher
      {
        Path = journalPath,
        NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.Size,
        Filter = "Journal.*.log"
      };

      fileSystemWatcher.Changed += FileSystemWatcherOnChanged;
      fileSystemWatcher.Created += FileSystemWatcherOnCreated;
      fileSystemWatcher.EnableRaisingEvents = true;
      var lastJournalFile = GetLastEditedJournalFile(journalPath, fileSystemWatcher.Filter);
      if (lastJournalFile != null)
      {
        FileSystemWatcherOnChanged(this, new FileSystemEventArgs(WatcherChangeTypes.Changed, journalPath, lastJournalFile));
      }
    }

    private string GetLastEditedJournalFile(string filePath, string filter)
    {
      return new DirectoryInfo(filePath).GetFiles(filter).OrderByDescending(f => f.LastWriteTime).Select(f => f.Name).FirstOrDefault();
    }

    private void EventBusOnSettingsChanged(object sender, EventArgs eventArgs)
    {
      Initialize();
    }

    private void EventBusOnApplicationExit(object sender, EventArgs eventArgs)
    {
      fileSystemWatcher.EnableRaisingEvents = false;
      streamReader?.Close();
      fileStream?.Close();
    }

    private async void FileSystemWatcherOnCreated(object sender, FileSystemEventArgs args)
    {
      await ChangeFile(args.FullPath);
    }

    private async void FileSystemWatcherOnChanged(object sender, FileSystemEventArgs args)
    {
      if (journalFile?.FullName != args.FullPath)
      {
        await ChangeFile(args.FullPath);
      }
      else
      {
        await Read();
      }
    }

    private async Task Read()
    {
      while (!streamReader.EndOfStream)
      {
        var line = await streamReader.ReadLineAsync();
        parser.Parse(line);
      }
    }

    private async Task ChangeFile(string path)
    {
      journalFile = new FileInfo(path);
      fileStream = journalFile.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
      streamReader = new StreamReader(fileStream, Encoding.UTF8, false, 50_000);
      await Read();
    }
  }
}