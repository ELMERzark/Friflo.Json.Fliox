﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.Host;
using Friflo.Json.Fliox.DB.Protocol;
using Friflo.Json.Fliox.DB.Protocol.Models;
using Friflo.Json.Fliox.DB.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Client
{
    public class TestDatabase : EntityDatabase
    {
        private readonly    EntityDatabase  local;
        private readonly    Dictionary<string, TestContainer>                   testContainers  = new Dictionary<string, TestContainer>();
        public  readonly    Dictionary<string, Func<MsgResponse<SyncResponse>>> syncErrors      = new Dictionary<string, Func<MsgResponse<SyncResponse>>>();
        
        public TestDatabase(EntityDatabase local, DbOpt opt = null) : base(opt) {
            this.local = local;
        }
        
        public void ClearErrors() {
            syncErrors.Clear();
            foreach (var pair in testContainers) {
                var container = pair.Value;
                container.readEntityErrors.Clear();
                container.missingResultErrors.Clear();
                container.readTaskErrors.Clear();
                container.writeEntityErrors.Clear();
                container.writeTaskErrors.Clear();
                container.queryErrors.Clear();
            }
        }

        public override EntityContainer CreateContainer(string name, EntityDatabase database) {
            if (TryGetContainer(name, out EntityContainer container)) {
                return container;
            }
            EntityContainer localContainer = local.GetOrCreateContainer(name);
            var testContainer = new TestContainer(name, this, localContainer);
            testContainers.Add(name, testContainer);
            return testContainer;
        }
        
        public override async Task<MsgResponse<SyncResponse>> ExecuteSync(SyncRequest syncRequest, MessageContext messageContext) {
            foreach (var task in syncRequest.tasks) {
                if (task is SendMessage message) {
                    if (!syncErrors.TryGetValue(message.name, out var fcn))
                        continue;
                    var resp = fcn();
                    return resp;
                }
            }
            var response = await base.ExecuteSync(syncRequest, messageContext);
            foreach (var pair in testContainers) {
                TestContainer testContainer = pair.Value;
                if (!response.success.resultMap.TryGetValue(testContainer.name, out var result))
                    continue;
                var entities = result.entityMap;
                foreach (var id in testContainer.missingResultErrors) {
                    var key = new JsonKey(id);
                    if (entities.TryGetValue(key, out EntityValue _)) {
                        entities.Remove(key);
                    }
                }
            }
            return response;
        }

        public TestContainer GetTestContainer(string container) {
            return (TestContainer) GetOrCreateContainer(container);
        }
    }
    
    /// <summary>
    /// Used to create all possible errors and exceptions which can be made by a <see cref="EntityContainer"/> implementation.
    /// These are:
    /// <para>1. A task error set to <see cref="ICommandResult.Error"/> in a <see cref="ICommandResult"/>.</para>
    /// <para>2. Exceptions thrown by a <see cref="EntityContainer"/> command by a buggy implementation.</para>
    /// <para>3. One or more <see cref="ReadError"/>'s added to a <see cref="SyncTaskResult"/> entity error dictionary.</para>
    /// <br></br>
    /// Note: The <see cref="TestContainer"/> doesnt modify the underlying <see cref="local"/> <see cref="EntityContainer"/>
    /// to avoid side effects by error tests.
    /// </summary>
    public class TestContainer : EntityContainer
    {
        private readonly    EntityContainer local;
        public  readonly    Dictionary<string, Action<EntityValue>> readEntityErrors    = new Dictionary<string, Action<EntityValue>>();
        public  readonly    HashSet<string>                         missingResultErrors = new HashSet<string>();
        public  readonly    Dictionary<string, Func<CommandError>>  readTaskErrors      = new Dictionary<string, Func<CommandError>>();

        public  readonly    Dictionary<string, Func<EntityError>>   writeEntityErrors   = new Dictionary<string, Func<EntityError>>();
        public  readonly    Dictionary<string, Func<CommandError>>  writeTaskErrors     = new Dictionary<string, Func<CommandError>>();

        public  readonly    Dictionary<string, Func<QueryEntitiesResult>>  queryErrors  = new Dictionary<string,  Func<QueryEntitiesResult>>();
        

        
        public  override    bool            Pretty       => local.Pretty;

        public TestContainer(string name, EntityDatabase database, EntityContainer localContainer)
            : base(name, database) {
            local = localContainer;
        }

        public override Task<CreateEntitiesResult> CreateEntities(CreateEntities command, MessageContext messageContext) {
            AssertEntityCounts(command.entityKeys, command.entities);
            var error = SimulateWriteErrors(command.entityKeys.ToHashSet(JsonKey.Equality), out var errors);
            if (error != null)
                return Task.FromResult(new CreateEntitiesResult {Error = error});
            if (errors != null)
                return Task.FromResult(new CreateEntitiesResult {createErrors = errors});
            return Task.FromResult(new CreateEntitiesResult());
        }

        public override Task<UpsertEntitiesResult> UpsertEntities(UpsertEntities command, MessageContext messageContext) {
            AssertEntityCounts(command.entityKeys, command.entities);
            var error = SimulateWriteErrors(command.entityKeys.ToHashSet(JsonKey.Equality), out var errors);
            if (error != null)
                return Task.FromResult(new UpsertEntitiesResult {Error = error});
            if (errors != null)
                return Task.FromResult(new UpsertEntitiesResult {upsertErrors = errors});
            return Task.FromResult(new UpsertEntitiesResult());
        }
        
        public override Task<DeleteEntitiesResult> DeleteEntities(DeleteEntities command, MessageContext messageContext) {
            var error = SimulateWriteErrors(command.ids, out var errors);
            if (error != null)
                return Task.FromResult(new DeleteEntitiesResult {Error = error});
            if (errors != null)
                return Task.FromResult(new DeleteEntitiesResult {deleteErrors = errors});
            return Task.FromResult(new DeleteEntitiesResult());
        }

        /// Validation of JSON entity values in result set is required, as this this implementation is able to
        /// simulate assign invalid JSON via .<see cref="SimulateReadErrors"/>.
        public override async Task<ReadEntitiesSetResult> ReadEntitiesSet(ReadEntitiesSet command, MessageContext messageContext) {
            var result = await local.ReadEntitiesSet(command, messageContext).ConfigureAwait(false);
            var databaseError = SimulateReadErrors(result.entities);
            if (databaseError != null)
                result.Error = databaseError;
            result.ValidateEntities(local.name, command.keyName, messageContext);
            return result;
        }
        
        public override async Task<QueryEntitiesResult> QueryEntities(QueryEntities command, MessageContext messageContext) {
            var result = await local.QueryEntities(command, messageContext).ConfigureAwait(false);
            var databaseError = SimulateReadErrors(result.entities);
            if (databaseError != null) {
                result.Error = databaseError;
                return result;
            }
            var linq = command.filter.Linq;
            if (queryErrors.TryGetValue(linq, out var func)) {
                return func();
            }
            return result;
        }
        
        
        // --- simulate read/write error methods
        private CommandError SimulateReadErrors(Dictionary<JsonKey,EntityValue> entities) {
            foreach (var pair in readEntityErrors) {
                var id      = pair.Key;
                if (entities.TryGetValue(new JsonKey(id), out EntityValue value)) {
                    var action = pair.Value;
                    action(value);
                }
            }
            foreach (var pair in readTaskErrors) {
                var id      = new JsonKey(pair.Key);
                if (entities.ContainsKey(id)) {
                    var func = pair.Value;
                    return func();
                }
            }
            return null;
        }
        
        public EntityError ReadError(string id) {
            var error = new EntityError(EntityErrorType.ReadError, name, new JsonKey(id), "simulated read entity error");
            return error;
        }
        
        public EntityError WriteError(string id) {
            var error = new EntityError(EntityErrorType.WriteError, name, new JsonKey(id), "simulated write entity error");
            return error;
        }
        
        private CommandError SimulateWriteErrors(HashSet<JsonKey> entities, out Dictionary<JsonKey, EntityError> errors) {
            errors = null;
            foreach (var pair in writeEntityErrors) {
                var id = new JsonKey(pair.Key);
                if (entities.Contains(id)) {
                    if (errors == null)
                        errors = new Dictionary<JsonKey, EntityError>(JsonKey.Equality);
                    var fcn = pair.Value;
                    var entityError = fcn();
                    errors.Add(id, entityError);
                }
            }
            foreach (var pair in writeTaskErrors) {
                var id = new JsonKey(pair.Key);
                if (entities.Contains(id)) {
                    var func = pair.Value;
                    return func();
                }
            }
            return null;
        }
    }

    public class SimulationException : Exception {
        public SimulationException(string message) : base(message) { }
    }
}
