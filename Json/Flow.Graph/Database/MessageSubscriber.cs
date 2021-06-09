﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Friflo.Json.Flow.Sync;

namespace Friflo.Json.Flow.Database
{
    public class MessageSubscriber {
        private  readonly   IMessageTarget                  messageTarget;
        internal readonly   SubscribeMessages               subscribe;
        internal readonly   ConcurrentQueue<PushMessage>    queue = new ConcurrentQueue<PushMessage>();
        
        public MessageSubscriber (IMessageTarget messageTarget, SubscribeMessages subscribe) {
            this.messageTarget  = messageTarget;
            this.subscribe      = subscribe;
        }
        
        internal async Task SendMessages () {
            if (!messageTarget.IsOpen())
                return;
            
            var contextPools    = new Pools(Pools.SharedPools);
            while (queue.TryPeek(out var message)) {
                try {
                    var syncContext     = new SyncContext(contextPools, messageTarget);
                    var success = await messageTarget.SendMessage(message, syncContext).ConfigureAwait(false);
                    if (success) {
                        queue.TryDequeue(out _);
                    }
                    syncContext.pools.AssertNoLeaks();
                } catch (Exception e) {
                    Console.WriteLine(e.ToString());
                }
            }
        }
    }
}