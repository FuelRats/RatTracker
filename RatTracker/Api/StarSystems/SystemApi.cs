using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RatTracker.Models.Apis.Systems;

namespace RatTracker.Api.StarSystems
{
  public class SystemApi
  {
    private const string SystemApiUrl = "https://system.api.fuelrats.com/";
    private const string EndpointNearest = "nearest";
    private const string EndpointSystems = "systems";

    public async Task<StarSystem> GetNearestPopulatedSystemAsync(double x, double y, double z, char landingPadSize)
    {
      var response = await QuerySystemApi(EndpointNearest,
        $"x={x.ToString(CultureInfo.InvariantCulture)}&y={y.ToString(CultureInfo.InvariantCulture)}&z={z.ToString(CultureInfo.InvariantCulture)}&limit=5");
      var candidates = JsonConvert.DeserializeObject<Response>(response).Candidates;

      foreach (var candidate in candidates.OrderBy(c => c.Distance))
      {
        var system = await GetSystemByNameAsync(candidate.Name);
        system.Distance = candidate.Distance;

        if (landingPadSize == 'L')
        {
          if (system.Bodies.SelectMany(b => b.Stations).Any(s => s.MaxLandingPadSize == landingPadSize))
          {
            return system;
          }
        }
        else if (system.Bodies.SelectMany(b => b.Stations).Any(s => s.MaxLandingPadSize != 'N'))
        {
          return system;
        }
      }

      return null;
    }

    public async Task<StarSystem> GetSystemByNameAsync(string name)
    {
      name = WebUtility.UrlEncode(name.ToUpper());
      var repsonse = await QuerySystemApi(EndpointSystems, $"filter[name:eq]={name}&include=bodies.stations");
      var starSystems = JsonApi.Deserialize<StarSystem[]>(repsonse);
      return starSystems.SingleOrDefault();
    }

    private static async Task<string> QuerySystemApi(string endpoint, string parameters)
    {
      using (var httpClient = new HttpClient())
      {
        var uri = $"{SystemApiUrl}{endpoint}?{parameters}";
        var request = new HttpRequestMessage(HttpMethod.Get, uri);
        var responseMessage = await httpClient.SendAsync(request);
        responseMessage.EnsureSuccessStatusCode();
        var response = await responseMessage.Content.ReadAsStringAsync();
        return response;
      }
    }
  }
}