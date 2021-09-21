﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.Host;
using Friflo.Json.Fliox.DB.Host.Event;
using Friflo.Json.Fliox.DB.Protocol;
using Friflo.Json.Fliox.Mapper;

// Note! - Must not have any dependency to System.Net or System.Net.Http (or other HTTP stuff)
namespace Friflo.Json.Fliox.DB.Remote
{
    public abstract class RemoteClientDatabase : EntityDatabase
    {
        private  readonly   Dictionary<string, IEventTarget>    clientTargets = new Dictionary<string, IEventTarget>();
        private  readonly   Pools                               pools = new Pools(Pools.SharedPools);

        // ReSharper disable once EmptyConstructor - added for source navigation
        protected RemoteClientDatabase() {
        }

        public override EntityContainer CreateContainer(string name, EntityDatabase database) {
            RemoteClientContainer container = new RemoteClientContainer(name, this);
            return container;
        }
        
        public override void AddEventTarget(string clientId, IEventTarget eventTarget) {
            clientTargets.Add(clientId, eventTarget);
        }
        
        public override void RemoveEventTarget(string clientId) {
            clientTargets.Remove(clientId);
        }
        
        protected void ProcessEvent(ProtocolEvent ev) {
            var eventTarget     = clientTargets[ev.targetId];
            var messageContext  = new MessageContext(pools, eventTarget);
            eventTarget.ProcessEvent(ev, messageContext);
            messageContext.Release();
        }

        protected static JsonUtf8 CreateProtocolMessage (ProtocolMessage request, IPools pools) {
            using (var pooledMapper = pools.ObjectMapper.Get()) {
                ObjectMapper mapper = pooledMapper.instance;
                mapper.Pretty = true;
                mapper.WriteNullMembers = false;
                return new JsonUtf8(mapper.WriteAsArray(request));
            }
        }
        
        protected static ProtocolMessage ReadProtocolMessage (JsonUtf8 jsonMessage, IPools pools) {
            using (var pooledMapper = pools.ObjectMapper.Get()) {
                ObjectReader reader = pooledMapper.instance.reader;
                var response = reader.Read<ProtocolMessage>(jsonMessage);
                if (reader.Error.ErrSet) {
                    var message = reader.Error.msg.ToString();
                    response = new ErrorResponse {
                        message = message
                    };
                }
                return response;
            }
        }
    }
    
    public class RemoteClientContainer : EntityContainer
    {
        public RemoteClientContainer(string name, EntityDatabase database)
            : base(name, database) {
        }

        public override Task<CreateEntitiesResult> CreateEntities(CreateEntities command, MessageContext messageContext) {
            throw new InvalidOperationException("RemoteClientContainer does not execute CRUD commands");
        }

        public override Task<UpsertEntitiesResult> UpsertEntities(UpsertEntities command, MessageContext messageContext) {
            throw new InvalidOperationException("RemoteClientContainer does not execute CRUD commands");
        }

        public override Task<ReadEntitiesResult> ReadEntities(ReadEntities command, MessageContext messageContext) {
            throw new InvalidOperationException("RemoteClientContainer does not execute CRUD commands");
        }
        
        public override Task<QueryEntitiesResult> QueryEntities(QueryEntities command, MessageContext messageContext) {
            throw new InvalidOperationException("RemoteClientContainer does not execute CRUD commands");
        }
        
        public override Task<DeleteEntitiesResult> DeleteEntities(DeleteEntities command, MessageContext messageContext) {
            throw new InvalidOperationException("RemoteClientContainer does not execute CRUD commands");
        }
    }
}
