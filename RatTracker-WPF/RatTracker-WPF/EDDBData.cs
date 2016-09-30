using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;
using Newtonsoft.Json;
using RatTracker_WPF.Models.Edsm;
using RatTracker_WPF.Models.Eddb;
using log4net;
using System.IO;
using RatTracker_WPF.Models.App;

namespace RatTracker_WPF
{
	public class EddbData : PropertyChangedBase
	{
		public static string EddbUrl { get; } = "http://eddb.io/archive/v4/";

		private static readonly ILog Logger =
			LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public IEnumerable<EddbStation> Stations;
		public IEnumerable<EddbSystem> Systems;
		private FireBird _fbworker;
		private int _systemcount;
		private string _status = "EDDB: Initializing";
		private List<string> _jsonfiles = new List<string>();
		string _rtPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
		private string _orthancUrl = "http://orthanc.localecho.net/json/";
        private bool _progressVisibility = false;
		public string Status
		{
			get { return _status; }
			set
			{
				_status = value;
				NotifyPropertyChanged();
			}
		}
        public bool ProgressVisibility
        {
            get { return _progressVisibility;  }
            set
            {
                _progressVisibility = value;
                NotifyPropertyChanged();
            }
        }
        public int SystemCount
		{
			get { return _systemcount; }
			set
			{
				_systemcount = value;
				NotifyPropertyChanged();
			}
		}
		private int _systemcounter;
		public int SystemCounter
		{
			get { return _systemcounter; }
			set
			{
				_systemcounter = value;
				NotifyPropertyChanged();
			}
		}
		private int _stationcount;
		public int StationCount
		{
			get { return _stationcount; }
			set
			{
				_stationcount = value;
				NotifyPropertyChanged();
			}
		}
		private int _stationcounter;
		public int StationCounter
		{
			get { return _stationcounter; }
			set
			{
				_stationcounter = value;
				NotifyPropertyChanged();
			}
		}
		public EddbData(ref FireBird fbworker)
		{
			_fbworker = fbworker;
		}

		public void Setworker(ref FireBird fbworker)
		{
			_fbworker = fbworker;
		}

