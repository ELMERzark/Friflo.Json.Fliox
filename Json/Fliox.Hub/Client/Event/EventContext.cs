// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using Friflo.Json.Fliox.Hub.Protocol;
using static System.Diagnostics.DebuggerBrowsableState;

// ReSharper disable ReturnTypeCanBeEnumerable.Global
// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Client
{
    /// <summary>
    /// Defines the signature of the event handler passed to <see cref="FlioxClient.SubscriptionEventHandler"/> <br/>
    /// </summary>
    /// <remarks>
    /// All subscription handler methods are synchronous by intention.<br/>
    /// <b>Reason:</b>
    /// In contrast to handler methods of a service or a web server subscription handlers don't return a result. <br/>
    /// In case the application need to call an asynchronous method consider using the approach below:
    /// <code>
    ///     Task.Factory.StartNew(() => AsyncMethod());
    /// </code>
    /// <b>Note:</b> exceptions thrown in the <c>AsyncMethod()</c> are unhandled. Add try/catch to log exceptions.
    /// </remarks>
    public delegate void SubscriptionEventHandler (EventContext context);
    
    /// <summary>
    /// The <see cref="EventContext"/> provide all information of subscription events received by a <see cref="FlioxClient"/>.<br/>
    /// Subscription events are received by a client in case the client setup subscriptions by the <b>Subscribe*()</b> methods
    /// of <see cref="FlioxClient"/> or <see cref="EntitySet{TKey,T}"/>.<br/>
    /// The event context provide the following event data.
    /// <list type="bullet">
    ///   <item> The <see cref="SrcUserId"/> - the origin of the event</item>
    ///   <item> The <see cref="Messages"/> send by a user </item>
    ///   <item> The database <see cref="Changes"/> made by a user </item>
    ///   <item> The <see cref="EventInfo"/> containing the number of messages and database changes </item>
    /// </list>
    /// Database change events are not automatically applied to a <see cref="FlioxClient"/>.<br/>
    /// To apply database change events to a <see cref="FlioxClient"/> call <see cref="ApplyChangesTo"/>.
    /// </summary>
    public sealed class EventContext : ILogSource
    {
        /// <summary> user id sending the <see cref="Messages"/> and causing the <see cref="Changes"/>  </summary>
        public              JsonKey                 SrcUserId       => ev.srcUserId;
        public              int                     EventSequence   => processor.EventSequence;
        /// <summary> return the <see cref="Messages"/> sent by a user </summary>
        public              IReadOnlyList<Message>  Messages        => processor.messages;
        /// <summary> <see cref="Changes"/> return the changes per database container. <br/>
        /// Use <see cref="GetChanges{TKey,T}"/> to access specific container changes </summary>
        public              IReadOnlyList<Changes>  Changes         => processor.contextChanges;
        /// <summary> return the number of <see cref="Messages"/> and <see cref="Changes"/> of the subscription event </summary>
        public              EventInfo               EventInfo       => ev.GetEventInfo();
        
        public  override    string                  ToString()      => $"source user: {ev.srcUserId}";
        
        [DebuggerBrowsable(Never)] public           IHubLogger              Logger { get; private set; }
        [DebuggerBrowsable(Never)] private readonly SubscriptionProcessor   processor;
        [DebuggerBrowsable(Never)] private          EventMessage            ev;

        internal EventContext(SubscriptionProcessor processor) {
            this.processor  = processor;
        }
        
        internal void Init(EventMessage ev, IHubLogger logger) {
            this.ev = ev;
            Logger  = logger;
        }
        
        /// <summary>
        /// return type-safe access to the changes made to a container. <br/>
        /// The container is identified by the passed <paramref name="entitySet"/>. <br/> 
        /// These changes contain the: <see cref="Changes{TKey,T}.Creates"/>, <see cref="Changes{TKey,T}.Upserts"/>,
        /// <see cref="Changes{TKey,T}.Deletes"/> and <see cref="Changes{TKey,T}.Patches"/> made to a container
        /// </summary>
        public Changes<TKey, T> GetChanges<TKey, T>(EntitySet<TKey, T> entitySet) where T : class {
            return (Changes<TKey, T>)processor.GetChanges(entitySet);
        }
        
        /// <summary> Apply all <see cref="Changes"/> the given <paramref name="client"/> </summary>
        public void ApplyChangesTo(FlioxClient client)
        {
            foreach (var entityChanges in processor.contextChanges) {
                var entityType = entityChanges.GetEntityType();
                if (!client._intern.TryGetSetByType(entityType, out var entitySet))
                    continue;
                entityChanges.ApplyChangesToInternal(entitySet);
            }
        }
    }
}