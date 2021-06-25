﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Burst;
using Friflo.Json.Flow.Database;
using Friflo.Json.Flow.Database.Utils;
using Friflo.Json.Tests.Common.Utils;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Flow.Graph.Happy
{
    public partial class TestStore
    {
        [Test] public static void TestConcurrentAccessSync () {
            SingleThreadSynchronizationContext.Run(async () => {
                using (var fileDatabase = new FileDatabase(CommonUtils.GetBasePath() + "assets/db")) {
                    await AssertConcurrentAccess(fileDatabase, 2, 2, 10);
                }
            });
        }
        
        private static async Task AssertConcurrentAccess(EntityDatabase database, int readerCount, int writerCount, int requestCount) {
            DebugUtils.StopLeakDetection();
            using (var _            = Pools.SharedPools) // for LeakTestsFixture
            {
                var readerStores = new List<PocStore>();
                var writerStores = new List<PocStore>();
                try {
                    for (int n = 0; n < readerCount; n++) {
                        readerStores.Add(new PocStore(database, $"reader-{n}"));
                    }
                    for (int n = 0; n < writerCount; n++) {
                        writerStores.Add(new PocStore(database, $"writer-{n}"));
                    }

                    const string    id          = "concurrent-access";
                    var             employee    = new Employee { id = id, firstName = "Concurrent accessed entity"};
                    
                    var tasks = new List<Task>();

                    foreach (var readerStore in readerStores) {
                        tasks.Add(ReadLoop (readerStore, id, requestCount));
                    }
                    foreach (var writerStore in writerStores) {
                        tasks.Add(WriteLoop (writerStore, employee, requestCount));
                    }
                    await Task.WhenAll(tasks);
                }
                finally {
                    foreach (var readerStore in readerStores)
                        readerStore.Dispose();
                    foreach (var writerStore in writerStores) {
                        writerStore.Dispose();
                    }
                }
            }
        }

        private static Task ReadLoop (PocStore store, string id, int requestCount) {
            return Task.Run(async () => {
                for (int n= 0; n < requestCount; n++) {
                    var readEmployee = store.employees.Read();
                    readEmployee.Find(id);
                    await store.Sync();
                    AreEqual (1, readEmployee.Results.Count);
                }
            });
        }
        
        private static Task WriteLoop (PocStore store, Employee employee, int requestCount) {
            return Task.Run(async () => {
                for (int n= 0; n < requestCount; n++) {
                    store.employees.Create(employee);
                    await store.Sync();
                }
            });
        }
    }
}