﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Threading;
using static System.Diagnostics.DebuggerBrowsableState;

// ReSharper disable ConvertToAutoPropertyWhenPossible
namespace Friflo.Json.Fliox.Hub.Host.Event
{
    internal enum TriggerType {
        None,
        Finish,
        Event
    }

    /// <summary>
    /// Each <see cref="EventSubClient"/> instance (Event Subscriber Client) - handle the subscriptions for a specific client. <br/>
    /// It send database changes and messages as events to the client for all subscriptions the client made. <br/>
    /// A client is identified by its <see cref="clientId"/>.
    /// </summary>
    internal sealed class EventSubClient : ILogSource {
        internal readonly   JsonKey                             clientId;   // key field
        internal readonly   EventSubUser                        user;
        internal            bool                                queueEvents;
        private             IEventReceiver                      eventReceiver; // can be null if created by a REST request

        public              bool                                Connected => eventReceiver?.IsOpen() ?? false;
        [DebuggerBrowsable(Never)]
        public              IHubLogger                          Logger { get; }
        
        /// key: database
        [DebuggerBrowsable(Never)]
        internal readonly   Dictionary<string, DatabaseSubs>    databaseSubs = new Dictionary<string, DatabaseSubs>();
        // ReSharper disable once UnusedMember.Local - expose Dictionary as list in Debugger
        private             IReadOnlyCollection<DatabaseSubs>   DatabaseSubs => databaseSubs.Values;
        
        internal            int                                 SubCount    => databaseSubs.Sum(sub => sub.Value.SubCount); 
        
        /// lock (<see cref="unsentEventsQueue"/>) {
        private             int                                 eventCounter;
        /// contains all events not yet sent. Should be a Deque but C# doesn't contain this container type. 
        private  readonly   LinkedList<SyncEvent>               unsentEventsQueue   = new LinkedList<SyncEvent>();
        /// contains all events which are sent but not acknowledged
        private  readonly   Queue<SyncEvent>                    sentEventsQueue     = new Queue<SyncEvent>();
        // }
        
        private  readonly   bool                                background;
        internal readonly   Task                                triggerLoop;
        private  readonly   IDataChannelWriter<TriggerType>     triggerWriter;

        internal            int                                 Seq                 => eventCounter;
        /// <summary> number of events stored for a client not yet acknowledged by the client </summary>
        internal            int                                 QueuedEventsCount   => unsentEventsQueue.Count + sentEventsQueue.Count;
        /// <summary>
        /// <b>true</b>  if eventReceiver is null or a remote target (WebSocket). <br/>
        /// <b>false</b> if the eventReceiver is provided by a FlioxClient (in process) 
        /// </summary>
        internal            bool                                SerializeEvents     => eventReceiver?.IsRemoteTarget() ?? true;
        
        public   override   string                              ToString()          => $"client: '{clientId.AsString()}'";
        
        
        internal EventSubClient (
            SharedEnv       env,
            EventSubUser    user,
            in JsonKey      clientId,
            bool            background)
        {
            Logger              = env.hubLogger;
            this.clientId       = clientId;
            this.user           = user;
            this.background     = background;
            if (!this.background)
                return;
            // --- use trigger channel and loop
            var channel         = DataChannelSlim<TriggerType>.CreateUnbounded(true, true);
            triggerWriter       = channel.Writer;
            var triggerReader   = channel.Reader;
            triggerLoop         = TriggerLoop(triggerReader);
        }
        
        internal bool UpdateTarget(IEventReceiver eventReceiver) {
            if (eventReceiver == null) throw new ArgumentNullException(nameof(eventReceiver));
            if (this.eventReceiver == eventReceiver)
                return false;
            var msg = this.eventReceiver != null ?
                $"eventReceiver changed - client id: {clientId}" :
                $"eventReceiver new     - client id: {clientId}";
            this.eventReceiver = eventReceiver;
            Logger.Log(HubLog.Info, msg);
            SendUnacknowledgedEvents();
            return true;
        }
        
        internal void EnqueueEvent(SyncEvent ev) {
            lock (unsentEventsQueue) {
                ev.seq = ++eventCounter;
                unsentEventsQueue.AddLast(ev);
            }
            // Signal new event. Need to be signaled after adding event to queue. No reason to execute this in the lock. 
            if (background) {
                EnqueueTrigger(TriggerType.Event);
            }
        }
        
