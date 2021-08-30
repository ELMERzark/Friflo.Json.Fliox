﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Friflo.Json.Flow.Database;
using Friflo.Json.Flow.Database.Remote;
using Friflo.Json.Flow.Database.Utils;
using Friflo.Json.Flow.Graph;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Sync;
using Friflo.Json.Tests.Common.Utils;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Flow.Graph.Happy
{
    public partial class TestStore
    {
        /// <summary>
        /// Assert that the used <see cref="FileDatabase"/> support multi threaded access when reading and writing
        /// the same entity. By using <see cref="AsyncReaderWriterLock"/> a multi read / single write lock
        /// is used for each <see cref="FileContainer"/>. Without this lock it would result in:
        /// IOException: The process cannot access the file 'path' because it is being used by another process. 
        /// </summary>
        [Test] public static void TestConcurrentFileAccessSync () {
            using (var _                = Pools.SharedPools) // for LeakTestsFixture
            {
                SingleThreadSynchronizationContext.Run(async () => {
                    using (var fileDatabase = new FileDatabase(CommonUtils.GetBasePath() + "assets~/Graph/testConcurrencyDb")) {
                        await ConcurrentAccess(fileDatabase, 2, 2, 10, true);
                    }
                });
            }
        }
        
        public static async Task ConcurrentAccess(EntityDatabase database, int readerCount, int writerCount, int requestCount, bool singleEntity) {
            // --- prepare
            var store       = new SimpleStore(database, "prepare");
            var entities    = new List<SimplyEntity>();
            int max         = Math.Max(readerCount, writerCount);
            if (singleEntity) {
                var entity = new SimplyEntity{ id = 222, text = "Concurrent accessed entity" };
                for (int n = 0; n < max; n++) {
                    entities.Add(entity);
                }
                store.entities.Create(entity);
            } else {
                // use individual entity per readerStores / writerStore
                for (int n = 0; n < max; n++) {
                    entities.Add(new SimplyEntity{ id = n, text = "Concurrent accessed entity" });
                }
                store.entities.CreateRange(entities);
            }
            await store.Sync();

            var readerStores = new List<SimpleStore>();
            var writerStores = new List<SimpleStore>();
            try {
                for (int n = 0; n < readerCount; n++) {
                    readerStores.Add(new SimpleStore(database, $"reader-{n}"));
                }
                for (int n = 0; n < writerCount; n++) {
                    writerStores.Add(new SimpleStore(database, $"writer-{n}"));
                }

                // --- run readers and writers
                var tasks = new List<Task>();

                for (int n = 0; n < readerStores.Count; n++) {
                    tasks.Add(ReadLoop  (readerStores[n], entities[n], requestCount));
                }
                for (int n = 0; n < writerStores.Count; n++) {
                    tasks.Add(WriteLoop (writerStores[n], entities[n], requestCount));
                }
                var lastCount = 0;
                var count = new Thread(() => {
                    while (true) {
                        try {
                            Thread.Sleep(1000);
                        } catch { break; }
                        lastCount = CountRequests(readerStores, writerStores, lastCount);
                    }
                });
                count.Start();

                await Task.WhenAll(tasks);
                CountRequests(readerStores, writerStores, lastCount);
                count.Interrupt();
            }
            finally {
                store.Dispose();
                foreach (var readerStore in readerStores)
                    readerStore.Dispose();
                foreach (var writerStore in writerStores) {
                    writerStore.Dispose();
                }
            }
            
        }

        private static Task ReadLoop (SimpleStore store, SimplyEntity entity, int requestCount) {
            var id = entity.id;
            return Task.Run(async () => {
                for (int n= 0; n < requestCount; n++) {
                    var readEmployee = store.entities.Read();
                    readEmployee.Find(id);
                    await store.Sync();
                    AreEqual (1, readEmployee.Results.Count);
                    if (!readEmployee.Results.ContainsKey(id))
                        throw new TestException($"Expect entity with id: {id}");
                }
            });
        }
        
        private static Task WriteLoop (SimpleStore store, SimplyEntity entity, int requestCount) {
            return Task.Run(async () => {
                for (int n= 0; n < requestCount; n++) {
                    store.entities.Create(entity);
                    await store.Sync();
                }
            });
        }
        
        private static int CountRequests (List<SimpleStore> readers, List<SimpleStore> writers, int lastCount) {
            int sum = 0;
            sum += readers.Sum(reader => reader.GetSyncCount());
            sum += writers.Sum(writer => writer.GetSyncCount());
            var requests = sum - lastCount;
            Console.WriteLine($"requests: {requests}, sum: {sum}");
            return sum;
        }
        
        /// <summary>
        /// Assert that <see cref="WebSocketClientDatabase"/> support being used by multiple clients aka
        /// <see cref="EntityStore"/>'s and using concurrent requests.
        /// All <see cref="EntityDatabase"/> implementations support this behavior, so <see cref="WebSocketClientDatabase"/>
        /// have to ensure this also. It utilize <see cref="DatabaseRequest.reqId"/> to ensure this.
        /// </summary>
#if !UNITY_5_3_OR_NEWER 
        [Test]
        public static async Task TestConcurrentWebSocket () {
            using (var _                = Pools.SharedPools) // for LeakTestsFixture
            using (var db               = new MemoryDatabase())
            using (var hostDatabase     = new HttpHostDatabase(db, "http://+:8080/", null))
            using (var remoteDatabase   = new WebSocketClientDatabase("ws://localhost:8080/")) {
                await RunRemoteHost(hostDatabase, async () => {
                    await remoteDatabase.Connect();
                    await ConcurrentWebSocket(remoteDatabase, 4, 10); // 10 requests are sufficient to force concurrency error
                    await remoteDatabase.Close();
                });
            }
        }
#endif
        
        private static async Task ConcurrentWebSocket(EntityDatabase database, int clientCount, int requestCount)
        {
            // --- prepare
            var stores = new List<EntityStore>();
            try {
                for (int n = 0; n < clientCount; n++) {
                    stores.Add(new EntityStore(database, null,$"reader-{n}"));
                }
                var tasks = new List<Task>();
                
                // run loops
                for (int n = 0; n < stores.Count; n++) {
                    tasks.Add(MessageLoop  (stores[n], $"message-{n}", requestCount));
                }
                await Task.WhenAll(tasks);
            }
            finally {
                foreach (var store in stores) {
                    store.Dispose();
                }
            }
        }
        
        private static Task MessageLoop (EntityStore store, string text, int requestCount) {
            var result = $"\"{text}\"";
            return Task.Run(async () => {
                for (int n= 0; n < requestCount; n++) {
                    var message = store.SendMessage(StdMessage.Echo, text);
                    await store.Sync();
                    AreEqual (result, message.ResultJson);
                }
            });
        }
    }
    
    public class SimpleStore : EntityStore
    {
        public readonly EntitySet <int, SimplyEntity>   entities;
        
        public SimpleStore(EntityDatabase database, TypeStore typeStore, string clientId) : base (database, typeStore, clientId) {
            entities    = new EntitySet <int, SimplyEntity>       (this);
        }
        
        public SimpleStore(EntityDatabase database, string clientId) : this (database, TestGlobals.typeStore, clientId) { }
    }
    
    // ------------------------------ models ------------------------------
    public class SimplyEntity {
        public  int     id;
        public  string  text;
    }
}