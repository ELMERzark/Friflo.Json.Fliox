// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Threading;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Threading;

namespace Friflo.Json.Fliox.Hub.Client
{
    public abstract class EventProcessor
    {
        public abstract void EnqueueEvent(FlioxClient client, EventMessage ev);
        
        protected void ProcessEvent(FlioxClient client, EventMessage ev) {
            client._intern.subscriptionProcessor.ProcessEvent(client, ev);
        }
    }
    /// <summary>
    /// Creates a <see cref="SubscriptionProcessor"/> using a <see cref="SynchronizationContext"/>
    /// The <see cref="SynchronizationContext"/> is required to ensure that <see cref="SubscriptionProcessor.ProcessEvent"/> is called on the
    /// same thread as all other methods calls of <see cref="FlioxClient"/> and <see cref="EntitySet{TKey,T}"/>.
    /// <para>
    ///   In case of UI applications like WinForms, WPF or Unity <see cref="SynchronizationContext.Current"/> can be used.
    /// </para> 
    /// <para>
    ///   In case of a Console application or a unit test where <see cref="SynchronizationContext.Current"/> is null
    ///   <see cref="SingleThreadSynchronizationContext"/> can be used.
    /// </para> 
    /// </summary>
    public class SynchronizedEventProcessor : EventProcessor
    {
        private readonly    SynchronizationContext              synchronizationContext;
        

        public SynchronizedEventProcessor(SynchronizationContext synchronizationContext) {
            this.synchronizationContext = synchronizationContext ?? throw new ArgumentNullException(nameof(synchronizationContext));
        }
        
        public SynchronizedEventProcessor() {
            synchronizationContext =
                SynchronizationContext.Current
                ?? throw new InvalidOperationException(SynchronizationContextIsNull);
        }
        
        private const string SynchronizationContextIsNull = @"SynchronizationContext.Current is null.
This is typically the case in console applications or unit tests. 
Consider running application / test withing SingleThreadSynchronizationContext.Run()";
        
        public override void EnqueueEvent(FlioxClient client, EventMessage ev) {
            synchronizationContext.Post(delegate {
                ProcessEvent(client, ev);
            }, null);
        }
    }
    
    public class DirectEventProcessor : EventProcessor
    {
        public override void EnqueueEvent(FlioxClient client, EventMessage ev) {
            ProcessEvent(client, ev);
        }
    }
    
    public class QueuingEventProcessor : EventProcessor
    {
        private readonly    ConcurrentQueue <QueuedMessage>      eventQueue = new ConcurrentQueue <QueuedMessage> ();

        /// <summary>
        /// Creates a queuing <see cref="SubscriptionProcessor"/>.
        /// In this case the application must frequently call <see cref="ProcessEvents"/> to apply changes to the
        /// <see cref="FlioxClient"/>.
        /// This allows to specify the exact code point in an application (e.g. Unity) where <see cref="EventMessage"/>'s
        /// are applied to the <see cref="FlioxClient"/>.
        /// </summary>
        public QueuingEventProcessor() { }
        
        public override void EnqueueEvent(FlioxClient client, EventMessage ev) {
            eventQueue.Enqueue(new QueuedMessage(client, ev));
        }
        
        /// <summary>
        /// Need to be called frequently if <see cref="SubscriptionProcessor"/> is initialized without a <see cref="SynchronizationContext"/>.
        /// </summary>
        public void ProcessEvents() {
            while (eventQueue.TryDequeue(out QueuedMessage queuedMessage)) {
                ProcessEvent(queuedMessage.client, queuedMessage.message);
            }
        }

        private readonly struct QueuedMessage
        {
            internal  readonly  FlioxClient     client;
            internal  readonly  EventMessage    message;
            
            internal QueuedMessage(FlioxClient client, EventMessage  message) {
                this.client     = client;
                this.message    = message;
            }
        }
    }
}