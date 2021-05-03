﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Flow.Mapper;

namespace Friflo.Json.Flow.Database.Models
{
public class ReadEntitiesList : DatabaseTask
    {
        public  string                          container;
        public  List<ReadEntities>              reads;
        
        internal override   TaskType    TaskType => TaskType.Read;
        public   override   string      ToString() => "container: " + container;

        internal override async Task<TaskResult> Execute(EntityDatabase database, SyncResponse response) {
            var result = new ReadEntitiesListResult {
                reads = new List<ReadEntitiesResult>(reads.Count)
            };
            // Optimization:
            // Combine all reads to a single read to call ReadEntities() only once instead of #reads times
            var combinedRead = new ReadEntities { ids = new HashSet<string>() };
            foreach (var read in reads) {
                combinedRead.ids.UnionWith(read.ids);
            }
            var entityContainer = database.GetContainer(container);
            var combinedResult = await entityContainer.ReadEntities(combinedRead);
            
            var combinedEntities = combinedResult.entities;
            combinedResult.entities = null;
            var containerResult = response.GetContainerResult(container);
            containerResult.AddEntities(combinedEntities);
            
            foreach (var read in reads) {
                var readResult  = new ReadEntitiesResult {
                    entities = new Dictionary<string, EntityValue>(read.ids.Count)
                };
                // distribute combinedEntities
                foreach (var id in read.ids) {
                    readResult.entities.Add(id, combinedEntities[id]);
                }
                await read.ReadReferences(readResult, entityContainer, response);
                readResult.entities = null;
                result.reads.Add(readResult);
            }
            return result;
        }
    }
    
    public class ReadEntitiesListResult : TaskResult
    {
        public   List<ReadEntitiesResult>       reads;
        
        internal override   TaskType    TaskType => TaskType.Read;
    }
    
    public class ReadEntities
    {
        public  HashSet<string>                 ids;
        public  List<References>                references;
        
        internal async Task ReadReferences(ReadEntitiesResult readResult, EntityContainer entityContainer, SyncResponse response) {
            List<ReferencesResult> readRefResults = null;
            if (references != null && references.Count > 0) {
                readRefResults = await entityContainer.ReadReferences(references, readResult.entities, response);
            }
            readResult.references = readRefResults;
        }
    }
    
    /// The data of requested entities are added to <see cref="ContainerEntities.entities"/> 
    public class ReadEntitiesResult
    {
        public   List<ReferencesResult>         references;
        [Fri.Ignore]
        internal Dictionary<string,EntityValue> entities;
    }
}