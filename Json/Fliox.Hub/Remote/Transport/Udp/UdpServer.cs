// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Utils;
using static Friflo.Json.Fliox.Hub.Remote.TransportUtils;

namespace Friflo.Json.Fliox.Hub.Remote.Transport.Udp
{
    public sealed class UdpServer : IDisposable, ILogSource
    {
        internal readonly   FlioxHub                                hub;
        private  readonly   Socket                                  socket;
        private  readonly   IPEndPoint                              ipEndPoint;
        internal readonly   MessageBufferQueueAsync<UdpMeta>        sendQueue;
        private  readonly   List<MessageItem<UdpMeta>>              messages;
        private  readonly   RemoteHostEnv                           hostEnv;
        private  readonly   Dictionary<IPEndPoint, UdpSocketHost>   clients;
        private  readonly   StringBuilder                           sbSend = new StringBuilder();
        private  readonly   StringBuilder                           sbRecv = new StringBuilder();
        
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public              IHubLogger  Logger { get; }
        
        public UdpServer(string endpoint, FlioxHub hub) {
            this.hub    = hub;
            ipEndPoint  = ParseEndpoint(endpoint) ?? throw new ArgumentException($"invalid endpoint: {endpoint}");
            socket      = new Socket(SocketType.Dgram, ProtocolType.Udp);
            socket.Bind(ipEndPoint);
            Logger      = hub.Logger;
            sendQueue   = new MessageBufferQueueAsync<UdpMeta>();
            messages    = new List<MessageItem<UdpMeta>>();
            hostEnv     = hub.GetFeature<RemoteHostEnv>();
            clients     = new Dictionary<IPEndPoint, UdpSocketHost>();
        }

        public void Dispose() {
            sendQueue.Dispose();
        }

        public async Task Run() {
            await SendReceiveMessages().ConfigureAwait(false);
        }
        
        /// <summary>
        /// Loop is purely I/O bound => don't wrap in
        /// return Task.Run(async () => { ... });
        /// </summary>
        /// <remarks>
        /// A send loop reading from a queue is required as message can be sent from two different sources <br/>
        /// 1. response messages created in <see cref="ReceiveMessageLoop"/> <br/>
        /// 2. event messages send with <see cref="SocketHost.SendEvent"/>'s <br/>
        /// The loop ensures a UdpClient.SendAsync() is called only once at a time.
        /// </remarks>
        private async Task RunSendMessageLoop() {
            try {
                await SendMessageLoop().ConfigureAwait(false);
            } catch (Exception e) {
                var msg = GetExceptionMessage("UdpServer.RunSendMessageLoop()", ipEndPoint, e);
                Logger.Log(HubLog.Info, msg);
            }
        }
        
        /// Send queue is required to ensure having only a single outstanding SendAsync() at any time
        private async Task SendMessageLoop() {
            while (true) {
                var remoteEvent = await sendQueue.DequeMessagesAsync(messages).ConfigureAwait(false);
                
                foreach (var message in messages) {
                    if (hostEnv.logMessages) LogMessage(Logger, sbSend, " server ->", message.meta.remoteEndPoint, message.value);
                    var array = message.value.AsMutableArraySegment();
                    await socket.SendToAsync(array, SocketFlags.None, message.meta.remoteEndPoint).ConfigureAwait(false);
                }
                if (remoteEvent == MessageBufferEvent.Closed) {
                    return;
                }
            }
        }
        
        private async Task RunReceiveMessageLoop() {
            await ReceiveMessageLoop().ConfigureAwait(false);
        }
        
        private static readonly IPEndPoint DummyEndpoint = new IPEndPoint(IPAddress.Any, 0);
        
        /// <summary>
        /// Parse, execute and send response message for all received request messages.<br/>
        /// </summary>
        private async Task ReceiveMessageLoop() {
            var buffer = new ArraySegment<byte>(new byte[0x10000]);
            while (true) {
                // --- 1. Read request from datagram
                var result = await socket.ReceiveFromAsync(buffer, SocketFlags.None, DummyEndpoint).ConfigureAwait(false);
                
                var remoteEndpoint  = (IPEndPoint)result.RemoteEndPoint;
                if (!clients.TryGetValue(remoteEndpoint, out var socketHost)) {
                    socketHost              = new UdpSocketHost(this, remoteEndpoint);
                    clients[remoteEndpoint] = socketHost;
                }
                var request = new JsonValue(buffer.Array, result.ReceivedBytes);
                if (hostEnv.logMessages) LogMessage(Logger, sbRecv, " server <-", socketHost.remoteClient, request);
                socketHost.OnReceive(request, ref hostEnv.metrics.udp);
            }
        }

        /// <summary>
        /// Create a send and receive queue and run a send and a receive loop. <br/>
        /// The loops are executed until the WebSocket is closed or disconnected. <br/>
        /// The method <b>don't</b> throw exception. WebSocket exceptions are catched and written to <see cref="FlioxHub.Logger"/> <br/>
        /// </summary>
        private async Task SendReceiveMessages()
        {
            Task sendLoop   = null;
            try {
                sendLoop = RunSendMessageLoop();

                await RunReceiveMessageLoop().ConfigureAwait(false);

                sendQueue.Close();
            }
            catch (Exception e) {
                var msg = GetExceptionMessage("UdpServer.SendReceiveMessages()", ipEndPoint, e);
                hub.Logger.Log(HubLog.Info, msg);
            }
            finally {
                if (sendLoop != null) {
                    await sendLoop.ConfigureAwait(false);
                }
                socket?.Dispose();
            }
        }
    }
}