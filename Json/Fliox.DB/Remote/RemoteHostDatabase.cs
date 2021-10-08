﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.Client;
using Friflo.Json.Fliox.DB.Host;
using Friflo.Json.Fliox.DB.Protocol;
using Friflo.Json.Fliox.Mapper;

// Note! - Must not have any dependency to System.Net or System.Net.Http (or other HTTP stuff)
namespace Friflo.Json.Fliox.DB.Remote
{
    public class RemoteHostDatabase : EntityDatabase
    {
        private  readonly   EntityDatabase  local;
        /// Only set to true for testing. It avoids an early out at <see cref="Host.Event.EventSubscriber.SendEvents"/> 
        public              bool            fakeOpenClosedSockets;

        public RemoteHostDatabase(EntityDatabase local) {
            this.local = local;
        }

        public override EntityContainer CreateContainer(string name, EntityDatabase database) {
            EntityContainer localContainer = local.CreateContainer(name, local);
            RemoteHostContainer container = new RemoteHostContainer(name, this, localContainer);
            return container;
        }
        
        public override async Task<MsgResponse<SyncResponse>> ExecuteSync(SyncRequest syncRequest, MessageContext messageContext) {
            var response = await local.ExecuteSync(syncRequest, messageContext).ConfigureAwait(false);
            SetContainerResults(response.success);
            response.Result.reqId       = syncRequest.reqId;
            return response;
        }

        public async Task<JsonResponse> ExecuteJsonRequest(JsonUtf8 jsonRequest, MessageContext messageContext) {
            try {
                var request = RemoteUtils.ReadProtocolMessage(jsonRequest, messageContext.pools, out string error);
                switch (request) {
                    case null:
                        return JsonResponse.CreateError(messageContext, error, JsonResponseStatus.Error);
                    case SyncRequest syncRequest:
                        var         response        = await ExecuteSync(syncRequest, messageContext).ConfigureAwait(false);
                        JsonUtf8    jsonResponse    = RemoteUtils.CreateProtocolMessage(response.Result, messageContext.pools);
                        return new JsonResponse(jsonResponse, JsonResponseStatus.Ok);
                    default:
                        var msg = $"Unknown request. Name: {request.GetType().Name}";
                        return JsonResponse.CreateError(messageContext, msg, JsonResponseStatus.Error);
                }
            }
            catch (Exception e) {
                var errorMsg = ErrorResponse.ErrorFromException(e).ToString();
                return JsonResponse.CreateError(messageContext, errorMsg, JsonResponseStatus.Exception);
            }
        }
        
        /// Required only by <see cref="RemoteHostDatabase"/>
        /// Distribute <see cref="ContainerEntities.entityMap"/> to <see cref="ContainerEntities.entities"/>,
        /// <see cref="ContainerEntities.notFound"/> and <see cref="ContainerEntities.errors"/> to simplify and
        /// minimize response by removing redundancy.
        /// <see cref="EntityStore.GetContainerResults"/> remap these properties.
        private static void SetContainerResults(SyncResponse response)
        {
            if (response == null)
                return;
            var resultMap = response.resultMap;
            response.resultMap = null;
            var results = response.results = new List<ContainerEntities>(resultMap.Count);
            foreach (var resultPair in resultMap) {
                ContainerEntities value = resultPair.Value;
                results.Add(value);
            }
            foreach (var container in results) {
                var entityMap       = container.entityMap;
                var entities        = container.entities;
                List<JsonKey> notFound = null;
                var errors          = container.errors;
                container.errors    = null;
                entities.Capacity   = entityMap.Count;
                foreach (var entityPair in entityMap) {
                    EntityValue entity = entityPair.Value;
                    if (entity.Error != null) {
                        errors.Add(entityPair.Key, entity.Error);
                        continue;
                    }
                    var json = entity.Json;
                    if (json.IsNull()) {
                        if (notFound == null) {
                            notFound = new List<JsonKey>();
                        }
                        notFound.Add(entityPair.Key);
                        continue;
                    }
                    entities.Add(new JsonValue(json));
                }
                entityMap.Clear();
                if (notFound != null) {
                    container.notFound = notFound;
                }
                if (errors != null && errors.Count > 0) {
                    container.errors = errors;
                }
            }
            resultMap.Clear();
        }
    }

    public sealed class RemoteHostContainer : EntityContainer
    {
        private readonly    EntityContainer local;
        
        public  override    bool            Pretty       => local.Pretty;

        public RemoteHostContainer(string name, EntityDatabase database, EntityContainer localContainer)
            : base(name, database) {
            local = localContainer;
        }

        public override async Task<CreateEntitiesResult> CreateEntities(CreateEntities command, MessageContext messageContext) {
            return await local.CreateEntities(command, messageContext).ConfigureAwait(false);
        }

        public override async Task<UpsertEntitiesResult> UpsertEntities(UpsertEntities command, MessageContext messageContext) {
            return await local.UpsertEntities(command, messageContext).ConfigureAwait(false);
        }

        public override async Task<ReadEntitiesResult> ReadEntities(ReadEntities command, MessageContext messageContext) {
            return await local.ReadEntities(command, messageContext).ConfigureAwait(false);
        }
        
        public override async Task<QueryEntitiesResult> QueryEntities(QueryEntities command, MessageContext messageContext) {
            return await local.QueryEntities(command, messageContext).ConfigureAwait(false);
        }
        
        public override async Task<DeleteEntitiesResult> DeleteEntities(DeleteEntities command, MessageContext messageContext) {
            return await local.DeleteEntities(command, messageContext).ConfigureAwait(false);
        }
    }
}
