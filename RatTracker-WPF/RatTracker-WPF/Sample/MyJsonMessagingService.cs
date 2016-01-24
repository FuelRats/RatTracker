using Noemax.WebSockets;
using Noemax.WebSockets.Messaging;

namespace Noemax.PowerWebSockets.Sample
{
    /// <summary>
    /// Implement/Override your WebSocket LiteJM JSON messaging service
    /// </summary>
    public class MyJsonMessagingService : WebSocketMessagingService
    {   
        /// <summary>
        /// Handles remote request to the operation with 'helloThere' action identifier.
        /// </summary>
        /// <param name="text">The text sent by the remote endpoint as a parameter.</param>
        /// <returns>The text response from the service.</returns>
        [MessageOperation("helloThere")]
        public string HelloThere(string text)
        {
            return "Reply on '" + text + "'";
        }

        /// <summary>
        /// Handles remote request to the operation with 'goodbye' action identifier.
        /// </summary>
        /// <param name="channel">The channel associated to remote operation, it is injected automatically when remote operation has WebSocketChannel parameter.</param>        
        [MessageOperation("goodbye")]
        public void GoodBye(WebSocketChannel channel)
        {
            // Send one-way message to 'goodbye' action on remote endpoint.
            channel.SendOneWay("goodbye-callback");
        }

        /// <summary>
        /// Handles remote request to the operation with 'goodbye' action identifier.
        /// </summary>
        /// <param name="channel">The channel associated to remote operation, it is injected automatically when remote operation has WebSocketChannel parameter.</param>        
        [MessageOperation("goodbye-callback")]
        public void GoodByeCallback(WebSocketChannel channel)
        {
            channel.Close();
        }       

        #region Channel Management

        /// <summary>
        /// Overrides the service behavior when a WebSocket channel is established.
        /// </summary>
        /// <param name="channel">The channel that was established.</param>
        /// <remarks>
        /// Overriding this method is not necessary unless you need to perform an action when the channel is established.
        /// </remarks>
        public override void OnOpen(WebSocketChannel channel)
        {
            base.OnOpen(channel);
            // Send ping to remote side.
            channel.SendPing();
        }

        /// <summary>
        /// Overrides the service behavior when a WebSocket channel is closed.
        /// </summary>
        /// <param name="channel">The channel being closed.</param>
        /// <param name="statusCode">The status code sent by the remote side in its request to close the connection. </param>
        /// <param name="reason">The text description of status code.</param>
        /// <remarks>
        /// Overriding this method is not necessary unless you need to perform an action when the channel is closed.
        /// </remarks>
        public override void OnClose(WebSocketChannel channel, short statusCode, string reason)
        {
            base.OnClose(channel, statusCode, reason);
        }

        /// <summary>
        /// Overrides the service behavior when a WebSocket channel encounters an exception.
        /// </summary>
        /// <param name="channel">The channel that encountered the exception.</param>
        /// <param name="e">The exception encountered by the channel.</param>
        public override void OnError(WebSocketChannel channel, System.Exception e)
        {
            base.OnError(channel, e);
        }

        #endregion
    }
}
