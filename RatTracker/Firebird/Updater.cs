using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using CsvHelper;
using Newtonsoft.Json;
using RatTracker.Models.App.StarSystems;

namespace RatTracker.Firebird
{
  public class Updater
  {
    private const string Orthanc = "http://orthanc.localecho.net/json/";
    private readonly StarSystemDatabase starSystemDatabase;
    private readonly string[] systemFiles = {"xaa", "xab", "xac", "xad", "xae", "xaf", "xag", "xah", "xai", "xaj", "xak", "xal", "xam", "xan", "xao"};

    public Updater(StarSystemDatabase starSystemDatabase)
    {
      this.starSystemDatabase = starSystemDatabase;
    }

    public async Task EnsureDatabase()
    {
      var exists = File.Exists(starSystemDatabase.DatabaseFullPath);
      await starSystemDatabase.Initialize();

      if (exists) { return; }

      await DownloadSystems();
      await DownloadStations();
    }

    private async Task DownloadSystems()
    {
      var first = true;
      using (var client = new HttpClient())
      {
        foreach (var file in systemFiles)
        {
          var systems = new BlockingCollection<EddbSystem>(new ConcurrentQueue<EddbSystem>());
          var inserterTask = Task.Run(() => starSystemDatabase.Insert(systems));

          var url = Orthanc + file;
          using (var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
          {
            using (var stream = await response.Content.ReadAsStreamAsync())
            {
              using (var streamReader = new StreamReader(stream))
              {
                var csvReader = new CsvReader(streamReader, true);
                if (first)
                {
                  await csvReader.ReadAsync();
                  csvReader.ReadHeader();
                  first = false;
                }

                while (await csvReader.ReadAsync())
                {
                  var system = new EddbSystem(long.Parse(csvReader.GetField(0)))
                  {
                    Name = csvReader.GetField(2),
                    X = double.Parse(csvReader.GetField(3), CultureInfo.InvariantCulture),
                    Y = double.Parse(csvReader.GetField(4), CultureInfo.InvariantCulture),
                    Z = double.Parse(csvReader.GetField(5), CultureInfo.InvariantCulture),
                    Population = string.IsNullOrWhiteSpace(csvReader.GetField(6)) ? 0L : long.Parse(csvReader.GetField(6)),
                    NeedsPermit = csvReader.GetField(21) != "0" && (csvReader.GetField(21) == "1" || bool.Parse(csvReader.GetField(21))),
                    UpdatedAt = long.Parse(csvReader.GetField(22))
                  };

                  systems.Add(system);
                }
              }
            }
          }

          systems.CompleteAdding();
          await inserterTask;
        }
      }
    }

    private async Task DownloadStations()
    {
      var stations = new BlockingCollection<EddbStation>(new ConcurrentQueue<EddbStation>());
      var inserterTask = Task.Run(() => starSystemDatabase.Insert(stations));

      var file = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "RatTracker", "EDDB", "stations.json");
      using (var fileStream = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.Read))
      {
        var serializer = new JsonSerializer();
        using (var sr = new StreamReader(fileStream))
        {
          using (JsonReader reader = new JsonTextReader(sr))
          {
            while (await reader.ReadAsync())
            {
              // deserialize only when there's "{" character in the stream
              if (reader.TokenType == JsonToken.StartObject)
              {
                var station = serializer.Deserialize<EddbStation>(reader);
                stations.Add(station);
              }
            }
          }
        }
      }
      stations.CompleteAdding();
      await inserterTask;
    }
  }
}