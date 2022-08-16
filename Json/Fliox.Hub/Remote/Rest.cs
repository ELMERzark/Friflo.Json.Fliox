// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Schema.Validation;
using Friflo.Json.Fliox.Transform;
using Friflo.Json.Fliox.Transform.Query.Ops;
using Friflo.Json.Fliox.Transform.Query.Parser;

// ReSharper disable SwitchStatementHandlesSomeKnownEnumValuesWithDefault
// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
namespace Friflo.Json.Fliox.Hub.Remote
{
    /// <summary>static class to ensure all REST methods are static</summary>
    internal static partial class Rest
    {
        // -------------------------------------- helper methods --------------------------------------
        private static JsonKey[] GetKeysFromIds(string[] ids) {
            var keys = new JsonKey[ids.Length];
            for (int n = 0; n < ids.Length; n++) {
                keys[n] = new JsonKey(ids[n]);
            }
            return keys;
        }
        
        private static bool IsValidJson (Pool pool, JsonValue value, out string error) {
            error = null;
            if (value.IsNull())
                return true;
            using (var pooled = pool.TypeValidator.Get()) {
                var validator = pooled.instance;
                if (!validator.ValidateJson(value, out error)) {
                    return false;
                }
            }
            return true;
        }
        
        private static string GetErrorType (string command) {
            return command != null ? "command error" : "message error";
        }
        
        private static bool TryParseParamAsInt(RequestContext context, string name, NameValueCollection queryParams, out int? result) {
            var valueStr = queryParams[name];
            if (valueStr == null) {
                result = null;
                return true;
            }
            if (int.TryParse(valueStr, out int value)) {
                result = value;
                return true;
            }
            context.WriteError("url parameter error", $"expect {name} as integer. was: {valueStr}", 400);
            result = null;
            return false;
        }
        
        private static bool HasQueryKey(NameValueCollection queryParams, string searchKey, out bool value, out string error) {
            var  allKeys  = queryParams.AllKeys;
            for (int n = 0; n < allKeys.Length; n++) {
                var paramValue = queryParams.Get(n); // what a crazy interface! :)
                if (paramValue == null)
                    continue;
                if (paramValue == searchKey || paramValue == "true") {
                    value = true;
                    error = null;
                    return true;
                }
                if (paramValue == "false") {
                    value = false;
                    error = null;
                    return true;
                }
                value = false;
                error = $"invalid boolean query parameter value: {paramValue}, parameter: {searchKey}";
                return false;
            }
            error = null;
            value = false;
            return true;
        }
        
        // -------------------------------------- resource access  --------------------------------------
        private static async Task GetEntitiesById(RequestContext context, string database, string container, JsonKey[] keys) {
            if (database == context.hub.DatabaseName)
                database = null;
            var readEntities = new ReadEntities { container = container, ids = new List<JsonKey>(keys.Length)};
            foreach (var id in keys) {
                readEntities.ids.Add(id);    
            }
            var syncRequest = CreateSyncRequest(context, database, readEntities, out var syncContext);
            var syncResult  = await context.hub.ExecuteSync(syncRequest, syncContext).ConfigureAwait(false);
            
            var restResult  = CreateRestResult(context, syncResult);
            if (restResult.taskResult == null)
                return;
            var readResult  = (ReadEntitiesResult)restResult.taskResult;
            var resultError = readResult.Error;
            if (resultError != null) {
                context.WriteError("read error", resultError.message, 500);
                return;
            }
            var entityMap   = restResult.syncResponse.resultMap[container].entityMap;
            var entities    = new List<JsonValue>(entityMap.Count);
            foreach (var pair in entityMap) {
                entities.Add(pair.Value.Json);
            }
            context.AddHeader("count", entities.Count.ToString()); // added to simplify debugging experience
            using (var pooled = context.ObjectMapper.Get()) {
                var writer          = pooled.instance.writer;
                writer.Pretty       = true;
                var entitiesJson    = new JsonValue(writer.WriteAsArray(entities));
                context.Write(entitiesJson, 0, "application/json", 200);
            }
        }
        
