using System;
using System.Collections.Generic;
using System.Linq;
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
		private string _status = "Initializing";
		private List<string> _jsonfiles = new List<string>(){"bubble.json", "alpha_positive.json", "alpha_negative.json", "beta_positive.json", "beta_negative.json",
			"delta_positive.json", "delta_negative.json", "gamma_positive.json", "gamma_negative.json"};
		string _rtPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
		private string _orthancUrl = "http://orthanc.localecho.net/json/";
		public string Status
		{
			get { return _status; }
			set
			{
				_status = value;
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

		public async Task<string> LoadChunkedJson()
		{
			Logger.Info("EDDB has begun loading chunked JSON data.");
			foreach (string jsonchunk in _jsonfiles)
			{
				DateTime filedate = File.Exists(_rtPath + @"\RatTracker\" + jsonchunk)
					? File.GetLastWriteTime(_rtPath + @"\RatTracker\" + jsonchunk)
					: new DateTime(1985, 4, 1);
				if (filedate.AddDays(7) < DateTime.Now)
				{
					using (HttpClient client = new HttpClient(new HttpClientHandler
					{
						AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
					}))
					{
						client.Timeout = TimeSpan.FromMinutes(10);
						UriBuilder content = new UriBuilder(_orthancUrl + jsonchunk);
						Logger.Info("Downloading " + content + "...");
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
					using (StreamReader sr = new StreamReader(_rtPath + @"\RatTracker\"+jsonchunk))
					{
						var loadedfile = sr.ReadLine();
						var temp = JsonConvert.DeserializeObject<List<EddbSystem>>(loadedfile);
						await InjectSystemsToSql(temp);
					}
				}

			
			}
			return "Complete";
		}
		public async Task<string> UpdateEddbData()
		{
			if (_fbworker == null)
			{
				Logger.Debug("No FbWorker reference in UpdateEddbData. Waiting for SQL to spin up...");
				Thread.Sleep(5000);
				await UpdateEddbData();
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
			/*
            DateTime filedate = File.Exists(rtPath + @"\RatTracker\stations.json") ? File.GetLastWriteTime(rtPath + @"\RatTracker\stations.json") : new DateTime(1985,4,1);
            if (filedate.AddDays(7) < DateTime.Now)
            {
                Logger.Info("EDDB cache is older than 7 days, updating...");
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
                        Stations = JsonConvert.DeserializeObject<IEnumerable<EddbStation>>(responseString);
                        Logger.Debug("Deserialized stations: " + eddbStations.Count());
                    }
                    
                    using (
                        HttpClient client =
                            new HttpClient(new HttpClientHandler
                            {
                                AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
                            }))
                    {
	                    client.Timeout = TimeSpan.FromMinutes(10);
                        UriBuilder content = new UriBuilder(EddbUrl + "systems.json") { Port = -1 };
                        Logger.Debug("Downloading " + content);
                        HttpResponseMessage response = await client.GetAsync(content.ToString());
                        response.EnsureSuccessStatusCode();
                        string responseString = await response.Content.ReadAsStringAsync();
                        //AppendStatus("Got response: " + responseString);
                        using (StreamWriter sw = new StreamWriter(rtPath + @"\RatTracker\systems.json"))
                        {
                            await sw.WriteLineAsync(responseString);
                            Logger.Info("Saved systems.json");
                        }
						responseString = null;

                        Systems = JsonConvert.DeserializeObject<IEnumerable<EddbSystem>>(responseString);
	                    IEnumerable<EddbSystem> eddbSystems = Systems as EddbSystem[] ?? Systems.ToArray();
	                    Logger.Info("Deserialized systems: " + eddbSystems.Count()+". Converting to SQL...");
						//await ConvertToSql();
						return "EDDB data downloaded. " + eddbSystems.Count() + " systems and " + eddbStations.Count() + " stations added.";
                    }
                }
                catch (Exception ex)
                {
                    Logger.Fatal("Exception in UpdateEDDBData: ", ex);
                    return "EDDB data download failed!";
                }
            }
            else
            {
				try {

					if (_fbworker.GetSystemCount() > 1)
						return "Cached EDDB data available, using existing SQL database.";
					else
					{
						Logger.Debug("No SQL data, loading to SQL from cached JSON...");
						string loadedfile;
						using (StreamReader sr = new StreamReader(rtPath + @"\RatTracker\stations.json"))
						{
							loadedfile = sr.ReadLine();
						}
						Stations = JsonConvert.DeserializeObject<IEnumerable<EddbStation>>(loadedfile);
						using (StreamReader sr = new StreamReader(rtPath + @"\RatTracker\systems.json"))
						{
							loadedfile = sr.ReadLine();
						}
						Systems = JsonConvert.DeserializeObject<IEnumerable<EddbSystem>>(loadedfile);
						//await ConvertToSql();
						return "Loaded new SQL data from cache!";
					}
					
				}
				catch(Exception ex)
				{
					Logger.Debug("Exception during load EDDB cached data: " + ex.Message);
					return "Failed to load EDDB cache!";
				}
            }
			*/
			await LoadChunkedJson();
			return "Complete.";
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
				SystemCount += systems.Count;
				foreach (EddbSystem system in systems)
				{
					SystemCounter++;
					await _fbworker.AddSystem(system.id, system.name, system.x, system.y, system.z, system.faction, system.population,
						system.government, system.allegiance, system.state, system.security, system.primary_economy, system.power,
						system.power_state, Convert.ToInt32(system.needs_permit), Convert.ToInt32(system.updated_at), system.simbad_ref);
				}
				Logger.Info("Completed injection.");
			}
			catch (Exception ex)
			{
				Logger.Debug("Exception in InjectSystemsToSql: " + ex.Message + "@" + ex.Source);
			}
		}
/*		public async Task ConvertToSql()
		{
			SystemCount = Stations.Count();
			Logger.Debug("Inserting " + StationCount + " stations into SQL.");
			SystemCounter = 0;
			foreach (EddbStation station in Stations)
			{
				SystemCounter++;
				await
					_fbworker.AddStation(station.id, station.name, station.system_id, station.max_landing_pad_size,
						station.distance_to_star, station.faction, station.government, station.allegiance, station.state, station.type_id, station.type,
						station.has_blackmarket, station.has_market, station.has_refuel, station.has_repair, station.has_rearm, station.has_outfitting,
						station.has_shipyard, station.has_docking, station.has_commodities, station.import_commodities, station.export_commodities,
						station.prohibited_commodities, station.economies, station.updated_at, station.shipyard_updated_at, station.outfitting_updated_at,
						station.market_updated_at, station.is_planetary, station.selling_ships, station.selling_modules);
			}
			Logger.Debug("Stations loaded to SQL. Inserting system data.");
			Stations = null;
			SystemCount = Systems.Count();
			Logger.Debug("Attempting to insert "+systemcount+" systems to SQL.");

			SystemCounter = 0;
			foreach (EddbSystem system in Systems)
			{
				SystemCounter++;
				await _fbworker.AddSystem(system.id, system.name, system.x, system.y, system.z, system.faction, system.population,
					system.government, system.allegiance, system.state, system.security, system.primary_economy, system.power,
					system.power_state, Convert.ToInt32(system.needs_permit), Convert.ToInt32(system.updated_at), system.simbad_ref);


			}
			Logger.Debug("Systems loaded to SQL.");
			Systems = null;
			Logger.Debug("Cleared Systems JSON from memory.");
		}
		*/
		public EddbSystem GetSystemById(int id)
		{
			return id < 1 ? new EddbSystem() : Systems.FirstOrDefault(sys => sys.id == id);
		}

		/*
		public EDDBSystem GetNearestSystem(string systemname)
		{
			try
			{
				logger.Debug("Searching for system " + systemname);
				var nearestsystem = systems.Where(mysystem => mysystem.name == systemname).Select(
					system => new
					{
						system.id,
						system.population,
						system.name
					}).OrderBy(mysys => mysys.name).First().id;
				return nearestsystem;
			}
			catch (Exception ex)
			{
				logger.Debug("FAIL!");
				return new EDDBSystem();
			}
		}
		*/
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