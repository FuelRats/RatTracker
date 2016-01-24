using System;
using Noemax.WebSockets;
using Noemax.WebSockets.Messaging;
using System.Threading;

namespace Noemax.PowerWebSockets.Sample
{
	/*
    You can request assistance or view more samples by visiting the PowerWebSockets home page:
    https://www.noemax.com/powerwebsockets/
    */
    static class RunSample
    {
        static void Run(string[] args)
        {
            MyServer.Start();

            Console.WriteLine("Server started.");

            // Using basic WebSocket API simply exchanging text and binary payload
            var basicClient = new WebSocketClient<MyBasicClientService>("http://localhost:10000/myService1");
            basicClient.OutboundHandshakeHeaders.Origin = "www.sample.com/ws";

            basicClient.Send("Hello there!!!");
            basicClient.Send(Guid.NewGuid().ToByteArray());

            basicClient.Close();

            // Using JSON messaging. We are using MyJsonMessagingService class both on the client and on the server
            // because this service is designed to be run on both ends. Normally depending on message contract you
            // need different WebSocketMessagingService implementations for the client and for the server.
            var jsonClient = new WebSocketClient<MyJsonMessagingService>("http://localhost:10000/myService2");

            // Enable and configure the standard permessage-deflate extension for transparent payload compression.
            // Extensions will be used automatically when target server endpoint supports permessage-deflate extension.
            jsonClient.Compression.CompressionScheme = CompressionScheme.Deflate;
            jsonClient.Compression.CompressionLevel = 6;

            // Perform request/response operation with the method with "helloThere" action 
            Console.WriteLine(  jsonClient.Request<string>("helloThere", "Hello there!!!")  );

            // Send one-way message to the method method with "goodbye" action
            jsonClient.SendOneWay("goodbye");

            // Waiting for a goodbye-callback
            Thread.Sleep(TimeSpan.FromSeconds(1));

            jsonClient.Close();


            Console.WriteLine("Complete! Press ENTER for exit...");
            Console.ReadLine();


            MyServer.Stop();
        }
    }
}
