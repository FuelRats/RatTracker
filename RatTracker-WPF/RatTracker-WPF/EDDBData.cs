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
            if (Thread.CurrentThread.Name == null)
            {
                Thread.CurrentThread.Name = "EDDBWorker";
            }
            try
			{
				using (
					HttpClient client =
						new HttpClient(new HttpClientHandler
						{
							AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
						}))
				{
					UriBuilder content = new UriBuilder(EDDBUrl + "stations.json") {Port = -1};
					logger.Info("Downloading " + content.ToString());
					HttpResponseMessage response = await client.GetAsync(content.ToString());
					response.EnsureSuccessStatusCode();
					string responseString = await response.Content.ReadAsStringAsync();
					//AppendStatus("Got response: " + responseString);
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
					UriBuilder content = new UriBuilder(EDDBUrl + "systems.json") {Port = -1};
					logger.Debug("Downloading " + content.ToString());
					HttpResponseMessage response = await client.GetAsync(content.ToString());
					response.EnsureSuccessStatusCode();
					string responseString = await response.Content.ReadAsStringAsync();
					//AppendStatus("Got response: " + responseString);
					systems = JsonConvert.DeserializeObject<IEnumerable<EDDBSystem>>(responseString);
					logger.Info("Deserialized systems: " + systems.Count());
					return "EDDB data downloaded. " + systems.Count() + " systems and " + stations.Count() + " stations added.";
				}
			}
			catch (Exception ex)
			{
				logger.Fatal("Exception in UpdateEDDBData: ",ex);
				return "EDDB data download failed!";
			}
		}

		public EDDBStation GetClosestStation(EdsmCoords coords)
		{
			var closestSystemId = systems.Select(
				system =>
					new
					{
						system.id,
						distance =
							Math.Sqrt(Math.Pow(coords.X - system.x, 2) + Math.Pow(coords.Y - system.y, 2) + Math.Pow(coords.Z - system.z, 2))
					}).OrderBy(x => x.distance).First().id;

			EDDBStation station =
				stations.Where(st => st.system_id == closestSystemId && st.distance_to_star != null)
					.OrderBy(st => st.distance_to_star)
					.FirstOrDefault();

			return station;
		}
	}
}