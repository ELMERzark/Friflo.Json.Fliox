﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Friflo.Json.Fliox.Hub.Client.Internal;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using static System.Diagnostics.DebuggerBrowsableState;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Client
{
    public abstract class WriteTask<T> : SyncTask where T : class {
        internal readonly   Dictionary<JsonKey, Peer<T>>    peers = new Dictionary<JsonKey, Peer<T>>(JsonKey.Equality);
        
        [DebuggerBrowsable(Never)]
        internal            TaskState                       state;
        internal override   TaskState                       State   => state;

        internal abstract void GetIds(List<JsonKey> ids);
        
        internal void AddPeer (Peer<T> peer, PeerState peerState) {
            peers.TryAdd(peer.id, peer);        // sole place a peer (entity) is added
            peer.state = peerState;             // sole place Updated is set
        }
    }
    
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public sealed class CreateTask<T> : WriteTask<T> where T : class
    {
        private readonly    EntitySetBase<T>    set;
        private readonly    SyncSetBase<T>      syncSet;
        private readonly    List<T>             entities;

        public   override   string              Details => $"CreateTask<{typeof(T).Name}> (#keys: {entities.Count})";
        internal override   TaskType            TaskType=> TaskType.create;
        
        
        internal CreateTask(List<T> entities, EntitySetBase<T> set, SyncSetBase<T>  syncSet) {
            this.set        = set;
            this.syncSet    = syncSet;
            this.entities   = entities;
        }
        
        public void Add(T entity) {
            if (entity == null)
                throw new ArgumentException($"CreateTask<{set.name}>.Add() entity must not be null.");
            var peer = set.CreatePeer(entity);
            AddPeer(peer, PeerState.Create);
            entities.Add(entity);
        }
        
        public void AddRange(ICollection<T> entities) {
            int n = 0;
            foreach (var entity in entities) {
                if (entity == null)
                    throw new ArgumentException($"CreateTask<{set.name}>.AddRange() entities[{n}] must not be null.");
                n++;
                var peer = set.CreatePeer(entity);
                AddPeer(peer, PeerState.Create);
            }
            this.entities.AddRange(entities);
        }

        internal override void GetIds(List<JsonKey> ids) {
            foreach (var entity in entities) {
                var id = set.GetEntityId(entity);
                ids.Add(id);    
            }
        }
        
        internal override SyncRequestTask CreateRequestTask(in CreateTaskContext context) {
            return syncSet.CreateEntities(this, context);
        }

    }
}