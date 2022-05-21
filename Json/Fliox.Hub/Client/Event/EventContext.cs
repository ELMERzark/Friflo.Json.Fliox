// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using Friflo.Json.Fliox.Hub.Client.Event;
using Friflo.Json.Fliox.Mapper;

// ReSharper disable ReturnTypeCanBeEnumerable.Global
// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Client
{
    public sealed class EventContext : ILogSource
    {
        public  readonly    JsonKey                         srcUserId;
        public              int                             EventSequence   => processor.EventSequence;
        public              IReadOnlyList<Message>          Messages        => processor.Messages;
        /// <summary> <see cref="ChangesList"/> return the changes per database <see cref="EntityChanges.Container"/>.
        /// Use <see cref="GetChanges{TKey,T}"/> to access specific container changes </summary>
        public              IReadOnlyList<EntityChanges>    ChangesList    => processor.contextChanges;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public              IHubLogger                      Logger { get; }

        private readonly    SubscriptionProcessor           processor;

        public  override    string                          ToString()  => $"source user: {srcUserId}";

        internal EventContext(SubscriptionProcessor processor, in JsonKey srcUserId, IHubLogger logger) {
            this.processor  = processor;
            this.srcUserId  = srcUserId;
            Logger          = logger;
        }
        
        public EntityChanges<TKey, T> GetChanges<TKey, T>(EntitySet<TKey, T> entitySet) where T : class {
            return (EntityChanges<TKey, T>)processor.GetChanges(entitySet);
        } 
    }
}