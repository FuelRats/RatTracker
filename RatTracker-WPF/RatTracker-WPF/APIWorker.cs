using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web;
using System.Net.Http;
using System.Diagnostics;
using WebSocket4Net;
using Newtonsoft.Json;
using ErrorEventArgs = SuperSocket.ClientEngine.ErrorEventArgs;
using System.Net;

namespace RatTracker_WPF
{

    /*
     * APIWorker is the HTTP based API query mechanism for RatTracker. As opposed to the WS version, it
     * hits HTTP endpoints on the API. This is primarily used to asynchronously fetch long JSON without
     * tieing up the WS connection, or if the WS connection is unavailable.
     * (And because Trezy still hasn't made WS do much more than be an echo chamber. See, it's still
     * all his fault. :P )
     */

    class APIWorker
    {
        static string apiURL = Properties.Settings.Default.APIURL; /* To be replaced with Settings property. */
        public WebSocket ws;
        /*
         * queryAPI sends a GET request to the API. Kindasorta deprecated behavior.
         */
        public void InitWs()
        {
            try
            {
                string wsurl = "ws://10.0.0.71:8888/";
                Console.WriteLine("Connecting to WS at " + wsurl);
                ws = new WebSocket(wsurl, "", WebSocketVersion.Rfc6455);
                ws.AllowUnstrustedCertificate = true;
                ws.Error += websocketClient_Error;
                ws.Opened += websocketClient_Opened;
                ws.MessageReceived += websocketClient_MessageReceieved;
                ws.Closed += websocket_Client_Closed;
                ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Well, that went tits up real fast: " + ex.Message);
            }
        }
        public void OpenWs()
        {
            ws.Open();

            Console.WriteLine("WS client is " + ws.State);
        }

        public void SendWs(string action, IDictionary<string, string> data)
        {
            data.Add("action", action);
            string json = JsonConvert.SerializeObject(data);
            Console.WriteLine("sendWS Serialized to: " + json);
            ws.Send(json);
        }

        private void websocket_Client_Closed(object sender, EventArgs e)
        {
            Console.WriteLine("API WS Connection closed. Reconnecting...");
            OpenWs();
        }

        private void websocketClient_MessageReceieved(object sender, MessageReceivedEventArgs e)
        {
            /* We can let this go away fairly soon, since there's not much we can do in the API Worker anyways
             * at this point. Maybe attach a logger here?
             */
            dynamic data = JsonConvert.DeserializeObject(e.Message);
            switch ((string)data.type)
            {
                case "welcome":
                    Console.WriteLine("API MOTD: " + data.data);
                    break;
                case "assignment":
                    Console.WriteLine("Got a new assignment datafield: " + data.data);
                    break;
                default:
                    Console.WriteLine("Unknown API type field: " + data.type + ": " + data.data);
                    break;
            }

            //appendStatus("Direct parse. Type:" + data.type + " Data:" + data.data);
        }

        public void websocketClient_Error(object sender, ErrorEventArgs e)
        {
            Console.WriteLine("Websocket: Exception thrown: " + e.Exception.Message);
        }

        public void websocketClient_Opened(object sender, EventArgs e)
        {
            Console.WriteLine("Websocket: Connection to API established.");
            string message = JsonConvert.SerializeObject(new { cmd = "message", msg = "Fukken message" },
                new JsonSerializerSettings { Formatting = Formatting.None });
            ws.Send(message);
            IDictionary<string, string> data = new Dictionary<string, string>();
            data.Add("Foo", "bar");
            SendWs("assignment", data);
        }

        public async Task<String> queryAPI(string action, Dictionary<string, string> data)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var content = new UriBuilder(apiURL + action+"/");
                    content.Port = Properties.Settings.Default.APIPort;
                    var query = HttpUtility.ParseQueryString(content.Query);
                    if (query==null)
                        return apiGetResponse("{\"Error\", \"No response\"}");
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
         * REDONE: Now returns the response as string, process in main.
         */
        public async Task<string> sendAPI(string action, List<KeyValuePair<string, string>> data)
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
                        return "";
                    }
                    //connectWS();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Well, that didn't go well. SendAPI exception: " + ex.Message);
                return "";
            }
        }

    }
}
