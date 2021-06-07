﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Flow.Sync;

namespace Friflo.Json.Flow.Database.PubSub
{
    public interface ISyncObserver
    {
        void EnqueueSyncRequest (SyncRequest syncRequest);
    }
    
    public class Subscriber {
        internal            EntityDatabase                  database;
        internal            Subscription                    subscription;
        internal readonly   ConcurrentQueue<SyncRequest>    queue = new ConcurrentQueue<SyncRequest>();
        
        internal async Task Publish () {
            while (queue.TryDequeue(out var syncRequest)) {
                var contextPools    = new Pools(Pools.SharedPools);
                var syncContext     = new SyncContext(contextPools);
                try {
                    await database.ExecuteSync(syncRequest, syncContext);
                } catch (Exception e) {
                    Console.WriteLine(e.ToString());
                }
            }
        }
    }
    
    public class Publisher : ISyncObserver
    {
        public void Subscribe (EntityDatabase database, Subscription subscription) {
            var subscriber = new Subscriber {
                database        = database,
                subscription    = subscription
            };
            subscribers.Add(database, subscriber);
        }
        
        private readonly Dictionary<EntityDatabase, Subscriber> subscribers = new Dictionary<EntityDatabase, Subscriber>();
        
        public void EnqueueSyncRequest (SyncRequest syncRequest) {
            foreach (var pair in subscribers) {
                List<DatabaseTask>  subscriberTasks = null;
                Subscriber          subscriber = pair.Value;
                foreach (var task in syncRequest.tasks) {
                    var taskResult = FilterTask(task, subscriber.subscription);
                    if (taskResult == null)
                        continue;
                    if (subscriberTasks == null) {
                        subscriberTasks = new List<DatabaseTask>();
                    }
                    subscriberTasks.Add(taskResult);
                }
                if (subscriberTasks == null)
                    continue;
                var subscriberSync = new SyncRequest {
                    tasks = subscriberTasks
                };
                subscriber.queue.Enqueue(subscriberSync);
            }
        }
        
        private DatabaseTask FilterTask (DatabaseTask task, Subscription subscription) {
            ContainerFilter filter;
            switch (task.TaskType) {
                case TaskType.create:
                    var create = (CreateEntities) task;
                    filter = FindFilter(subscription, create.container, TaskType.create);
                    if (filter == null)
                        return null;
                    var createResult = new CreateEntities {
                        container   = create.container,
                        entities    = create.entities // new Dictionary<string, EntityValue>()
                    };
                    // todo apply filter to create.entities
                    return createResult;
                case TaskType.update:
                    var update = (UpdateEntities) task;
                    filter = FindFilter(subscription, update.container, TaskType.update);
                    if (filter == null)
                        return null;
                    var updateResult = new UpdateEntities {
                        container   = update.container,
                        entities    = update.entities // new Dictionary<string, EntityValue>()
                    };
                    // todo apply filter to update.entities
                    return updateResult;
                case TaskType.delete:
                    // todo
                    return null;
                case TaskType.patch:
                    // todo
                    return null;
                default:
                    return null;
            }
        }
        
        private static ContainerFilter FindFilter (Subscription subscription, string container, TaskType taskType) {
            foreach (var filter in subscription.filters) {
                if (filter.container == container) {
                    if (Array.IndexOf(filter.taskTypes, taskType) != -1)
                        return filter;
                    return null;
                }
            }
            return null;
        }
    }
}