        private static async Task QueryEntities(RequestContext context, string database, string container, NameValueCollection queryParams) {
            if (database == context.hub.DatabaseName)
                database = null;
            var filter = CreateFilterTree(context, queryParams);
            if (filter.IsNull())
                return;
            if (!TryParseParamAsInt(context, "maxCount", queryParams, out int? maxCount))
                return;
            if (!TryParseParamAsInt(context, "limit",    queryParams, out int? limit))
                return;
            var cursor          = queryParams["cursor"];
            var queryEntities   = new QueryEntities{ container = container, filterTree = filter, maxCount = maxCount, cursor = cursor, limit = limit };
            var syncRequest     = CreateSyncRequest(context, database, queryEntities, out var syncContext);
            var syncResult      = await context.hub.ExecuteSync(syncRequest, syncContext).ConfigureAwait(false);
            
            var restResult      = CreateRestResult(context, syncResult);
            if (restResult.taskResult == null)
                return;
            var queryResult     = (QueryEntitiesResult)restResult.taskResult;
            var resultError     = queryResult.Error;
            if (resultError != null) {
                context.WriteError("query error", resultError.message, 500);
                return;
            }
            var taskResult  = (QueryEntitiesResult)restResult.syncResponse.tasks[0];
            if (taskResult.cursor != null) {
                context.AddHeader("cursor", taskResult.cursor);
            }
            var entityMap   = restResult.syncResponse.resultMap[container].entityMap;
            var entities    = new List<JsonValue>(entityMap.Count);
            foreach (var pair in entityMap) {
                entities.Add(pair.Value.Json);
            }
            context.AddHeader("count", entities.Count.ToString()); // added to simplify debugging experience
            using (var pooled = context.ObjectMapper.Get()) {
                var writer      = pooled.instance.writer;
                writer.Pretty   = true;
                var entityArray = writer.WriteAsArray(entities);
                var response    = new JsonValue(entityArray);
                context.Write(response, 0, "application/json", 200);
            }
        }
        
        /// enforce "o" as lambda argument
        private const string DefaultParam   = "o";
        private const string InvalidFilter  = "invalid filter";
        
        private static JsonValue CreateFilterTree(RequestContext context, NameValueCollection queryParams) {
            var sharedCache         = context.SharedCache;
            var filterValidation    = sharedCache.GetValidationType(typeof(FilterOperation));
            using (var pooled = context.ObjectMapper.Get()) {
                var mapper = pooled.instance;
                var filter = CreateFilter(context, queryParams, mapper, filterValidation);
                if (filter == null)
                    return new JsonValue();
                var filterJson = mapper.writer.Write(filter);
                return new JsonValue(filterJson);
            }
        }
        
        private static FilterOperation CreateFilter(RequestContext context, NameValueCollection queryParams, ObjectMapper mapper, ValidationType filterValidation) {
            // --- handle filter expression
            var filter = queryParams["filter"];
            if (filter != null) {
                var env   = new QueryEnv(DefaultParam); 
                var op    = Operation.Parse(filter, out string error, env);
                if (error != null) {
                    context.WriteError(InvalidFilter, error, 400);
                    return null;
                }
                if (op is FilterOperation filterOperation) {
                    return filterOperation;
                }
                context.WriteError(InvalidFilter, "filter must be boolean operation", 400);
                return null;
            }
            // --- handle filter tree
            var filterTree = queryParams["filter-tree"];
            if (filterTree == null) {
                return Operation.FilterTrue;
            }
            var pool = context.Pool;
            using (var pooled = pool.TypeValidator.Get()) {
                var validator   = pooled.instance;
                var json        = new JsonValue(filterTree);
                if (!validator.ValidateObject(json, filterValidation, out var error)) {
                    context.WriteError(InvalidFilter, error, 400);
                    return null;
                }
            }
            var reader = mapper.reader;
            var filterOp = mapper.reader.Read<FilterOperation>(filterTree);
            if (reader.Error.ErrSet) {
                context.WriteError(InvalidFilter, reader.Error.ToString(), 400);
                return null;
            }
            // Early out on invalid filter (e.g. symbol not found). Init() is cheap. If successful QueryEntities does the same check. 
            var operationCx = new OperationContext();
            if (!operationCx.Init(filterOp, out var message)) {
                context.WriteError(InvalidFilter, message, 400);
                return null;
            }
            return filterOp;
        }
        
