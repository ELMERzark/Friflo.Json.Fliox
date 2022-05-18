﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Client.Internal;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Utils;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Utils;
using Friflo.Json.Tests.Common.UnitTest.Fliox.Hubs;
using NUnit.Framework;
using static NUnit.Framework.Assert;


namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Client
{
    public class TestUtils
    {
        [Test]
        public void TestQueryRef() {
            using (var __       = SharedEnv.Default) // for LeakTestsFixture
            using (var database = new MemoryDatabase("db"))
            using (var env      = new SharedEnv())
            using (var hub      = new FlioxHub(database, env))
            using (var store    = new PocStore(hub) { UserId = "TestQueryRef"}) {
                var orders = store.orders;
                var customerId = orders.Query(o => o.customer.Key == "customer-1");
                AreEqual("QueryTask<Order> (filter: o => o.customer == 'customer-1')", customerId.ToString());
                
                var e = Throws<NotSupportedException>(() => { var _ = orders.Query(o => o.customer.Entity == null); });
                AreEqual("Query using Ref<>.Entity intentionally not supported. Only Ref<>.id is valid: o.customer.Entity, expression: o => (o.customer.Entity == null)", e.Message);

                store.SyncTasks().Wait();
            }
        }

        [Test]
        public void TestEmptyDictionary() {
            // ReSharper disable once CollectionNeverUpdated.Local
            var empty = new EmptyDictionary<string, string>();
            empty.Clear(); // no exception
            
            var kvPair = new KeyValuePair<string, string>("A","B");
            IsFalse(empty.Contains(kvPair));
            
            IsFalse(empty.ContainsKey("X"));
            
            AreEqual(0, empty.Count);
            
            IsFalse(empty.TryGetValue("Y", out var value));
            IsNull(value);
            
            foreach (var _ in empty) {
                Fail("cant be reached - dictionary is always empty");
            }
        }
        
        [Test]
        public void TestJsonEntities() {
            using (var typeStore = new TypeStore())
            using (var mapper = new ObjectMapper(typeStore)) {
                JsonEntities entities = new JsonEntities(2);
                entities.entities.Add(new JsonKey("int"), new EntityValue("1"));
                entities.entities.Add(new JsonKey("str"), new EntityValue("\"hello\""));
                var json = mapper.Write(entities);
                AreEqual("{\"int\":1,\"str\":\"hello\"}", json);
                
                var result = mapper.Read<JsonEntities>(json);
                AreEqual(entities.entities[new JsonKey("int")].Json.AsString(), result.entities[new JsonKey("int")].Json.AsString());
                AreEqual(entities.entities[new JsonKey("str")].Json.AsString(), result.entities[new JsonKey("str")].Json.AsString());
            }
        }
        
        [Test]
        public void TestEntityProcessor() {
            using (var processor = new EntityProcessor()) {
                {
                    // --- return modified JSON
                    var     json = new JsonValue("{\"myId\": \"123\"}");
                    var     result  = processor.ReplaceKey(json, "myId", false, "id", out JsonKey _, out _);
                    AreEqual("{\"id\":\"123\"}", result.AsString());
                } {
                    // --- return modified JSON
                    var     json =  new JsonValue("{\"myId\": \"111\"}");
                    var     result  = processor.ReplaceKey(json, "myId", true, "id", out JsonKey _, out _);
                    AreEqual("{\"id\":111}", result.AsString());
                } {
                    // --- return modified JSON
                    var     json = new JsonValue("{\"id\": 456}");
                    var     result  = processor.ReplaceKey(json, "id", false, "id", out JsonKey _, out _);
                    AreEqual("{\"id\":\"456\"}", result.AsString());
                } {
                    // --- return modified JSON - key ist not first member
                    var     json = new JsonValue("{\"x\":42,\"id2\":222}");
                    var     result  = processor.ReplaceKey(json, "id2", true, "id", out JsonKey _, out _);
                    AreEqual("{\"x\":42,\"id\":222}", result.AsString());
                } {
                    // --- return modified JSON - previous member contains unicode (☀), key is unicode (🌎)
                    var     json = new JsonValue("{\"☀\":1,\"🌎\": \"xyz\",\"♥\":2}");
                    var     result  = processor.ReplaceKey(json, "🌎", false, "🪐", out JsonKey _, out _);
                    AreEqual("{\"☀\":1,\"🪐\":\"xyz\",\"♥\":2}", result.AsString());
                } {
                    // --- return original JSON
                    var     json = new JsonValue("{\"id\": 789}");
                    var     result  = processor.ReplaceKey(json, "id", true, "id", out JsonKey _, out _);
                    IsTrue(json.IsEqualReference(result));
                } {
                    // --- return original JSON
                    var     json =  new JsonValue("{\"id\": \"abc\"}");
                    // null defaults to "id"
                    var result = processor.ReplaceKey(json, null, false, "id", out JsonKey _, out _);
                    IsTrue(json.IsEqualReference(result));
                } {
                    // --- error on invalid integer key. Valid range: [long.MinValue, long.MaxValue] 
                    var     json = new JsonValue("{\"id\": 9223372036854776000}");
                    var     result  = processor.ReplaceKey(json, "id", false, "id", out JsonKey _, out string error);
                    IsTrue(result.IsNull()); 
                    AreEqual("invalid integer key: Value out of range when parsing long: 9223372036854776000", error);
                }
            }
        }

#if !UNITY_2020_1_OR_NEWER
        [Test]
        public void TestDictionaryValueIterator() {
            var env     = new SharedEnv();
            var hub     = new FlioxHub(new MemoryDatabase("db"), env);
            var store   = new PocStore(hub) { UserId = "TestDictionaryValueIterator"};
            var readArticles = store.articles.Read();
                        readArticles.Find("missing-id");
            var task =  readArticles.ReadRef(a => a.producer);
            SubRefs subRefs = new SubRefs();
            subRefs.AddTask("someTask", task);

            // ensure iterator does not allocate something on heap by boxing
            var startBytes = GC.GetAllocatedBytesForCurrentThread();
            foreach (var _ in subRefs) {
            }
            var endBytes = GC.GetAllocatedBytesForCurrentThread();
            AreEqual(startBytes, endBytes);
        }
        
        /// <summary>
        /// FlioxClient / sub class resource consumption:
        /// Memory:    ~ 1540 + 200 * (EntitySet count) [bytes]
        /// Execution: ~  600 + 100 * (EntitySet count) [ns]
        /// </summary>
        [Test]
        public void BenchmarkCreateClient() {
            var env         = new SharedEnv();
            var hub         = new NoopDatabaseHub("noop_db", env);
            var _           = new PocStore(hub);
            var __          = new PocStore(hub);
            
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            int count = 1; // 1_000_000;
            for (int n = 0; n < count; n++) {
                new PocStore(hub);                      // ~ 1.2 µs (Release)
            }
            stopwatch.Stop();
            Console.WriteLine($"client instantiation count: {count}, ms: {stopwatch.ElapsedMilliseconds}");
            
            var start = GC.GetAllocatedBytesForCurrentThread();
            // ReSharper disable once UnusedVariable
            var store = new PocStore(hub);
            var diff = GC.GetAllocatedBytesForCurrentThread() - start;
            var platform    = Environment.OSVersion.Platform;
            var isWindows   = platform == PlatformID.Win32NT; 
            var expected    = isWindows ? 2712 : 2496;  // Test Windows & Linux
            Console.WriteLine($"PocStore allocation. platform: {platform}, memory: {diff}");
            AreEqual(expected, diff);
        }
        
        /// <see cref="ObjectPool{T}.Get"/> returns a <see cref="Pooled{T}"/> <see cref="FlioxClient"/> or create a new one.
        /// When leaving the using scope { } it calls <see cref="FlioxClient.Reset"/>.
        /// => Same behavior as new <see cref="FlioxClient"/>.
        [Test]
        public void BenchmarkPooledClient() {
            var env             = new SharedEnv();
            var hub             = new NoopDatabaseHub("noop_db", env);
            var pocStorePool    = new SharedPool<PocStore>(() => new PocStore(hub));
            FlioxClient client;
            using (var pooled = pocStorePool.Get()) {
                client = pooled.instance;
            }
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            int count = 1; // 10_000_000;
            for (int n = 0; n < count; n++) {
                using (var pooled = pocStorePool.Get()) // ~ 0.12 µs (Release)
                {
                    if (client != pooled.instance)
                        throw new InvalidOperationException ("Expect same reference");
                }
            }
            stopwatch.Stop();
            Console.WriteLine($"get pool client count: {count}, ms: {stopwatch.ElapsedMilliseconds}");
            
            var store = new PocStore(hub);
            var start = GC.GetAllocatedBytesForCurrentThread();
            store.Reset();
            var diff = GC.GetAllocatedBytesForCurrentThread() - start;
            var platform    = Environment.OSVersion.Platform;
            Console.WriteLine($"PocStore Reset. platform: {platform}, memory: {diff}");
            AreEqual(96, diff);
        }

        [Test]
        public async Task TestMemorySync() {
            var env         = new SharedEnv();
            var hub         = new NoopDatabaseHub("noop_db", env);
            var store       = new PocStore(hub);
            await store.SyncTasks();                    // force one time allocations
            // GC.Collect();
            
            var start = GC.GetAllocatedBytesForCurrentThread();
            await store.SyncTasks();                    // ~ 1 µs (Release)
            var diff = GC.GetAllocatedBytesForCurrentThread() - start;
            var expected = IsDebug() ? 1312 : 1224;     // Test Debug & Release
            AreEqual(expected, diff);
        }
        
        [Test]
        public async Task TestMemorySyncRead() {
            var env         = new SharedEnv();
            var database    = new MemoryDatabase("db");
            var hub         = new FlioxHub(database, env);
            var store       = new EntityIdStore(hub);
            var read = store.intEntities.Read();
            var ids = new int [100];
            for (int n = 0; n < 100; n++)
                ids[n] = n;
            read.FindRange(ids);
            await store.SyncTasks();                // force one time allocations
            
            var start = GC.GetAllocatedBytesForCurrentThread();
            for (int n = 0; n < 1; n++) {
                read = store.intEntities.Read();
                read.FindRange(ids);
                await store.SyncTasks();
            }
            var diff = GC.GetAllocatedBytesForCurrentThread() - start;
            var expected = IsDebug() ? Is.InRange(46952, 47296) : Is.InRange(44048, 44432); // Test Debug & Release
            That(diff, expected);
        }
        
        private static bool IsDebug() {
#if DEBUG
            return true;
#else
            return false;
#endif
        }

#endif
    }
}
