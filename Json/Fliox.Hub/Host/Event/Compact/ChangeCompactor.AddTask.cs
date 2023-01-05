// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Burst.Utils;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;

// ReSharper disable SuggestBaseTypeForParameter
// ReSharper disable ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
namespace Friflo.Json.Fliox.Hub.Host.Event.Compact
{
    public sealed partial class ChangeCompactor
    {
        internal bool  AddTask(EntityDatabase database, SyncRequestTask task)
        {
            switch (task.TaskType) {
                case TaskType.create:
                    lock (databaseChangesMap) {
                        if (!databaseChangesMap.TryGetValue(database, out var databaseChanges))
                            return false;
                        var create = (CreateEntities)task;
                        AddWriteTask(databaseChanges, create.containerSmall, TaskType.create, create.entities);
                        return true;
                    }
                case TaskType.upsert:
                    lock (databaseChangesMap) {
                        if (!databaseChangesMap.TryGetValue(database, out var databaseChanges))
                            return false;
                        var upsert = (UpsertEntities)task;
                        AddWriteTask(databaseChanges, upsert.containerSmall, TaskType.upsert, upsert.entities);
                        return true;
                    }
                case TaskType.merge:
                    lock (databaseChangesMap) {
                        if (!databaseChangesMap.TryGetValue(database, out var databaseChanges))
                            return false;
                        var merge = (MergeEntities)task;
                        AddWriteTask(databaseChanges, merge.containerSmall, TaskType.merge, merge.patches);
                        break;
                    }
                case TaskType.delete:
                    lock (databaseChangesMap) {
                        if (!databaseChangesMap.TryGetValue(database, out var databaseChanges))
                            return false;
                        var delete = (DeleteEntities)task;
                        AddDeleteTask(databaseChanges, delete.containerSmall, delete.ids);
                        return true;
                    }
            }
            return false;
        }
        
        private static void AddWriteTask(
            DatabaseChanges     databaseChanges,
            in SmallString      name,
            TaskType            taskType,
            List<JsonEntity>    entities)
        {
            var containers = databaseChanges.containers;
            if (!containers.TryGetValue(name, out var container)) {
                container = new ContainerChanges(name);
                containers.Add(name, container);
            }
            var writeBuffer = databaseChanges.writeBuffer;
            var values      = writeBuffer.values;
            var valueBuffer = writeBuffer.valueBuffer;
            writeBuffer.changeTasks.Add(new ChangeTask(container, taskType, values.Count, entities.Count));
            foreach (var entity in entities) {
                var value = valueBuffer.Add(entity.value);
                values.Add(value);
            }
        }
        
        private static void AddDeleteTask(
            DatabaseChanges     databaseChanges,
            in SmallString      name,
            List<JsonKey>       ids)
        {
            var containers = databaseChanges.containers;
            if (!containers.TryGetValue(name, out var container)) {
                container = new ContainerChanges(name);
                containers.Add(name, container);
            }
            var writeBuffer = databaseChanges.writeBuffer;
            var keys        = writeBuffer.keys;
            writeBuffer.changeTasks.Add(new ChangeTask(container, TaskType.delete, keys.Count, ids.Count));
            keys.AddRange(ids);
        }
    }
}