        private static async Task GetEntity(RequestContext context, string database, string container, string id) {
            if (database == context.hub.DatabaseName)
                database = null;
            var entityId        = new JsonKey(id);
            var readEntities = new ReadEntities { container = container, ids = new List<JsonKey> {entityId}};
            var syncRequest = CreateSyncRequest(context, database, readEntities, out var syncContext);
            var syncResult  = await context.hub.ExecuteSync(syncRequest, syncContext).ConfigureAwait(false);
            
            var restResult  = CreateRestResult(context, syncResult);
            if (restResult.taskResult == null)
                return;
            var readResult  = (ReadEntitiesResult)restResult.taskResult;
            var resultError = readResult.Error;
            if (resultError != null) {
                context.WriteError("read error", resultError.message, 500);
                return;
            }
            var entityMap   = restResult.syncResponse.resultMap[container].entityMap;
            var content     = entityMap[entityId];
            var entityError = content.Error;
            if (entityError != null) {
                context.WriteError("entity error", $"{entityError.type} - {entityError.message}", 404);
                return;
            }
            var entityStatus = content.Json.IsNull() ? 404 : 200;
            context.Write(content.Json, 0, "application/json", entityStatus);
        }
        
        private static async Task DeleteEntities(RequestContext context, string database, string container, JsonKey[] keys) {
            if (database == context.hub.DatabaseName)
                database = null;
            var deleteEntities  = new DeleteEntities { container = container, ids = new List<JsonKey>(keys.Length) };
            foreach (var key in keys) {
                deleteEntities.ids.Add(key);
            }
            var syncRequest     = CreateSyncRequest(context, database, deleteEntities, out var syncContext);
            var syncResult      = await context.hub.ExecuteSync(syncRequest, syncContext).ConfigureAwait(false);
            
            var restResult      = CreateRestResult(context, syncResult);
            if (restResult.taskResult == null)
                return;
            var deleteResult    = (DeleteEntitiesResult)restResult.taskResult;
            var resultError     = deleteResult.Error;
            if (resultError != null) {
                context.WriteError("delete error", resultError.message, 500);
                return;
            }
            var entityErrors = deleteResult.errors;
            if (entityErrors != null) {
                var sb = new StringBuilder();
                FormatEntityErrors (entityErrors, sb);
                context.WriteError("DELETE errors", sb.ToString(), 400);
                return;
            }
            context.WriteString("deleted successful", "text/plain", 200);
        }
        
