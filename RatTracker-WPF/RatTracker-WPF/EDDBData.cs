using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Net.Http;
using RatTracker_WPF.Models.EDDB;
using RatTracker_WPF.Models.Edsm;
using Newtonsoft.Json;
namespace RatTracker_WPF
{
    class EDDBData
    {
        private readonly string EDDBUrl = "http://10.0.0.71/eddb/";
        public IEnumerable<EDDBStation> stations;
        public IEnumerable<EDDBSystem> systems;
        public async Task<string> UpdateEDDBData()
        {
            try
            {
                using (HttpClient client = new HttpClient( new HttpClientHandler { AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate }))
                {
                    UriBuilder content = new UriBuilder(EDDBUrl + "stations.json") { Port = -1 };
                    Console.WriteLine("Downloading " + content.ToString());
                    HttpResponseMessage response = await client.GetAsync(content.ToString());
                    response.EnsureSuccessStatusCode();
                    string responseString = await response.Content.ReadAsStringAsync();
                    //AppendStatus("Got response: " + responseString);
                    stations = JsonConvert.DeserializeObject<IEnumerable<EDDBStation>>(responseString);
                    Console.WriteLine("Deserialized stations: " + stations.Count());

                }
                using (HttpClient client = new HttpClient(new HttpClientHandler { AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate }))
                {
                    UriBuilder content = new UriBuilder(EDDBUrl + "systems.json") { Port = -1 };
                    Console.WriteLine("Downloading " + content.ToString());
                    HttpResponseMessage response = await client.GetAsync(content.ToString());
                    response.EnsureSuccessStatusCode();
                    string responseString = await response.Content.ReadAsStringAsync();
                    //AppendStatus("Got response: " + responseString);
                    systems = JsonConvert.DeserializeObject<IEnumerable<EDDBSystem>>(responseString);
                    Console.WriteLine("Deserialized systems: " + systems.Count());
                    return "EDDB data downloaded. "+systems.Count()+" systems and "+stations.Count()+" stations added.";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception in UpdateEDDBData: " + ex.Message);
                return "EDDB data download failed!";
            }
        }
        public async Task<EDDBStation> GetClosestStation(EdsmCoords coords)
        {
            double myx = coords.X;
            double myy = coords.Y;
            double myz = coords.Z;
            //EDDBStation mystation = stations.Where(x => Math.Pow((coords.X - x.)
            return new EDDBStation();
        }
        private double GetDistance(EdsmCoords coords, double EDDBx, double EDDBy, double EDDBz)
        {
            double deltaX = coords.X - EDDBx;
            double deltaY = coords.Y - EDDBy;
            double deltaZ = coords.Z - EDDBz;
            double distance = (double)Math.Sqrt(deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ);
            return distance;
        }
    }

    }


