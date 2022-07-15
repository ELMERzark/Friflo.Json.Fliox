﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Client;
using static NUnit.Framework.Assert;

#if UNITY_5_3_OR_NEWER
    using UnitTest.Dummy;
#else
    using NUnit.Framework;
#endif

// ReSharper disable PossibleNullReferenceException
// ReSharper disable JoinDeclarationAndInitializer
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Client.Happy
{
    public partial class TestHappy
    {
        [Test] public async Task TestRead       () { await TestCreate(async (store) => await AssertRead             (store)); }
        
        /// Optimization: <see cref="RelationPath{TRef}"/> and <see cref="RelationsPath{TRef}"/> can be created static as creating
        /// a path from a <see cref="System.Linq.Expressions.Expression"/> is costly regarding heap allocations and CPU.
         
        private static readonly RelationPath <Customer> OrderCustomer = RelationPath<Customer>.Create <string,Order>(o => o.customer);
        private static readonly RelationsPath<Article>  ItemsArticle  = RelationsPath<Article>.Create<string,Order>(o => o.items.Select(a => a.article));
        
        private static async Task AssertRead(PocStore store) {
            var orders      = store.orders;
            var articles    = store.articles;
            var producers   = store.producers;
            var customers   = store.customers;
            
            var readOrders  = orders.Read()                                      .TaskName("readOrders");
            var order1Task  = readOrders.Find("order-1")                         .TaskName("order1Task");
            await store.SyncTasks();
            
            // schedule ReadRefs on an already synced Read operation
            Exception e;
            var orderCustomer = orders.RelationPath(customers, o => o.customer);
            AreEqual(OrderCustomer.path, orderCustomer.path);
            e = Throws<TaskAlreadySyncedException>(() => { readOrders.ReadRelation(customers, orderCustomer); });
            AreEqual("Task already executed. readOrders", e.Message);
            var itemsArticle = orders.RelationsPath(articles, o => o.items.Select(a => a.article));
            AreEqual(ItemsArticle.path, itemsArticle.path);
            e = Throws<TaskAlreadySyncedException>(() => { readOrders.ReadRelations(articles, itemsArticle); });
            AreEqual("Task already executed. readOrders", e.Message);
            
            // todo add Read() without ids 

            readOrders              = orders.Read()                                             .TaskName("readOrders");
            var order1              = readOrders.Find("order-1")                                .TaskName("order1");
            var articleRefsTask     = readOrders.ReadRelations(articles, itemsArticle);
            var articleRefsTask2    = readOrders.ReadRelations(articles, itemsArticle);
            AreSame(articleRefsTask, articleRefsTask2);
            
            var articleRefsTask3 = readOrders.ReadRelations(articles, o => o.items.Select(a => a.article));
            AreSame(articleRefsTask, articleRefsTask3);
            AreEqual("readOrders -> .items[*].article", articleRefsTask.Details);

            e = Throws<TaskNotSyncedException>(() => { var _ = articleRefsTask.Result; });
            AreEqual("ReadRelations.Result requires SyncTasks(). readOrders -> .items[*].article", e.Message);

            var articleProducerTask = articleRefsTask.ReadRelations(producers, a => a.producer);
            AreEqual("readOrders -> .items[*].article -> .producer", articleProducerTask.Details);

            var readTask        = articles.Read()                                           .TaskName("readTask");
            var duplicateId     = "article-galaxy"; // support duplicate ids
            var galaxy          = readTask.Find(duplicateId)                                .TaskName("galaxy");
            var article1And2    = readTask.FindRange(new [] {"article-1", "article-2"})     .TaskName("article1And2");
            var articleSetIds   = new [] {duplicateId, duplicateId, "article-ipad"};
            var articleSet      = readTask.FindRange(articleSetIds)                         .TaskName("articleSet");
            var articleMissing  = readTask.Find("article-missing")                          .TaskName("article-missing");

            AreEqual(@"readOrders
order1
readOrders -> .items[*].article
readOrders -> .items[*].article -> .producer
readTask
galaxy
article1And2
articleSet
article-missing", string.Join("\n", store.Functions));

            await store.SyncTasks(); // ----------------
        
            AreEqual(2,                 articleRefsTask.Result.Count);
            AreEqual("Changed name",    articleRefsTask.Result.Find(i => i.id == "article-1").name);
            AreEqual("Smartphone",      articleRefsTask.Result.Find(i => i.id == "article-2").name);
            
            AreEqual(1,                 articleProducerTask.Result.Count);
            AreEqual("Canon",           articleProducerTask.Result.Find(i => i.id == "producer-canon").name);

            AreEqual(2,                 articleSet.Result.Count);
            AreEqual("Galaxy S10",      articleSet.Result["article-galaxy"].name);
            AreEqual("iPad Pro",        articleSet.Result["article-ipad"].name);
            IsNull(articleMissing.Result);
            
            AreEqual("Galaxy S10",      galaxy.Result.name);
            
            AreEqual(2,                 article1And2.Result.Count);
            AreEqual("Smartphone",      article1And2.Result["article-2"].name);
            
            AreEqual(5,                 readTask.Result.Count);
            AreEqual("Galaxy S10",      readTask.Result["article-galaxy"].name);
            
            var localArticles           = articles.Local;
            var localArticleEntities    = localArticles.Entities;
            var localArticleKey         = localArticles.Keys;
            
            AreEqual(7,                 localArticleEntities.Length);
            AreEqual(7,                 localArticleKey.Length);
            NotNull(localArticles["article-galaxy"]);
            IsNull (localArticles["article-missing"]);
            e = Throws<KeyNotFoundException>(() => { var _ = localArticles["foo"]; });
            AreEqual("key 'foo' not found in articles.Local", e.Message);
        }
    }
}