        private static async Task PutEntities(RequestContext context, string database, string container, string id, string keyName, JsonValue value, TaskType type) {
            if (database == context.hub.DatabaseName)
                database = null;
            var             pool = context.Pool;
            List<JsonValue> entities;
            if (id != null) {
                entities = new List<JsonValue> {value};
            } else {
                // read entity array from request body
                using (var pooled = pool.EntityProcessor.Get()) {
                    var processor = pooled.instance;
                    entities = processor.ReadJsonArray(value, out string error);
                    if (error != null) {
                        context.WriteError("PUT error", error, 400);
                        return;
                    }
                }
            }
            var entityId    = new JsonKey(id);
            keyName         = keyName ?? "id";
            if (id != null) {
                // check if given id matches entity key
                using (var pooled = pool.EntityProcessor.Get()) {
                    var processor = pooled.instance;
                    if (!processor.GetEntityKey(value, keyName, out JsonKey key, out string entityError)) {
                        context.WriteError("PUT error", entityError, 400);
                        return;
                    }
                    if (!entityId.IsEqual(key)) {
                        context.WriteError("PUT error", $"entity {keyName} != resource id. expect: {id}, was: {key.AsString()}", 400);
                        return;
                    }
                }
            }
            SyncRequestTask task;
            switch (type) {
                case TaskType.upsert: task  = new UpsertEntities { container = container, keyName = keyName, entities = entities }; break;
                case TaskType.create: task  = new CreateEntities { container = container, keyName = keyName, entities = entities }; break;
                default:
                    throw new InvalidOperationException($"Invalid PUT type: {type}");
            }
            var syncRequest = CreateSyncRequest(context, database, task, out var syncContext);
            var syncResult  = await context.hub.ExecuteSync(syncRequest, syncContext).ConfigureAwait(false);
            
            var restResult  = CreateRestResult(context, syncResult);
            if (restResult.taskResult == null)
                return;
            var taskResult  = (ICommandResult)restResult.taskResult;
            var resultError = taskResult.Error;
            if (resultError != null) {
                context.WriteError("PUT error", resultError.message, 500);
                return;
            }
            List<EntityError> entityErrors;
            switch (type) {
                case TaskType.upsert: entityErrors = ((UpsertEntitiesResult)taskResult).errors;   break;
                case TaskType.create: entityErrors = ((CreateEntitiesResult)taskResult).errors;   break;
                default:
                    throw new InvalidOperationException($"Invalid PUT type: {type}");
            }
            if (entityErrors != null) {
                var sb = new StringBuilder();
                FormatEntityErrors (entityErrors, sb);
                context.WriteError("PUT errors", sb.ToString(), 400);
                return;
            }
            context.WriteString("PUT successful", "text/plain", 200);
        }
        
        private static async Task PatchEntity(RequestContext context, string database, string container, string id, string keyName, JsonValue value) {
            if (database == context.hub.DatabaseName)
                database = null;
            List<JsonPatch> patches;
            using (var pooled = context.ObjectMapper.Get()) {
                var reader  = pooled.instance.reader;
                patches     = reader.Read<List<JsonPatch>>(value);
                if (reader.Error.ErrSet) {
                    context.WriteError("PATCH error", reader.Error.ToString(), 400);
                    return;
                }
            }
            var entityId    = new JsonKey(id);
            keyName         = keyName ?? "id";
            var entityPatch = new EntityPatch { id = entityId, patches = patches };
            var task        = new PatchEntities { container = container, keyName = keyName };
            task.patches.Add(entityPatch);
            var syncRequest = CreateSyncRequest(context, database, task, out var syncContext);
            var syncResult  = await context.hub.ExecuteSync(syncRequest, syncContext).ConfigureAwait(false);
            
            var restResult  = CreateRestResult(context, syncResult);
            if (restResult.taskResult == null)
                return;
            var patchResult = (PatchEntitiesResult)restResult.taskResult;
            var resultError = patchResult.Error;
            if (resultError != null) {
                context.WriteError("PATCH error", resultError.message, 500);
                return;
            }
            var entityErrors = patchResult.errors;
            if (entityErrors != null) {
                var sb = new StringBuilder();
                FormatEntityErrors (entityErrors, sb);
                context.WriteError("PATCH errors", sb.ToString(), 400);
                return;
            }
            context.WriteString("PATCH successful", "text/plain", 200);
        }
        
        private static void FormatEntityErrors(List<EntityError> entityErrors, StringBuilder sb) {
            foreach (var error in entityErrors) {
                sb.Append("\n| ");
                sb.Append(error.type);
                sb.Append(": [");
                sb.Append(error.id);
                sb.Append("], ");
                sb.Append(error.message);
            }
        }
        
