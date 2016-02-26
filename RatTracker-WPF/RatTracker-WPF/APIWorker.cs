using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.Web;
using System.Net.Http;
using System.Diagnostics;
using WebSocket4Net;
using Newtonsoft.Json;
using ErrorEventArgs = SuperSocket.ClientEngine.ErrorEventArgs;
using System.Net;
using log4net;
using RatTracker_WPF.Models;
using RatTracker_WPF.Models.Api;

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
        private static readonly ILog logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private bool stopping = false;
        public WebSocket ws;
        /*
         * queryAPI sends a GET request to the API. Kindasorta deprecated behavior.
         */
        public void InitWs()
        {
            if (Thread.CurrentThread.Name == null)
            {
                Thread.CurrentThread.Name = "APIWorker";
            }

            try
            {
                string wsurl = "ws://orthanc.localecho.net:7070/"; //TODO: Remove this hardcoding!
                //string wsurl = "ws://dev.api.fuelrats.com/";
                logger.Info("Connecting to WS at " + wsurl);
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
                logger.Fatal("Well, that went tits up real fast: " + ex.Message);
            }
        }
        public void OpenWs()
        {
            if (ws == null)
            {
                logger.Debug("Attempted to open uninitialized WS connection.");
                return;
            }
            ws.Open();
            logger.Debug("WS client is " + ws.State);
        }

        public void DisconnectWs()
        {
            stopping = true;
            if(ws!=null)
                ws.Close();
            return;
        }
        public void SendWs(string action, IDictionary<string, string> data)
        {
            if (ws == null)
            {
                logger.Debug("Attempt to send data over uninitialized WS connection!");
                return;
            }
            switch (action)
            {
                case "rescues:read":
                    logger.Debug("SendWS is requesting a rescues update.");
                    break;
                default:
                    break;

            }
            APIQuery myquery = new APIQuery();
            myquery.action = action;
            myquery.data = data;
            string json = JsonConvert.SerializeObject(myquery);
            logger.Debug("sendWS Serialized to: " + json);
            ws.Send(json);
        }
        public void SendQuery(APIQuery myquery)
        {
            string json = JsonConvert.SerializeObject(myquery);
            logger.Debug("Sent an APIQuery serialized as: " + json);
            ws.Send(json);
        }
        public void SendTPAMessage(TPAMessage message)
        {
            if (ws == null)
            {
                logger.Debug("Attempt to send TPA message over uninitialized WS connection!");
                return;
            }
            message.data.Add("platform", "PC");
            message.applicationId = Properties.Settings.Default.AppID;
            string json = JsonConvert.SerializeObject(message);
            logger.Debug("Serialized TPA data: " + json);
            ws.Send(json);
        }
        private void websocket_Client_Closed(object sender, EventArgs e)
        {
            if (stopping == true)
                logger.Info("Disconnected from API WS server, stopping...");
            else {
                logger.Info("API WS Connection closed unexpectedly. Reconnecting...");
                OpenWs();
            }
        }

        private void websocketClient_MessageReceieved(object sender, MessageReceivedEventArgs e)
        {
            /* We can let this go away fairly soon, since there's not much we can do in the API Worker anyways
             * at this point. Maybe attach a logger here?
             */
            dynamic data = JsonConvert.DeserializeObject(e.Message);
            //TODO: Implement error handling.
            if(data.errors!= null)
            {
                logger.Fatal("API error! " + data.data);
                return;
            }
            //TODO: Implement actual pass to our 3PA logic.
            if(data.application != null)
            {
                logger.Debug("Got an application message, pass to own parser.");
                return;
            }
            switch ((string)data.action)
            {
                case "welcome":
                    logger.Info("API MOTD: " + data.data);
                    break;
            }

            //appendStatus("Direct parse. Type:" + data.type + " Data:" + data.data);
        }

        public void websocketClient_Error(object sender, ErrorEventArgs e)
        {
            logger.Fatal("Websocket: Exception thrown: ",e.Exception);
        }

        public void websocketClient_Opened(object sender, EventArgs e)
        {
            logger.Info("Websocket: Connection to API established.");
            SubscribeStream("0xDEADBEEF");
			APIQuery login = new APIQuery();
			login.action = "users:read";
			login.data = new Dictionary<string, string>();
			login.data.Add("email", Properties.Settings.Default.APIUsername);
            SendQuery(login);
            //TODO: Put stream subscription messages here when Mecha goes live. Do we want to listen to ourselves?
        }
		public void SubscribeStream(string applicationId)
		{
			Dictionary<string, string> data = new Dictionary<string, string>();
			data.Add("action", "stream:subscribe");
			data.Add("applicationId", applicationId);
			logger.Debug("Subscribing to stream: " + applicationId);
			string json = JsonConvert.SerializeObject(data);
			ws.Send(json);
		}
        public async Task<String> queryAPI(string action, Dictionary<string, string> data)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var content = new UriBuilder(Properties.Settings.Default.APIURL + action+"/");
                    content.Port = Properties.Settings.Default.APIPort;
                    var query = HttpUtility.ParseQueryString(content.Query);
                    if (query==null)
                        return apiGetResponse("{\"Error\", \"No response\"}");
                    foreach (KeyValuePair<string, string> entry in data)
                    {
                        query[entry.Key] = entry.Value;
                    }
                    content.Query = query.ToString();
                    logger.Debug("Built query string:" + content.ToString());
                    var response = await client.GetAsync(content.ToString());
                    //appendStatus("AsyncPost sent.");
                    if (response.IsSuccessStatusCode)
                    {
                        var responseString = await response.Content.ReadAsStringAsync();
                        logger.Debug("Starting response string parse task.");
                        return apiGetResponse(responseString);
                    }
                    else
                    {
                        logger.Debug("HTTP request returned an error:" + response.StatusCode);
                        return "";
                    }
                    
                }
            }
            catch (Exception ex)
            {
                logger.Fatal("Exception in QueryAPI: ", ex);
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
            logger.Debug("SendAPI was called with action" + action);
            try
            {
                using (var client = new HttpClient())
                {
                    var content = new FormUrlEncodedContent(data);
                    var response = await client.PostAsync(Properties.Settings.Default.APIURL + action, content); //TODO: This does not pull port number!
                    logger.Debug("AsyncPost sent.");
                    if (response.IsSuccessStatusCode)
                    {
                        var responseString = await response.Content.ReadAsStringAsync();
                        logger.Debug("Starting response string parse task.");
                        return apiResponse(responseString);
                    }
                    else
                    {
                        logger.Info("HTTP request returned an error:" + response.StatusCode);
                        return "";
                    }
                    //connectWS();
                }
            }
            catch (Exception ex)
            {
                logger.Fatal("Well, that didn't go well. SendAPI exception: ", ex);
                return "";
            }
        }

    }
}
