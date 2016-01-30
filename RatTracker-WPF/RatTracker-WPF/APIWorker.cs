using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Net.Http;
using System.Diagnostics;
using Newtonsoft.Json;
using System.Data;

namespace RatTracker_WPF
{

    /*
     * APIWorker is the HTTP based API query mechanism for RatTracker. As opposed to the WS version, it
     * hits HTTP endpoints on the API. This is primarily used to asynchronously fetch long JSON without
     * tieing up the WS connection, or if the WS connection is unavailable.
     */

    class APIWorker
    {
        static string apiURL = "http://dev.api.fuelrats.com/"; /* To be replaced with Settings property. */

        /*
         * queryAPI sends a GET request to the API. Kindasorta deprecated behavior.
         */
        public async Task<String> queryAPI(string action, Dictionary<string, string> data)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var content = new UriBuilder(apiURL +"/"+ action);
                    content.Port = -1;
                    var query = HttpUtility.ParseQueryString(content.Query);
                    if (query==null)
                        return apiGetResponse("{\"Error\", \"No response\"");
                    foreach (KeyValuePair<string, string> entry in data)
                    {
                        query[entry.Key] = entry.Value;
                    }
                    content.Query = query.ToString();
                    Console.WriteLine("Built query string:" + content.ToString());
                    var response = await client.GetAsync(content.ToString());
                    //appendStatus("AsyncPost sent.");
                    if (response.IsSuccessStatusCode)
                    {
                        var responseString = await response.Content.ReadAsStringAsync();
                        Console.WriteLine("Starting response string parse task.");
                        return apiGetResponse(responseString);
                    }
                    else
                    {
                        Console.WriteLine("HTTP request returned an error:" + response.StatusCode);
                        return "";
                    }
                    
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception in QueryAPI: " + ex.Message);
                return "";
            }
            /* Again, waiting for Trezy. For now, return a placeholder field. */
        }
        /* apiGetResponse is called by queryAPI asynchronously when the response arrives from the
         * server. This is then passed as a NVC to the main class.
         * TODO: Recode to use IDictionary.
         * REDONE: Now returns the JSON itself, do parsing in main.
         */
        public string apiGetResponse(string data)
        {
            //Console.WriteLine("apiGetResponse has string:" + data);
            return data;
        }
        public bool connectAPI()
        {
            /* Connect to the API here. */
            //appendStatus("Connecting to API.");
            return true;
        }
        private void submitPaperwork(string url)
        {
            Process.Start(url);
        }
        public string apiResponse(string data)
        {
            return data;
        }
        /* sendAPI is called from the main class to send POST requests to the API.
         * Primarily used for login, as most of what we need to do is handled through WS.
         */
        public async Task<Object> sendAPI(string action, List<KeyValuePair<string, string>> data)
        {
            Console.WriteLine("SendAPI was called with action" + action);
            try
            {
                using (var client = new HttpClient())
                {
                    var content = new FormUrlEncodedContent(data);
                    var response = await client.PostAsync(apiURL + action, content);
                    Console.WriteLine("AsyncPost sent.");
                    if (response.IsSuccessStatusCode)
                    {
                        var responseString = await response.Content.ReadAsStringAsync();
                        Console.WriteLine("Starting response string parse task.");
                        return apiResponse(responseString);
                    }
                    else
                    {
                        Console.WriteLine("HTTP request returned an error:" + response.StatusCode);
                        return new Object();
                    }
                    //connectWS();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Well, that didn't go well. SendAPI exception: " + ex.Message);
                return new Object();
            }
        }

    }
}
