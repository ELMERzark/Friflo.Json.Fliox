﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.Host.Event;
using Friflo.Json.Fliox.DB.Host.Internal;
using Friflo.Json.Fliox.DB.Protocol;

namespace Friflo.Json.Fliox.DB.Client.Internal
{
    internal sealed class EventTarget : IEventTarget
    {
        private readonly EntityStore    store;
        
        internal EventTarget (EntityStore store) {
            this.store = store; 
        } 
            
        // --- IEventTarget
        public bool     IsOpen ()   => true;

        public Task<bool> ProcessEvent(ProtocolEvent ev, MessageContext messageContext) {
            if (!ev.dstClientId.IsEqual(store._intern.clientId))
                throw new InvalidOperationException("Expect ProtocolEvent.dstId == EntityStore.clientId");
            
            // Skip already received events
            if (store._intern.lastEventSeq >= ev.seq)
                return Task.FromResult(true);
            
            store._intern.lastEventSeq = ev.seq;
            var eventMessage = ev as EventMessage;
            if (eventMessage == null)
                return Task.FromResult(true);

            store._intern.subscriptionProcessor?.EnqueueEvent(eventMessage);

            return Task.FromResult(true);
        }
    }
}