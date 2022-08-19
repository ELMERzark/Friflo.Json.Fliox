﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host.Event;
using Friflo.Json.Fliox.Hub.Protocol;

namespace Friflo.Json.Fliox.Hub.Client.Internal
{
    internal sealed class EventReceiver : IEventReceiver
    {
        private readonly FlioxClient    client;
        
        internal EventReceiver (FlioxClient client) {
            this.client = client; 
        } 
            
        // --- IEventReceiver
        public bool     IsRemoteTarget ()   => false;
        public bool     IsOpen ()           => true;

        public Task<bool> ProcessEvent(ProtocolEvent ev) {
            if (!ev.dstClientId.IsEqual(client._intern.clientId))
                throw new InvalidOperationException("Expect ProtocolEvent.dstId == FlioxClient.clientId");
            
            // Skip already received events
            if (client._intern.lastEventSeq >= ev.seq)
                return Task.FromResult(true);
            
            client._intern.lastEventSeq = ev.seq;
            var eventMessage = ev as EventMessage;
            if (eventMessage == null)
                return Task.FromResult(true);

            client._intern.eventProcessor.EnqueueEvent(client, eventMessage);

            return Task.FromResult(true);
        }
    }
}