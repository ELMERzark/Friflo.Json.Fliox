﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Flow.Transform;

namespace Friflo.Json.Flow.Database.Models
{
    public class PatchEntities : DatabaseTask
    {
        public  string                          container;
        public  Dictionary<string, EntityPatch> patches;
        
        internal override   TaskType            TaskType => TaskType.Patch;
        public   override   string              ToString() => "container: " + container;
        
        internal override async Task<TaskResult> Execute(EntityDatabase database, SyncResponse response) {
            var entityContainer = database.GetContainer(container);
            return await entityContainer.PatchEntities(this);
        }
    }

    public class EntityPatch
    {
        public List<JsonPatch>                  patches;
    }

    public class PatchEntitiesResult : TaskResult
    {
        internal override TaskType      TaskType => TaskType.Patch;
    }
}