		public async Task<string> LoadChunkedJson(bool forced)
		{
			Logger.Info("EDDB has begun loading chunked JSON data.");
            using (HttpClient client=new HttpClient(new HttpClientHandler { AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate }))
            {
                UriBuilder content = new UriBuilder(_orthancUrl + "sysinfo.json");
                Logger.Info("Downloading sysinfo.json...");
                Status = "Downloading SysInfo";
                HttpResponseMessage response = await client.GetAsync(content.ToString());
                response.EnsureSuccessStatusCode();
                string responseString = await response.Content.ReadAsStringAsync();
                var temp = JsonConvert.DeserializeObject<List<SysInfo>>(responseString);
                SystemCount = 0;
                foreach (SysInfo sys in temp)
                {
                    Logger.Debug("System chunk " + sys.SectorName +" has "+sys.SectorSize+" systems.");
                    _jsonfiles.Add(sys.SectorName);
                    SystemCount += sys.SectorSize;
                }

            }
			foreach (string jsonchunk in _jsonfiles)
			{
				DateTime filedate = File.Exists(_rtPath + @"\RatTracker\" + jsonchunk)
					? File.GetLastWriteTime(_rtPath + @"\RatTracker\" + jsonchunk)
					: new DateTime(1985, 4, 1);
				if (filedate.AddDays(30) < DateTime.Now || forced==true)
				{
					using (HttpClient client = new HttpClient(new HttpClientHandler
					{
						AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
					}))
					{
						client.Timeout = TimeSpan.FromMinutes(120);
						UriBuilder content = new UriBuilder(_orthancUrl + jsonchunk);
						Logger.Info("Downloading " + content + "...");
                        Status = "Downloading "+content;

                        HttpResponseMessage response = await client.GetAsync(content.ToString());
						response.EnsureSuccessStatusCode();
						string responseString = await response.Content.ReadAsStringAsync();
						using (StreamWriter sw = new StreamWriter(_rtPath + @"\RatTracker\" + jsonchunk))
						{
							sw.WriteLine(responseString); // Do not make async, can prevent file from completing before we try to access it for SQLization!
							Logger.Info("Saved " + jsonchunk + ".");
						}
						var temp = JsonConvert.DeserializeObject<List<EddbSystem>>(responseString);
						if (temp == null)
						{
							Logger.Debug("Failed to deserialize " + jsonchunk+". HTTP Response reason: "+response.ReasonPhrase);
						}
						else
						{
							Logger.Debug("Deserialized systems: " + temp.Count);
							await InjectSystemsToSql(temp);
						}
					}
				}
				else
				{
					Logger.Info("Found a recent cached "+jsonchunk+", injecting directly.");
                    Status = "Injecting "+jsonchunk;
                    ProgressVisibility = true;
                    using (StreamReader sr = new StreamReader(_rtPath + @"\RatTracker\"+jsonchunk))
					{
						var loadedfile = sr.ReadLine();
						var temp = JsonConvert.DeserializeObject<List<EddbSystem>>(loadedfile);
						await InjectSystemsToSql(temp);
					}
				}

			
			}
            Status = "Creating Indexes";
            _fbworker.CreateIndexes();
            ProgressVisibility = false;
			return "Complete";
		}
        public async Task<string> UpdateEddbData(bool forced)
        {
            if (_fbworker == null)
            {
                Logger.Debug("No FbWorker reference in UpdateEddbData. Waiting for SQL to spin up...");
                Status = "Waiting for Firebird";
                Thread.Sleep(5000);
                await UpdateEddbData(false);
            }
            else
            {
                Logger.Debug("FbWorker reference has been aquired.");
            }
            string rtPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            if (Thread.CurrentThread.Name == null)
            {
                Thread.CurrentThread.Name = "EDDBWorker";
            }
            if (forced == true)
            {
                Logger.Debug("Forcing a redownload and reinjection of EDDB systems.");
                await LoadChunkedJson(true);
            }
            if (_fbworker.GetSystemCount() < 1)
                await LoadChunkedJson(false);
            DateTime filedate = File.Exists(rtPath + @"\RatTracker\stations.json") ? File.GetLastWriteTime(rtPath + @"\RatTracker\stations.json") : new DateTime(1985, 4, 1);
            if (filedate.AddDays(7) < DateTime.Now)
            {
                Logger.Info("EDDB station cache is older than 7 days, updating...");
                Status = "Downloading Stations";
                try
                {
                    List<EddbStation> eddbStations = new List<EddbStation>();
                    using (
                        HttpClient client =
                            new HttpClient(new HttpClientHandler
                            {
                                AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
                            }))
                    {
                        client.Timeout = TimeSpan.FromMinutes(10);
                        UriBuilder content = new UriBuilder(EddbUrl + "stations.json") { Port = -1 };
                        Logger.Info("Downloading " + content);
                        HttpResponseMessage response = await client.GetAsync(content.ToString());
                        response.EnsureSuccessStatusCode();
                        string responseString = await response.Content.ReadAsStringAsync();
                        //AppendStatus("Got response: " + responseString);
                        using (StreamWriter sw = new StreamWriter(rtPath + @"\RatTracker\stations.json"))
                        {
                            await sw.WriteLineAsync(responseString);
                            Logger.Info("Saved stations.json");
                        }
                        Status = "Deserializing Stations";
                        eddbStations = JsonConvert.DeserializeObject<List<EddbStation>>(responseString);
                        Logger.Debug("Deserialized stations: " + eddbStations.Count());
                        Status = "Injecting Stations";
                        await InjectStationsToSql(eddbStations);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Debug("Exception in Station dechunk: " + ex.Message);
                    Status = "Error during Station dechunk!";
                    return "Failed!";
                }
            }
            Status = "EDDB Ready!";
            return "Complete.";
        }
        public async Task InjectStationsToSql(List<EddbStation> stations)
        {
            if (stations == null)
            {
                Logger.Debug("Null station list passed to InjectStationsToSql!");
                return;
            }
            Logger.Info("Injecting " + stations.Count + "stations to SQL.");
            try
            {
                foreach(EddbStation station in stations)
                {
                    await _fbworker.AddStation(station.id,station.name,station.system_id,station.max_landing_pad_size,station.distance_to_star,station.faction,
                        station.government,station.allegiance,station.state,station.type_id,station.type,station.has_blackmarket,station.has_market,station.has_refuel,
                        station.has_repair,station.has_rearm,station.has_outfitting,station.has_shipyard,station.has_docking,station.has_commodities,station.import_commodities,
                        station.export_commodities,station.prohibited_commodities,station.economies,station.updated_at,station.shipyard_updated_at,station.outfitting_updated_at,
                        station.market_updated_at,station.is_planetary,station.selling_ships,station.selling_modules);
                }
                
                Logger.Info("Completed Station injection");
            }
            catch (Exception ex)
            {
                Logger.Debug("Exception in InjectStationsToSql: " + ex.Message + "@" + ex.Source);
            }
        }
        public async Task InjectSystemsToSql(List<EddbSystem> systems)
		{
			if (systems == null)
			{
				Logger.Debug("Null system list passed to InjectSystemsToSql!");
				return;
			}
			Logger.Info("Injecting " + systems.Count + " systems to SQL.");
			try
			{
                await _fbworker.BulkAddSystem(systems);
                Logger.Info("Completed injection.");
			}
			catch (Exception ex)
			{
				Logger.Debug("Exception in InjectSystemsToSql: " + ex.Message + "@" + ex.Source);
			}
		}
		public EddbSystem GetSystemById(int id)
		{
			return id < 1 ? new EddbSystem() : Systems.FirstOrDefault(sys => sys.id == id);
		}

		public EddbStation GetClosestStation(EdsmCoords coords)
		{
			try {
				Logger.Debug("Calculating closest station to X:" + coords.X + " Y:" + coords.Y + " Z:" + coords.Z);
				var closestSystemId = Systems.Where(system => system.population > 0).Select(
					system =>
						new
						{
							system.id,system.population,
							distance =
								Math.Sqrt(Math.Pow(coords.X - system.x, 2) + Math.Pow(coords.Y - system.y, 2) + Math.Pow(coords.Z - system.z, 2))
						}).OrderBy(x => x.distance).First().id;
				Logger.Debug("Got system " + closestSystemId);
				EddbStation station =
					Stations.Where(st => st.system_id == closestSystemId && st.distance_to_star != null)
						.OrderBy(st => st.distance_to_star)
						.FirstOrDefault();
				if (station != null)
				{
					Logger.Debug("Got station " + station.name);
					return station;
				}
				return new EddbStation();
			}
			catch (Exception ex)
			{
				Logger.Fatal("Exception in GetClosestStation: " + ex.Message);
				return new EddbStation();
			}
		}

	}
}