        // ----------------------------------------- command / message -----------------------------------------
        private static async Task Command(RequestContext context, string database, string command, JsonValue param) {
            var sendCommand = new SendCommand { name = command, param = param };
            var syncRequest = CreateSyncRequest(context, database, sendCommand, out var syncContext);
            var syncResult  = await context.hub.ExecuteSync(syncRequest, syncContext).ConfigureAwait(false);
            
            var restResult  = CreateRestResult(context, syncResult);
            if (restResult.taskResult == null)
                return;
            var sendResult  = (SendCommandResult)restResult.taskResult;
            var resultError = sendResult.Error;
            if (resultError != null) {
                context.WriteError("send error", resultError.message, 500);
                return;
            }
            context.Write(sendResult.result, 0, "application/json", 200);
        }
        
        private static async Task Message(RequestContext context, string database, string message, JsonValue param) {
            var sendMessage = new SendMessage { name = message, param = param };
            var syncRequest = CreateSyncRequest(context, database, sendMessage, out var syncContext);
            var syncResult  = await context.hub.ExecuteSync(syncRequest, syncContext).ConfigureAwait(false);
            
            var restResult  = CreateRestResult(context, syncResult);
            if (restResult.taskResult == null)
                return;
            var sendResult  = (SendMessageResult)restResult.taskResult;
            var resultError = sendResult.Error;
            if (resultError != null) {
                context.WriteError("message error", resultError.message, 500);
                return;
            }
            context.WriteString("\"received\"", "application/json", 200);
        }


        // ----------------------------------------- utils -----------------------------------------
        private static SyncRequest CreateSyncRequest (RequestContext context, string database, SyncRequestTask task, out SyncContext syncContext) {
            var tasks   = new List<SyncRequestTask> { task };
            var userId  = context.cookies["fliox-user"];
            var token   = context.cookies["fliox-token"];
            var clientId= context.headers["fliox-client"];
            var syncRequest = new SyncRequest {
                database    = database,
                tasks       = tasks,
                userId      = new JsonKey(userId),
                token       = token,
                clientId    = new JsonKey(clientId) 
            };
            var hub         = context.hub;
            var sharedCache = context.SharedCache;
            var pool        = hub.sharedEnv.Pool;
            syncContext     = new SyncContext(pool, null, sharedCache);
            return syncRequest;
        }
        
        private static RestResult CreateRestResult (RequestContext context, ExecuteSyncResult result)
        {
            var error = result.error;
            if (error != null) {
                var status = error.type == ErrorResponseType.BadRequest ? 400 : 500;
                context.WriteError("sync error", error.message, status);
                return default;
            }
            var syncResponse    = result.success;
            var taskResult      = syncResponse.tasks[0];
            if (taskResult is TaskErrorResult errorResult) {
                int status;
                switch (errorResult.type) {
                    case TaskErrorResultType.InvalidTask:           status = 400;   break;
                    case TaskErrorResultType.PermissionDenied:      status = 403;   break;
                    case TaskErrorResultType.DatabaseError:         status = 500;   break;
                    case TaskErrorResultType.FilterError:           status = 400;   break;
                    case TaskErrorResultType.ValidationError:       status = 400;   break;
                    case TaskErrorResultType.CommandError:          status = 400;   break;
                    case TaskErrorResultType.None:                  status = 500;   break;
                    case TaskErrorResultType.UnhandledException:    status = 500;   break;
                    case TaskErrorResultType.NotImplemented:        status = 501;   break;
                    case TaskErrorResultType.SyncError:             status = 500;   break;
                    default:                                        status = 500;   break;
                }
                var errorMessage    = errorResult.message;
                var stacktrace      = errorResult.stacktrace;
                // append new line to stacktrace to avoid annoying scrolling in monaco editor when clicking below stacktrace
                var message         = stacktrace == null ? errorMessage : $"{errorMessage}\n{stacktrace}\n";
                context.WriteError(errorResult.type.ToString(), message, status);
                return default;
            }
            return new RestResult (syncResponse, taskResult);
        }
    }
}