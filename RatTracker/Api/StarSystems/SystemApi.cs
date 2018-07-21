using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using log4net;
using Newtonsoft.Json;
using RatTracker.Models.Apis.Systems;

namespace RatTracker.Api.StarSystems
{
  public class SystemApi
  {
    private const string SystemApiUrl = "https://system.api.fuelrats.com/";
    private const string EndpointNearest = "nearest";
    private const string EndpointSystems = "systems";
    private readonly ILog log;

    public SystemApi(ILog log)
    {
      this.log = log;
    }

    public async Task<StarSystem> GetNearestPopulatedSystemAsync(double x, double y, double z, char landingPadSize)
    {
      var response = await QuerySystemApi(EndpointNearest,
        $"x={x.ToString(CultureInfo.InvariantCulture)}&y={y.ToString(CultureInfo.InvariantCulture)}&z={z.ToString(CultureInfo.InvariantCulture)}&limit=5");

      if (string.IsNullOrWhiteSpace(response)) { return null; }

      var candidates = JsonConvert.DeserializeObject<Response>(response).Candidates;

      foreach (var candidate in candidates.OrderBy(c => c.Distance))
      {
        var system = await GetSystemByNameAsync(candidate.Name);
        if (system == null) { continue; }

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
      var response = await QuerySystemApi(EndpointSystems, $"filter[name:eq]={name}&include=bodies.stations");
      if (string.IsNullOrWhiteSpace(response)) { return null; }

      var starSystems = JsonApi.Deserialize<StarSystem[]>(response);
      return starSystems.SingleOrDefault();
    }

    private async Task<string> QuerySystemApi(string endpoint, string parameters)
    {
      using (var httpClient = new HttpClient())
      {
        var uri = $"{SystemApiUrl}{endpoint}?{parameters}";
        log.Debug($"Querying system API: '{uri}'");
        var request = new HttpRequestMessage(HttpMethod.Get, uri);
        try
        {
          var responseMessage = await httpClient.SendAsync(request);
          responseMessage.EnsureSuccessStatusCode();
          var response = await responseMessage.Content.ReadAsStringAsync();
          log.Debug($"Received system API response: '{response}'");
          return response;
        }
        catch (Exception e)
        {
          log.Error("Error querying system API: ", e);
          return null;
        }
      }
    }
  }
}