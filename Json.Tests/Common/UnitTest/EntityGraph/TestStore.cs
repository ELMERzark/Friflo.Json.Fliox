﻿using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.EntityGraph;
using Friflo.Json.EntityGraph.Database;
using Friflo.Json.Mapper;
using Friflo.Json.Tests.Common.Utils;
using Friflo.Json.Tests.Unity.Utils;
using UnityEngine.TestTools;
using static NUnit.Framework.Assert;

#if UNITY_5_3_OR_NEWER
    using UnitTest.Dummy;
#else
    using NUnit.Framework;
#endif


namespace Friflo.Json.Tests.Common.UnitTest.EntityGraph
{
    public class TestStore : LeakTestsFixture
    {
        [UnityTest] public IEnumerator  CollectAwaitCoroutine() { yield return RunAsync.Await(CollectAwait(), i => Log.Info("--- " + i)); }
        [Test]      public async Task   CollectAwaitAsync() { await CollectAwait(); }
        
        private async Task CollectAwait() {
            List<Task> tasks = new List<Task>();
            for (int n = 0; n < 1000; n++) {
                Task task = Task.Delay(1);
                tasks.Add(task);
            }
            await Task.WhenAll(tasks);
        }

        [UnityTest] public IEnumerator  ChainAwaitCoroutine() { yield return RunAsync.Await(ChainAwait(), i => Log.Info("--- " + i)); }
        [Test]      public async Task   ChainAwaitAsync() { await ChainAwait(); }
        private async Task ChainAwait() {
            for (int n = 0; n < 5; n++) {
                await Task.Delay(1);
            }
        }
        
        [UnityTest] public IEnumerator  WriteReadMemoryCreateCoroutine() { yield return RunAsync.Await(WriteReadMemoryCreate()); }
        [Test]      public async Task   WriteReadMemoryCreateAsync() { await WriteReadMemoryCreate(); }
        
        private async Task WriteReadMemoryCreate() {
            var database = new MemoryDatabase();
            using (var store = await TestRelationPoC.CreateStore(database)) {
                await WriteRead(store);
            }
        }
        
        [UnityTest] public IEnumerator WriteReadFileCreateCoroutine() { yield return RunAsync.Await(WriteReadFileCreate(), i => Log.Info("--- " + i)); }
        [Test]      public async Task  WriteReadFileCreateAsync() { await WriteReadFileCreate(); }

        private async Task WriteReadFileCreate() {
            var database = new FileDatabase(CommonUtils.GetBasePath() + "assets/db");
            using (var store = await TestRelationPoC.CreateStore(database)) {
                await WriteRead(store);
            }
        }
        
        [UnityTest] public IEnumerator WriteReadFileEmptyCoroutine() { yield return RunAsync.Await(WriteReadFileEmpty()); }
        [Test]      public async Task  WriteReadFileEmptyAsync() { await WriteReadFileEmpty(); }
        
        private async Task WriteReadFileEmpty() {
            var database = new FileDatabase(CommonUtils.GetBasePath() + "assets/db");
            using (var store = new PocStore(database)) {
                await WriteRead(store);
            }
        }

        private async Task WriteRead(PocStore store) {
            // --- cache empty
            var order = store.orders.Read("order-1");
            await store.Sync();
            // var xxx = order.Result.customer.Entity;

            WriteRead(order.Result, store);
            await AssertStore(order.Result, store);
        }
        
        private static void WriteRead(Order order, EntityStore store) {
            var m = store.jsonMapper;
            m.Pretty = true;
            
            AssertWriteRead(m, order);
            AssertWriteRead(m, order.customer);
            AssertWriteRead(m, order.items[0]);
            AssertWriteRead(m, order.items[1]);
            AssertWriteRead(m, order.items[0].article);
            AssertWriteRead(m, order.items[1].article);
        }

        private static async Task AssertStore(Order order, PocStore store) {
            var order1 =    store.orders.Read("order-1");
            // await store.Sync();
            
            var article1 =  store.articles.Read("article-1");
            var article2 =  store.articles.Read("article-2");
            var customer1 = store.customers.Read("customer-1");

            await store.Sync();
            
            // AreEqual(1, store.customers.Count);
            // AreEqual(2, store.articles.Count);
            // AreEqual(1, store.orders.Count);

            IsTrue(order1.Result      == order);
            IsTrue(customer1.Result   == order.customer.Entity);
            IsTrue(article1.Result    == order.items[0].article.Entity);
            IsTrue(article2.Result    == order.items[1].article.Entity);
        }

        private static void AssertWriteRead<T>(JsonMapper m, T entity) {
            var json    = m.Write(entity);
            var result  = m.Read<T>(json);
            AssertUtils.Equivalent(entity, result);
            // IsTrue(entity.Equals(result)); // references are equal
        }
    }
}