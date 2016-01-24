using System;
using Noemax.WebSockets;

namespace Noemax.PowerWebSockets.Sample
{
    /// <summary>
    /// Implement/Override your WebSocket messaging service
    /// </summary>
    public class MyBasicService : WebSocketService
    {
        /// <summary>
        /// Handles inbound binary messages.
        /// </summary>
        /// <param name="channel">The channel through which the inbound message was received.</param>
        /// <param name="buffer">The segment of the byte array containing the binary message payload.</param>
        public override void OnMessage(WebSocketChannel channel, ArraySegment<byte> buffer)
        {
            // Send the received message back to the sender using the same channel through which it was received.
            channel.SendAsync(buffer);

            // Push the received binary message to all connected channels concurrently.
            Broadcast(buffer);
        }

        public override void OnMessage(WebSocketChannel channel, string text)
        {
            // Send the received message back to the sender using the same channel through which it was received.
            channel.SendAsync(text);

            // Push the received binary message to all connected channels concurrently.
            Broadcast(text);
        }

        #region Channel Management

        /// <summary>
        /// Validates the handshake performed when a WebSocket channel is being established.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="handshake">The instance providing methods and properties for performing the handshake. </param>
        public override void OnHandshake(object sender, WebSocketHandshake handshake)
        {
            if (handshake.Origin != "www.sample.com/ws")
            {
                handshake.ResultAction = WebSocketHandshakeAction.Drop;
            }
        }

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
            // Sends a ping to the remote endpoint.
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
        public override void OnError(WebSocketChannel channel, Exception e)
        {
            base.OnError(channel, e);
        }

        #endregion
    }
}