        private bool DequeueEvents(out SyncEvent[] events) {
            lock (unsentEventsQueue) {
                var count = unsentEventsQueue.Count;
                if (count == 0) {
                    events = null;
                    return false;
                }
                if (count > 100) count = 100;
                events = new SyncEvent[count];
                for (int n = 0; n < count; n++) {
                    var ev = unsentEventsQueue.First();
                    unsentEventsQueue.RemoveFirst();
                    events[n] = ev;
                    if (queueEvents) {
                        sentEventsQueue.Enqueue(ev);
                    }
                } 
                return true;
            }
        }
        
        /// <summary>
        /// Remove all acknowledged events from <see cref="sentEventsQueue"/>
        /// </summary>
        internal void AcknowledgeEvents(int eventAck) {
            lock (unsentEventsQueue) {
                while (sentEventsQueue.Count > 0) {
                    var ev = sentEventsQueue.Peek();
                    if (ev.seq <= eventAck) {
                        sentEventsQueue.Dequeue();
                        continue;
                    }
                    break; 
                }
            }
        }

        /// <summary>
        /// Prepend all not acknowledged events to <see cref="unsentEventsQueue"/> in their original order
        /// and trigger sending these events stored in <see cref="unsentEventsQueue"/>
        /// </summary>
        private void SendUnacknowledgedEvents() {
            lock (unsentEventsQueue) {
                LinkedListNode<SyncEvent> head = null;
                foreach (var ev in sentEventsQueue) {
                    if (head == null) {
                        head = unsentEventsQueue.AddFirst(ev);
                    } else  {
                        head = unsentEventsQueue.AddAfter(head, ev);
                    }
                }
                sentEventsQueue.Clear();
                if (background && unsentEventsQueue.Count > 0) {
                    // Console.WriteLine($"unsentEventsQueue: {unsentEventsQueue.Count}");
                    // Trace.WriteLine($"*** SendUnacknowledgedEvents. Count: {unsentEventsQueue.Count}");
                    EnqueueTrigger (TriggerType.Event);
                }
            }
        }
        
        internal async Task SendEvents () {
            var receiver = eventReceiver;
            // early out in case the target is a remote connection which already closed.
            if (receiver == null || !receiver.IsOpen()) {
                if (queueEvents)
                    return;
                lock (unsentEventsQueue) {
                    unsentEventsQueue.Clear();
                }
                return;
            }
            // Trace.WriteLine("--- SendEvents");
            while (DequeueEvents(out var events)) {
                // var msg = $"DequeueEvent {ev.seq}";
                // Trace.WriteLine(msg);
                // Console.WriteLine(msg);
                try {
                    // Console.WriteLine($"--- SendEvents: {events.Length}");
                    // In case the event target is remote connection it is not guaranteed that the event arrives.
                    // The remote target may already be disconnected and this is still not know when sending the event.
                    var eventMessage = new EventMessage { dstClientId = clientId, events = events };
                    await receiver.ProcessEvent(eventMessage).ConfigureAwait(false);
                }
                catch (Exception e) {
                    var message = "SendEvents failed";
                    Logger.Log(HubLog.Error, message, e);
                    Debug.Fail($"{message}, exception: {e}");
                }
            }
        }
        
        // ---------------------------- trigger channel and queue ----------------------------
        private Task TriggerLoop(IDataChannelReader<TriggerType> triggerReader) {
            var loopTask = Task.Run(async () => {
                try {
                    while (true) {
                        var trigger = await triggerReader.ReadAsync().ConfigureAwait(false);
                        if (trigger == TriggerType.Event) {
                            await SendEvents().ConfigureAwait(false);
                            continue;
                        }
                        Logger.Log(HubLog.Info, $"TriggerLoop() returns. {trigger}");
                        return;
                    }
                } catch (Exception e) {
                    var message = "TriggerLoop() failed";
                    Logger.Log(HubLog.Error, message, e);
                    Debug.Fail(message, e.Message);
                }
            });
            return loopTask;
        }
        
        private void EnqueueTrigger(TriggerType trigger) {
            bool success = triggerWriter.TryWrite(trigger);
            if (success)
                return;
            Debug.Fail("EnqueueTrigger() - writer.TryWrite() failed");
        }
        
        internal void FinishQueue() {
            EnqueueTrigger(TriggerType.Finish);
            triggerWriter.Complete();
        }
    }
}