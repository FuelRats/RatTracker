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

namespace RatTracker_WPF
{
    class APIWorker
    {
        static string apiURL = "http://dev.api.fuelrats.com/";
        public async Task<NameValueCollection> queryAPI(string action, List<KeyValuePair<String, String>> data)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var content = new UriBuilder(apiURL + action);
                    content.Port = -1;
                    var query = HttpUtility.ParseQueryString(content.Query);
                    foreach (KeyValuePair<string, string> entry in data)
                    {
                        query[entry.Key] = entry.Value;
                    }
                    content.Query = query.ToString();
                    //appendStatus("Built query string:" + content.ToString());
                    var response = await client.GetAsync(content.ToString());
                    //appendStatus("AsyncPost sent.");
                    response.EnsureSuccessStatusCode();
                    var responseString = await response.Content.ReadAsStringAsync();
                    Console.WriteLine("Starting response string parse task.");
                    return apiGetResponse(responseString);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception in QueryAPI: " + ex.Message);
                return new NameValueCollection();
            }
            /* Again, waiting for Trezy. For now, return a placeholder field. */
        }
        public NameValueCollection apiGetResponse(string data)
        {
            Console.WriteLine("apiGetResponse has string:" + data);
            try {
                NameValueCollection temp = new NameValueCollection();
                object m = JsonConvert.DeserializeObject(data);
                foreach (PropertyDescriptor pd in TypeDescriptor.GetProperties(m)) {
                    string value = pd.GetValue(m).ToString();
                    Console.WriteLine("Add value " + pd.Name + ": " + value);
                    temp.Add(pd.Name, value);
                }

                return temp;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in apiGetResponse:" + ex.Message);
                return new NameValueCollection();
            }
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
        public NameValueCollection apiResponse(string data)
        {
            NameValueCollection temp= new NameValueCollection();
            Console.WriteLine("Task apiResponse processing:" + data);
            object m = JsonConvert.DeserializeObject(data);
            foreach (PropertyDescriptor pd in TypeDescriptor.GetProperties(m))
            {
                string value = pd.GetValue(m).ToString();
                Console.WriteLine("Add value " + pd.Name + ": " + value);
                temp.Add(pd.Name, value);
            }

            return temp;
        }
        public async Task<NameValueCollection> sendAPI(string action, List<KeyValuePair<string, string>> data)
        {
            Console.WriteLine("SendAPI was called with action" + action);
            try
            {
                using (var client = new HttpClient())
                {
                    var content = new FormUrlEncodedContent(data);
                    var response = await client.PostAsync(apiURL + action, content);
                    Console.WriteLine("AsyncPost sent.");
                    response.EnsureSuccessStatusCode();
                    var responseString = await response.Content.ReadAsStringAsync();
                    Console.WriteLine("Starting response string parse task.");
                    return apiResponse(responseString);
                    //connectWS();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Well, that didn't go well. SendAPI exception: " + ex.Message);
                return new NameValueCollection();
            }
        }

    }
}
