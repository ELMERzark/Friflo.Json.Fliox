﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Friflo.Json.Flow.Graph;
using Friflo.Json.Flow.Mapper.Map.Val;

namespace Friflo.Json.EntityGraph.Database
{
    // ----------------------------------------- EntityDatabase -----------------------------------------
    public abstract class EntityDatabase : IDisposable
    {
        // [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly Dictionary<string, EntityContainer>    containers = new Dictionary<string, EntityContainer>();
        
        public abstract EntityContainer CreateContainer(string name, EntityDatabase database);

        public virtual void Dispose() {
            foreach (var container in containers ) {
                container.Value.Dispose();
            }
        }

        internal void AddContainer(EntityContainer container)
        {
            containers.Add(container.name, container);
        }

        public EntityContainer GetContainer(string name)
        {
            if (containers.TryGetValue(name, out EntityContainer container))
                return container;
            containers[name] = container = CreateContainer(name, this);
            return container;
        }
        
        public virtual SyncResponse Execute(SyncRequest syncRequest) {
            var response = new SyncResponse {
                results             = new List<CommandResult>(),
                containerResults    = new Dictionary<string, ContainerEntities>()
            };
            foreach (var command in syncRequest.commands) {
                var result = command.Execute(this, response);
                response.results.Add(result);
            }
            return response;
        }
    }
    
    public class EntityValue {
        public JsonValue    value;

        public EntityValue() { } // required for TypeMapper

        public EntityValue(string json) {
            value.json = json;
        }
    }
    
    
    // ----------------------------------------- EntityContainer -----------------------------------------
    public abstract class EntityContainer : IDisposable
    {
        public  readonly    string          name;
        private readonly    EntityDatabase  database;

        public virtual      bool            Pretty      => false;
        public virtual      SyncContext     SyncContext => null;


        protected EntityContainer(string name, EntityDatabase database) {
            this.name = name;
            database.AddContainer(this);
            this.database = database;
        }
        
        public virtual  void                            Dispose() { }
        
        public abstract void                            CreateEntities(Dictionary<string, EntityValue> entities);
        public abstract void                            UpdateEntities(Dictionary<string, EntityValue> entities);
        public abstract Dictionary<string, EntityValue> ReadEntities  (ICollection<string> ids);

        /// <summary>
        /// Default implementation to apply patches to entities.
        /// The implementation perform three steps:
        /// 1. Read entities to be patches from a database
        /// 2. Apply patches
        /// 3. Write back the patched entities
        ///
        /// If the used database has integrated support for patching JSON its <see cref="EntityContainer"/>
        /// implementation can override this method to replace two database requests by one.
        /// </summary>
        public virtual void PatchEntities(IList<EntityPatch> entityPatches) {
            var ids = entityPatches.Select(patch => patch.id).ToList();
            // Read entities to be patched
            var entities = ReadEntities(ids);
            if (entities.Count != ids.Count)
                throw new InvalidOperationException($"PatchEntities: Expect entities.Count of response matches request. expect: {ids.Count} got: {entities.Count}");
            
            // Apply patches
            var patcher = SyncContext.jsonPatcher;
            int n = 0;
            foreach (var entity in entities) {
                var expectedId = ids[n];
                var patch = entityPatches[n++];
                if (entity.Key != expectedId) {
                    throw new InvalidOperationException($"PatchEntities: Expect entity key of response matches request: index:{n} expect: {expectedId} got: {entity.Key}");
                }
                entity.Value.value.json = patcher.ApplyPatches(entity.Value.value.json, patch.patches, Pretty);
            }
            // Write patched entities back
            CreateEntities(entities); // should be UpdateEntities
        }

        public List<ReadDependencyResult> ReadDependencies(
                List<ReadDependency>            dependencies,
                Dictionary<string, EntityValue> entities,
                SyncResponse                    syncResponse)
        {
            var jsonPath    = SyncContext.scalarSelector;
            var dependencyResults = new List<ReadDependencyResult>();
            foreach (var dependency in dependencies) {
                var depContainer = database.GetContainer(dependency.container);
                var dependencyResult = new ReadDependencyResult {
                    container   = dependency.container,
                    ids         = new List<string>()
                };
                foreach (var id in dependency.ids) {
                    EntityValue depEntity = entities[id];
                    if (depEntity == null) {
                        throw new InvalidOperationException($"expect entity dependency available: {id}");
                    }
                    // todo call Select() only once with multiple selectors 
                    var select = new ScalarSelect(dependency.refPath);
                    var selectorResults = jsonPath.Select(depEntity.value.json, select);
                    var depIds = selectorResults[0].AsStrings();
                    dependencyResult.ids.AddRange(depIds);
                    
                    // add dependencies to syncDependencies
                    var depEntities = depContainer.ReadEntities(depIds);
                    var containerResult = syncResponse.GetContainerResult(dependency.container);
                    containerResult.AddEntities(depEntities);
                }
                dependencyResults.Add(dependencyResult);
            }
            return dependencyResults;
        }
    }
}
