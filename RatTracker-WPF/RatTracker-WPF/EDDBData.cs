using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;
using Newtonsoft.Json;
using RatTracker_WPF.Models.Edsm;
using RatTracker_WPF.Models.EDDB;
using log4net;
using System.IO;

namespace RatTracker_WPF
{
	class EDDBData
	{
		private readonly string EDDBUrl = "http://eddb.io/archive/v4/";
        private static readonly ILog logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public IEnumerable<EDDBStation> stations;
		public IEnumerable<EDDBSystem> systems;

		public async Task<string> UpdateEDDBData()
		{
            DateTime filedate;
            string RTPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
            if (Thread.CurrentThread.Name == null)
            {
                Thread.CurrentThread.Name = "EDDBWorker";
            }
            if (File.Exists(RTPath + @"\RatTracker\stations.json")) 
                filedate = File.GetLastWriteTime(RTPath + @"\RatTracker\stations.json");
            else
            {
                filedate=new DateTime(1985,4,1);
            }
            if (filedate.AddDays(7) < DateTime.Now)
            {
                logger.Info("EDDB cache is older than 7 days, updating...");
                try
                {
                    using (
                        HttpClient client =
                            new HttpClient(new HttpClientHandler
                            {
                                AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
                            }))
                    {
                        UriBuilder content = new UriBuilder(EDDBUrl + "stations.json") { Port = -1 };
                        logger.Info("Downloading " + content.ToString());
                        HttpResponseMessage response = await client.GetAsync(content.ToString());
                        response.EnsureSuccessStatusCode();
                        string responseString = await response.Content.ReadAsStringAsync();
                        //AppendStatus("Got response: " + responseString);
                        using (StreamWriter sw = new StreamWriter(RTPath + @"\RatTracker\stations.json"))
                        {
                            await sw.WriteLineAsync(responseString);
                            logger.Info("Saved stations.json");
                        }
                        stations = JsonConvert.DeserializeObject<IEnumerable<EDDBStation>>(responseString);
                        logger.Debug("Deserialized stations: " + stations.Count());
                    }
                    
                    using (
                        HttpClient client =
                            new HttpClient(new HttpClientHandler
                            {
                                AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
                            }))
                    {
                        UriBuilder content = new UriBuilder(EDDBUrl + "systems.json") { Port = -1 };
                        logger.Debug("Downloading " + content.ToString());
                        HttpResponseMessage response = await client.GetAsync(content.ToString());
                        response.EnsureSuccessStatusCode();
                        string responseString = await response.Content.ReadAsStringAsync();
                        //AppendStatus("Got response: " + responseString);
                        using (StreamWriter sw = new StreamWriter(RTPath + @"\RatTracker\systems.json"))
                        {
                            await sw.WriteLineAsync(responseString);
                            logger.Info("Saved systems.json");
                        }

                        systems = JsonConvert.DeserializeObject<IEnumerable<EDDBSystem>>(responseString);
                        logger.Info("Deserialized systems: " + systems.Count());
                        return "EDDB data downloaded. " + systems.Count() + " systems and " + stations.Count() + " stations added.";
                    }
                }
                catch (Exception ex)
                {
                    logger.Fatal("Exception in UpdateEDDBData: ", ex);
                    return "EDDB data download failed!";
                }
            }
            else
            {
                string loadedfile;
                using(StreamReader sr = new StreamReader(RTPath + @"\RatTracker\stations.json"))
                {
                    loadedfile= sr.ReadLine();
                }
                stations = JsonConvert.DeserializeObject<IEnumerable<EDDBStation>>(loadedfile);
                using (StreamReader sr = new StreamReader(RTPath + @"\RatTracker\systems.json"))
                {
                    loadedfile = sr.ReadLine();
                }
                systems = JsonConvert.DeserializeObject<IEnumerable<EDDBSystem>>(loadedfile);
                return "Loaded cached EDDB data. " + systems.Count() + " systems and " + stations.Count() + " stations added.";
            }
		}

		public EDDBSystem GetSystemById(int id)
		{
			if (id<1)
				return new EDDBSystem();
			return systems.Where(sys => sys.id == id).FirstOrDefault();
		}

		public EDDBStation GetClosestStation(EdsmCoords coords)
		{
			try {
				logger.Debug("Calculating closest station to X:" + coords.X + " Y:" + coords.Y + " Z:" + coords.Z);
				var closestSystemId = systems.Where(system => system.population > 0).Select(
					system =>
						new
						{
							system.id,system.population,
							distance =
								Math.Sqrt(Math.Pow(coords.X - system.x, 2) + Math.Pow(coords.Y - system.y, 2) + Math.Pow(coords.Z - system.z, 2))
						}).OrderBy(x => x.distance).First().id;
				logger.Debug("Got system " + closestSystemId);
				EDDBStation station =
					stations.Where(st => st.system_id == closestSystemId && st.distance_to_star != null)
						.OrderBy(st => st.distance_to_star)
						.FirstOrDefault();
				logger.Debug("Got station " + station.name);
				return station;
			}
			catch (Exception ex)
			{
				logger.Fatal("Exception in GetClosestStation: " + ex.Message);
				return new EDDBStation();
			}
		}
	}
}