using Noemax.WebSockets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Noemax.PowerWebSockets.Sample
{
    public static class MyServer
    {
        private static WebSocketServer _webSocketServer;

        public static void Start()
        {
            _webSocketServer = new WebSocketServer();

            // Enable standard permessage-deflate WebSocket extension for payload compression.
            // This extension will be enable for those client connections that support standard payload compression.
            _webSocketServer.Compression.CompressionScheme = CompressionScheme.Auto;

            // Add service endpoints
            _webSocketServer.AddEndpoint<MyBasicService>("http://0.0.0.0:10000/myService1");
            _webSocketServer.AddEndpoint<MyJsonMessagingService>("http://0.0.0.0:10000/myService2");

            // Open the server and start listening on all endpoints
            _webSocketServer.Open();
        }

        public static void Stop()
        {
            if (_webSocketServer != null)
            {
                _webSocketServer.Close();
                _webSocketServer = null;
            }
        }
    }
}
