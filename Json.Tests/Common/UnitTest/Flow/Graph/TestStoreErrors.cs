﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using Friflo.Json.Flow.Database;
using Friflo.Json.Flow.Database.Remote;
using Friflo.Json.Flow.Graph;
using Friflo.Json.Flow.Sync;
using Friflo.Json.Tests.Common.Utils;
using Friflo.Json.Tests.Unity.Utils;
using UnityEngine.TestTools;
using static NUnit.Framework.Assert;

#if UNITY_5_3_OR_NEWER
    using UnitTest.Dummy;
#else
    using NUnit.Framework;
#endif

// ReSharper disable JoinDeclarationAndInitializer
namespace Friflo.Json.Tests.Common.UnitTest.Flow.Graph
{
    public class TestStoreErrors : LeakTestsFixture
    {
        /// withdraw from allocation detection by <see cref="LeakTestsFixture"/> => init before tracking starts
        static TestStoreErrors() { SyncTypeStore.Init(); }

        
        [UnityTest] public IEnumerator FileUseCoroutine() { yield return RunAsync.Await(FileUse()); }
        [Test]      public async Task  FileUseAsync() { await FileUse(); }
        
        private async Task FileUse() {
            using (var _            = Pools.SharedPools) // for LeakTestsFixture
            using (var fileDatabase = new FileDatabase(CommonUtils.GetBasePath() + "assets/db"))
            using (var testDatabase = new TestDatabase(fileDatabase))
            using (var useStore     = new PocStore(testDatabase)) {
                await TestStoresErrors(useStore, testDatabase);
            }
        }
        
        [UnityTest] public IEnumerator LoopbackUseCoroutine() { yield return RunAsync.Await(LoopbackUse()); }
        [Test]      public async Task  LoopbackUseAsync() { await LoopbackUse(); }
        
        private async Task LoopbackUse() {
            using (var _                = Pools.SharedPools) // for LeakTestsFixture
            using (var fileDatabase     = new FileDatabase(CommonUtils.GetBasePath() + "assets/db"))
            using (var testDatabase     = new TestDatabase(fileDatabase))
            using (var loopbackDatabase = new LoopbackDatabase(testDatabase))
            using (var useStore         = new PocStore(loopbackDatabase)) {
                await TestStoresErrors(useStore, testDatabase);
            }
        }
        
        [UnityTest] public IEnumerator HttpUseCoroutine() { yield return RunAsync.Await(HttpUse()); }
        [Test]      public async Task  HttpUseAsync() { await HttpUse(); }
        
        private async Task HttpUse() {
            using (var _            = Pools.SharedPools) // for LeakTestsFixture
            using (var fileDatabase = new FileDatabase(CommonUtils.GetBasePath() + "assets/db"))
            using (var testDatabase = new TestDatabase(fileDatabase))
            using (var hostDatabase = new HttpHostDatabase(testDatabase, "http://+:8080/")) {
                await TestStore.RunRemoteHost(hostDatabase, async () => {
                    using (var remoteDatabase   = new HttpClientDatabase("http://localhost:8080/"))
                    using (var useStore         = new PocStore(remoteDatabase)) {
                        await TestStoresErrors(useStore, testDatabase);
                    }
                });
            }
        }

        // following strings are used as entity ids to invoke a handled <see cref="TaskError"/> via <see cref="TestContainer"/>

        // following strings are used as entity ids to invoke an <see cref="TaskErrorType.UnhandledException"/> via <see cref="TestContainer"/>
        // These test assertions ensure that all unhandled exceptions (bugs) are caught in a <see cref="EntityContainer"/> implementation.

        
        private static async Task TestStoresErrors(PocStore useStore, TestDatabase testDatabase) {
            await AssertQueryTask       (useStore, testDatabase);
            await AssertReadTask        (useStore, testDatabase);
            await AssertTaskExceptions  (useStore, testDatabase);
            await AssertTaskError       (useStore, testDatabase);
            await AssertEntityWrite     (useStore, testDatabase);
            await AssertEntityPatch     (useStore, testDatabase);
            await AssertLogChangesPatch (useStore, testDatabase);
            await AssertLogChangesCreate(useStore, testDatabase);
            await AssertSyncErrors      (useStore, testDatabase); }

        private const string ArticleError = @"EntityErrors ~ count: 2
| ReadError: Article 'article-1', simulated read entity error
| ParseError: Article 'article-2', JsonParser/JSON error: Expected ':' after key. Found: X path: 'invalidJson' at position: 16";
        
        private static async Task AssertQueryTask(PocStore store, TestDatabase testDatabase) {
            testDatabase.ClearErrors();
            const string article1ReadError      = "article-1";
            const string article2JsonError      = "article-2";
            const string readTaskError          = "read-task-error";
            
            var testArticles  = testDatabase.GetTestContainer("Article");
            var testCustomers = testDatabase.GetTestContainer("Customer");
            
            testArticles.readEntityErrors.Add(article2JsonError, (value) => value.SetJson(@"{""invalidJson"" XXX}"));
            testArticles.readEntityErrors.Add(article1ReadError, (value) => value.SetError(testArticles.ReadError(article1ReadError)));
            testCustomers.readTaskErrors.Add(readTaskError,     () => new CommandError{message = "simulated read task error"});

            var orders = store.orders;
            var articles = store.articles;

            var readOrders  = orders.Read()                                                         .TaskName("readOrders");
            var order1      = readOrders.Find("order-1")                                            .TaskName("order1");
            AreEqual("Find<Order> (id: 'order-1')", order1.Details);
            var allArticles             = articles.QueryAll().TaskName("allArticles")               .TaskName("allArticles");
            var producersTask           = allArticles.ReadRefs(a => a.producer);
            var hasOrderCamera          = orders.Query(o => o.items.Any(i => i.name == "Camera"))   .TaskName("hasOrderCamera");
            var ordersWithCustomer1     = orders.Query(o => o.customer.id == "customer-1")          .TaskName("ordersWithCustomer1");
            var read3                   = orders.Query(o => o.items.Count(i => i.amount < 1) > 0)   .TaskName("read3");
            var ordersAnyAmountLower2   = orders.Query(o => o.items.Any(i => i.amount < 2))         .TaskName("ordersAnyAmountLower2");
            var ordersAllAmountGreater0 = orders.Query(o => o.items.All(i => i.amount > 0))         .TaskName("ordersAllAmountGreater0");
            var orders2WithTaskError    = orders.Query(o => o.customer.id == readTaskError)         .TaskName("orders2WithTaskError");
            var order2CustomerError     = orders2WithTaskError.ReadRefs(o => o.customer);
            
            AreEqual("ReadTask<Order> (#ids: 1)",                                       readOrders              .Details);
            AreEqual("Find<Order> (id: 'order-1')",                                     order1                  .Details);
            AreEqual("QueryTask<Article> (filter: true)",                               allArticles             .Details);
            AreEqual("allArticles -> .producer",                                        producersTask           .Details);
            AreEqual("QueryTask<Order> (filter: .items.Any(i => i.name == 'Camera'))",  hasOrderCamera          .Details);
            AreEqual("QueryTask<Order> (filter: .customer == 'customer-1')",            ordersWithCustomer1     .Details);
            AreEqual("QueryTask<Order> (filter: .items.Count() > 0)",                   read3                   .Details);
            AreEqual("QueryTask<Order> (filter: .items.Any(i => i.amount < 2))",        ordersAnyAmountLower2   .Details);
            AreEqual("QueryTask<Order> (filter: .items.All(i => i.amount > 0))",        ordersAllAmountGreater0 .Details);
            AreEqual("QueryTask<Order> (filter: .customer == 'read-task-error')",       orders2WithTaskError    .Details);
            AreEqual("orders2WithTaskError -> .customer",                               order2CustomerError     .Details);

            var orderCustomer   = orders.RefPath(o => o.customer);
            var customer        = readOrders.ReadRefPath(orderCustomer);
            var customer2       = readOrders.ReadRefPath(orderCustomer);
            AreSame(customer, customer2);
            var customer3       = readOrders.ReadRef(o => o.customer);
            AreSame(customer, customer3);
            AreEqual("readOrders -> .customer", customer.ToString());

            var readOrders2     = orders.Read()                                                     .TaskName("readOrders2");
            var order2          = readOrders2.Find("order-2")                                       .TaskName("order2");
            var order2Customer  = readOrders2.ReadRefPath(orderCustomer);
            
            AreEqual("readOrders -> .customer",         customer        .Details);
            AreEqual("ReadTask<Order> (#ids: 1)",       readOrders2     .Details);
            AreEqual("Find<Order> (id: 'order-2')",     order2          .Details);
            AreEqual("readOrders2 -> .customer",        order2Customer  .Details);

            Exception e;
            e = Throws<TaskNotSyncedException>(() => { var _ = customer.Id; });
            AreEqual("ReadRefTask.Id requires Sync(). readOrders -> .customer", e.Message);
            e = Throws<TaskNotSyncedException>(() => { var _ = customer.Result; });
            AreEqual("ReadRefTask.Result requires Sync(). readOrders -> .customer", e.Message);

            e = Throws<TaskNotSyncedException>(() => { var _ = hasOrderCamera.Results; });
            AreEqual("QueryTask.Result requires Sync(). hasOrderCamera", e.Message);
            e = Throws<TaskNotSyncedException>(() => { var _ = hasOrderCamera["arbitrary"]; });
            AreEqual("QueryTask[] requires Sync(). hasOrderCamera", e.Message);

            var producerEmployees = producersTask.ReadArrayRefs(p => p.employeeList);
            AreEqual("allArticles -> .producer -> .employees[*]", producerEmployees.Details);
            
            var sync = await store.TrySync(); // -------- Sync --------
            
            IsFalse(sync.Success);
            AreEqual("tasks: 14, failed: 5", sync.ToString());
            AreEqual(14, sync.tasks.Count);
            AreEqual(5,  sync.failed.Count);
            const string msg = @"Sync() failed with task errors. Count: 5
|- allArticles # EntityErrors ~ count: 2
|   ReadError: Article 'article-1', simulated read entity error
|   ParseError: Article 'article-2', JsonParser/JSON error: Expected ':' after key. Found: X path: 'invalidJson' at position: 16
|- allArticles -> .producer # EntityErrors ~ count: 2
|   ReadError: Article 'article-1', simulated read entity error
|   ParseError: Article 'article-2', JsonParser/JSON error: Expected ':' after key. Found: X path: 'invalidJson' at position: 16
|- orders2WithTaskError -> .customer # DatabaseError ~ read references failed: 'Order -> .customer' - simulated read task error
|- readOrders2 -> .customer # DatabaseError ~ read references failed: 'Order -> .customer' - simulated read task error
|- allArticles -> .producer -> .employees[*] # EntityErrors ~ count: 2
|   ReadError: Article 'article-1', simulated read entity error
|   ParseError: Article 'article-2', JsonParser/JSON error: Expected ':' after key. Found: X path: 'invalidJson' at position: 16";
            AreEqual(msg, sync.Message);
            
            AreEqual(1,                 ordersWithCustomer1.Results.Count);
            NotNull(ordersWithCustomer1["order-1"]);
            
            AreEqual(1,                 ordersAnyAmountLower2.Results.Count);
            NotNull(ordersAnyAmountLower2["order-1"]);
            
            AreEqual(2,                 ordersAllAmountGreater0.Results.Count);
            NotNull(ordersAllAmountGreater0["order-1"]);


            IsFalse(allArticles.Success);
            AreEqual(2, allArticles.Error.entityErrors.Count);
            AreEqual(ArticleError, allArticles.Error.ToString());
            
            TaskResultException te;
            te = Throws<TaskResultException>(() => { var _ = allArticles.Results; });
            AreEqual(ArticleError, te.Message);
            AreEqual(2, te.error.entityErrors.Count);
            
            te = Throws<TaskResultException>(() => { var _ = allArticles.Results["article-galaxy"]; });
            AreEqual(ArticleError, te.Message);
            AreEqual(2, te.error.entityErrors.Count);
            
            AreEqual(1,                 hasOrderCamera.Results.Count);
            AreEqual(3,                 hasOrderCamera["order-1"].items.Count);
    
            AreEqual("customer-1",      customer.Id);
            AreEqual("Smith Ltd.",      customer.Result.name);
                
            IsFalse(producersTask.Success);
            te = Throws<TaskResultException>(() => { var _ = producersTask.Results; });
            AreEqual(ArticleError, te.Message);
            AreEqual(2, te.error.entityErrors.Count);
                
            IsFalse(producerEmployees.Success);
            te = Throws<TaskResultException>(() => { var _ = producerEmployees.Results; });
            AreEqual(ArticleError, te.Message);
            AreEqual(2, te.error.entityErrors.Count);
            
            IsTrue(readOrders2.Success);
            IsTrue(order2.Success);
            IsFalse(order2Customer.Success);
            AreEqual("read-task-error", readOrders2["order-2"].customer.id);
            AreEqual("read-task-error", order2.Result.customer.id);
            AreEqual("DatabaseError ~ read references failed: 'Order -> .customer' - simulated read task error", order2Customer.   Error.ToString());
            
            IsTrue(orders2WithTaskError.Success);
            IsFalse(order2CustomerError.Success);
            AreEqual("read-task-error", orders2WithTaskError.Results["order-2"].customer.id);
            AreEqual("DatabaseError ~ read references failed: 'Order -> .customer' - simulated read task error", order2CustomerError.  Error.ToString());
        }
        
        private static async Task AssertReadTask(PocStore store, TestDatabase testDatabase) {
            testDatabase.ClearErrors();
            
            TestContainer testArticles = testDatabase.GetTestContainer("Article");
            const string article1ReadError      = "article-1";
            const string article2JsonError      = "article-2";
            const string articleInvalidJson     = "article-invalidJson";
            const string articleIdDontMatch     = "article-idDontMatch";
            
            testArticles.readEntityErrors.Add(article2JsonError,    (value) => value.SetJson(@"{""invalidJson"" XXX}"));
            testArticles.readEntityErrors.Add(article1ReadError,    (value) => value.SetError(testArticles.ReadError(article1ReadError)));
            testArticles.readEntityErrors.Add(articleInvalidJson,   (value) => value.SetJson(@"{""invalidJson"" YYY}"));
            testArticles.readEntityErrors.Add(articleIdDontMatch,   (value) => value.SetJson(@"{""id"": ""article-unexpected-id"""));
            
            var orders = store.orders;
            var readOrders = orders.Read();
            var order1Task = readOrders.Find("order-1");
            await store.Sync();
            
            // schedule ReadRefs on an already synced Read operation
            Exception e;
            var orderCustomer = orders.RefPath(o => o.customer);
            e = Throws<TaskAlreadySyncedException>(() => { readOrders.ReadRefPath(orderCustomer); });
            AreEqual("Task already synced. ReadTask<Order> (#ids: 1)", e.Message);
            var itemsArticle = orders.RefsPath(o => o.items.Select(a => a.article));
            e = Throws<TaskAlreadySyncedException>(() => { readOrders.ReadRefsPath(itemsArticle); });
            AreEqual("Task already synced. ReadTask<Order> (#ids: 1)", e.Message);
            
            // todo add Read() without ids 

            readOrders              = orders.Read()                                                 .TaskName("readOrders");
            var order1              = readOrders.Find("order-1")                                    .TaskName("order1");
            var articleRefsTask     = readOrders.ReadRefsPath(itemsArticle);
            var articleRefsTask2    = readOrders.ReadRefsPath(itemsArticle);
            AreSame(articleRefsTask, articleRefsTask2);
            
            var articleRefsTask3 = readOrders.ReadArrayRefs(o => o.items.Select(a => a.article));
            AreSame(articleRefsTask, articleRefsTask3);
            AreEqual("readOrders -> .items[*].article", articleRefsTask.ToString());

            e = Throws<TaskNotSyncedException>(() => { var _ = articleRefsTask["article-1"]; });
            AreEqual("ReadRefsTask[] requires Sync(). readOrders -> .items[*].article", e.Message);
            e = Throws<TaskNotSyncedException>(() => { var _ = articleRefsTask.Results; });
            AreEqual("ReadRefsTask.Results requires Sync(). readOrders -> .items[*].article", e.Message);

            var articleProducerTask = articleRefsTask.ReadRefs(a => a.producer);
            AreEqual("readOrders -> .items[*].article -> .producer", articleProducerTask.ToString());

            var duplicateId     = "article-galaxy"; // support duplicate ids
            
            var readTask1       = store.articles.Read()                                             .TaskName("readTask1");
            var galaxy          = readTask1.Find(duplicateId)                                       .TaskName("galaxy");
            var article1        = readTask1.Find(article1ReadError)                                 .TaskName("article1");
            var article1And2    = readTask1.FindRange(new [] {article1ReadError, article2JsonError}).TaskName("article1And2");
            var articleSet      = readTask1.FindRange(new [] {duplicateId, duplicateId, "article-ipad"}).TaskName("articleSet");
            
            var readTask2       = store.articles.Read()                                             .TaskName("readTask2"); // separate Read without errors
            var galaxy2         = readTask2.Find(duplicateId)                                       .TaskName("galaxy2");
            
            var readTask3       = store.articles.Read()                                             .TaskName("readTask3");
            var invalidJson     = readTask3.Find(articleInvalidJson)                                .TaskName("invalidJson");
            var idDontMatch     = readTask3.Find(articleIdDontMatch)                                .TaskName("idDontMatch");

            // test throwing exception in case of task or entity errors
            try {
                await store.Sync(); // -------- Sync --------
                
                Fail("Sync() intended to fail - code cannot be reached");
            } catch (SyncResultException sre) {
                AreEqual(6, sre.failed.Count);
                const string expect = @"Sync() failed with task errors. Count: 6
|- readOrders -> .items[*].article # EntityErrors ~ count: 2
|   ReadError: Article 'article-1', simulated read entity error
|   ParseError: Article 'article-2', JsonParser/JSON error: Expected ':' after key. Found: X path: 'invalidJson' at position: 16
|- readOrders -> .items[*].article -> .producer # EntityErrors ~ count: 2
|   ReadError: Article 'article-1', simulated read entity error
|   ParseError: Article 'article-2', JsonParser/JSON error: Expected ':' after key. Found: X path: 'invalidJson' at position: 16
|- article1 # EntityErrors ~ count: 1
|   ReadError: Article 'article-1', simulated read entity error
|- article1And2 # EntityErrors ~ count: 2
|   ReadError: Article 'article-1', simulated read entity error
|   ParseError: Article 'article-2', JsonParser/JSON error: Expected ':' after key. Found: X path: 'invalidJson' at position: 16
|- invalidJson # EntityErrors ~ count: 1
|   ParseError: Article 'article-invalidJson', JsonParser/JSON error: Expected ':' after key. Found: Y path: 'invalidJson' at position: 16
|- idDontMatch # EntityErrors ~ count: 1
|   ParseError: Article 'article-idDontMatch', entity id does not match key. id: article-unexpected-id";
                AreEqual(expect, sre.Message);
            }
            
            IsTrue(readOrders.Success);
            AreEqual(3, readOrders.Results["order-1"].items.Count);
            // readOrders is successful
            // but resolving its Ref<>'s (.items[*].article and .items[*].article > .producer) failed:
            
            IsFalse(articleRefsTask.Success);
            AreEqual(ArticleError, articleRefsTask.Error.ToString());
            
            IsFalse(articleProducerTask.Success);
            AreEqual(ArticleError, articleProducerTask.Error.ToString());

            // readTask1 failed - A ReadTask<> fails, if any FindTask<> of it failed.
            TaskResultException te;
            
            IsFalse(readTask1.Success);
            te = Throws<TaskResultException>(() => { var _ = readTask1.Results; });
            AreEqual(ArticleError, te.Message);
            AreEqual(2, te.error.entityErrors.Count);
            
            IsTrue(articleSet.Success);
            AreEqual(2,             articleSet.Results.Count);
            AreEqual("Galaxy S10",  articleSet.Results[duplicateId].name);
            AreEqual("iPad Pro",    articleSet.Results["article-ipad"].name);

            IsTrue(galaxy.Success);
            AreEqual("Galaxy S10",  galaxy.Result.name);
            
            IsFalse(article1.Success);
            AreEqual(@"EntityErrors ~ count: 1
| ReadError: Article 'article-1', simulated read entity error", article1.Error.ToString());

            te = Throws<TaskResultException>(() => { var _ = article1And2.Results; });
            AreEqual(ArticleError, te.Message);
            AreEqual(2, te.error.entityErrors.Count);
            
            // readTask2 succeed - All it FindTask<> were successful
            IsTrue(readTask2.Success);
            IsTrue(galaxy2.Success);
            AreEqual("Galaxy S10", galaxy2.Result.name); 
            
            IsFalse(invalidJson.Success);
            AreEqual(@"EntityErrors ~ count: 1
| ParseError: Article 'article-invalidJson', JsonParser/JSON error: Expected ':' after key. Found: Y path: 'invalidJson' at position: 16", invalidJson.Error.ToString());
            
            IsFalse(idDontMatch.Success);
            AreEqual(@"EntityErrors ~ count: 1
| ParseError: Article 'article-idDontMatch', entity id does not match key. id: article-unexpected-id", idDontMatch.Error.ToString());
            
        }

        private static async Task AssertTaskExceptions(PocStore store, TestDatabase testDatabase) {
            testDatabase.ClearErrors();
            TestContainer testCustomers = testDatabase.GetTestContainer("Customer");
            const string readTaskException      = "read-task-exception"; // throws an exception also for a Query
            const string createTaskException    = "create-task-exception";
            const string updateTaskException    = "update-task-exception";
            const string deleteTaskException    = "delete-task-exception";
            
            testCustomers.readTaskErrors.Add(readTaskException,     () => throw new SimulationException("simulated read task exception"));
            testCustomers.writeTaskErrors.Add(createTaskException,  () => throw new SimulationException("simulated write task exception"));
            testCustomers.writeTaskErrors.Add(updateTaskException,  () => throw new SimulationException("simulated write task exception"));
            testCustomers.writeTaskErrors.Add(deleteTaskException,  () => throw new SimulationException("simulated write task exception"));
            // Query(c => c.id == "query-task-exception")
            testCustomers.queryErrors.Add(".id == 'query-task-exception'", () => throw new SimulationException("simulated query exception"));

            
            var customers = store.customers;

            var readCustomers   = customers.Read()                                          .TaskName("readCustomers");
            var customerRead    = readCustomers.Find(readTaskException)                     .TaskName("customerRead");
            var customerQuery   = customers.Query(c => c.id == "query-task-exception")      .TaskName("customerQuery");

            var createError     = customers.Create(new Customer{id = createTaskException})  .TaskName("createError");
            var updateError     = customers.Update(new Customer{id = updateTaskException})  .TaskName("updateError");
            var deleteError     = customers.Delete(new Customer{id = deleteTaskException})  .TaskName("deleteError");
            
            AreEqual("CreateTask<Customer> (#ids: 1)", createError.Details);
            AreEqual("UpdateTask<Customer> (#ids: 1)", updateError.Details);
            AreEqual("DeleteTask<Customer> (#ids: 1)", deleteError.Details);
            
            Exception e;
            e = Throws<TaskNotSyncedException>(() => { var _ = createError.Success; });
            AreEqual("SyncTask.Success requires Sync(). createError", e.Message);
            e = Throws<TaskNotSyncedException>(() => { var _ = createError.Error; });
            AreEqual("SyncTask.Error requires Sync(). createError", e.Message);

            var sync = await store.TrySync(); // -------- Sync --------
            
            AreEqual("tasks: 5, failed: 5", sync.ToString());
            AreEqual(@"Sync() failed with task errors. Count: 5
|- customerRead # UnhandledException ~ SimulationException: simulated read task exception
|- customerQuery # UnhandledException ~ SimulationException: simulated query exception
|- createError # UnhandledException ~ SimulationException: simulated write task exception
|- updateError # UnhandledException ~ SimulationException: simulated write task exception
|- deleteError # UnhandledException ~ SimulationException: simulated write task exception", sync.Message);
            
            TaskResultException te;
            te = Throws<TaskResultException>(() => { var _ = customerRead.Result; });
            AreEqual("UnhandledException ~ SimulationException: simulated read task exception", te.Message); // No stacktrace by intention
            AreEqual("SimulationException: simulated read task exception", te.error.taskMessage);

            te = Throws<TaskResultException>(() => { var _ = customerQuery.Results; });
            AreEqual("UnhandledException ~ SimulationException: simulated query exception", te.error.Message);

            IsFalse(createError.Success);
            AreEqual("UnhandledException ~ SimulationException: simulated write task exception", createError.Error.Message);
            
            IsFalse(updateError.Success);
            AreEqual("UnhandledException ~ SimulationException: simulated write task exception", updateError.Error.Message);
            
            IsFalse(deleteError.Success);
            AreEqual("UnhandledException ~ SimulationException: simulated write task exception", deleteError.Error.Message);
        }
        
        private static async Task AssertTaskError(PocStore store, TestDatabase testDatabase) {
            testDatabase.ClearErrors();
            TestContainer testCustomers = testDatabase.GetTestContainer("Customer");
            
            const string createTaskError        = "create-task-error";
            const string updateTaskError        = "update-task-error";
            const string deleteTaskError        = "delete-task-error";
            const string readTaskError          = "read-task-error";
            
            testCustomers.writeTaskErrors.Add(createTaskError,  () => new CommandError {message = "simulated write task error"});
            testCustomers.writeTaskErrors.Add(updateTaskError,  () => new CommandError {message = "simulated write task error"});
            testCustomers.writeTaskErrors.Add(deleteTaskError,  () => new CommandError {message = "simulated write task error"});
            testCustomers.readTaskErrors.Add(readTaskError,     () => new CommandError{message = "simulated read task error"});
            // Query(c => c.id == "query-task-error")
            testCustomers.queryErrors.Add(".id == 'query-task-error'", () => new QueryEntitiesResult {Error = new CommandError {message = "simulated query error"}});
        
            var customers = store.customers;

            var readCustomers   = customers.Read()                                      .TaskName("readCustomers");
            var customerRead    = readCustomers.Find(readTaskError)                     .TaskName("customerRead");
            var customerQuery   = customers.Query(c => c.id == "query-task-error")      .TaskName("customerQuery");
            
            var createError     = customers.Create(new Customer{id = createTaskError})  .TaskName("createError");
            var updateError     = customers.Update(new Customer{id = updateTaskError})  .TaskName("updateError");
            var deleteError     = customers.Delete(new Customer{id = deleteTaskError})  .TaskName("deleteError");
            
            var sync = await store.TrySync(); // -------- Sync --------
            AreEqual("tasks: 5, failed: 5", sync.ToString());
            AreEqual("tasks: 5, failed: 5", sync.ToString());
            AreEqual(@"Sync() failed with task errors. Count: 5
|- customerRead # DatabaseError ~ simulated read task error
|- customerQuery # DatabaseError ~ simulated query error
|- createError # DatabaseError ~ simulated write task error
|- updateError # DatabaseError ~ simulated write task error
|- deleteError # DatabaseError ~ simulated write task error", sync.Message);

            TaskResultException te;
            te = Throws<TaskResultException>(() => { var _ = customerRead.Result; });
            AreEqual("DatabaseError ~ simulated read task error", te.Message);
            AreEqual(TaskErrorType.DatabaseError, customerRead.Error.type);
            
            te = Throws<TaskResultException>(() => { var _ = customerQuery.Results; });
            AreEqual("DatabaseError ~ simulated query error", te.Message);
            AreEqual(TaskErrorType.DatabaseError, customerQuery.Error.type);
            
            IsFalse(createError.Success);
            AreEqual("DatabaseError ~ simulated write task error", createError.Error.Message);
            
            IsFalse(updateError.Success);
            AreEqual("DatabaseError ~ simulated write task error", updateError.Error.Message);
            
            IsFalse(deleteError.Success);
            AreEqual("DatabaseError ~ simulated write task error", deleteError.Error.Message);
        }
        
        private static async Task AssertEntityWrite(PocStore store, TestDatabase testDatabase) {
            testDatabase.ClearErrors();
            TestContainer testCustomers = testDatabase.GetTestContainer("Customer");
            
            const string deleteEntityError      = "delete-entity-error";
            const string createEntityError      = "create-entity-error";
            const string updateEntityError      = "update-entity-error";
            
            testCustomers.writeEntityErrors.Add(deleteEntityError,    () => testCustomers.WriteError(deleteEntityError));
            testCustomers.writeEntityErrors.Add(createEntityError,    () => testCustomers.WriteError(createEntityError));
            testCustomers.writeEntityErrors.Add(updateEntityError,    () => testCustomers.WriteError(updateEntityError));
            
            var customers = store.customers;
            
            var createError = customers.Create(new Customer{id = createEntityError})    .TaskName("createError");
            var updateError = customers.Update(new Customer{id = updateEntityError})    .TaskName("updateError");
            var deleteError = customers.Delete(new Customer{id = deleteEntityError})    .TaskName("deleteError");

            var sync = await store.TrySync(); // -------- Sync --------
            
            AreEqual("tasks: 3, failed: 3", sync.ToString());
            AreEqual(@"Sync() failed with task errors. Count: 3
|- createError # EntityErrors ~ count: 1
|   WriteError: Customer 'create-entity-error', simulated write entity error
|- updateError # EntityErrors ~ count: 1
|   WriteError: Customer 'update-entity-error', simulated write entity error
|- deleteError # EntityErrors ~ count: 1
|   WriteError: Customer 'delete-entity-error', simulated write entity error", sync.Message);

            IsFalse(deleteError.Success);
            var deleteErrors = deleteError.Error.entityErrors;
            AreEqual(1,        deleteErrors.Count);
            AreEqual("WriteError: Customer 'delete-entity-error', simulated write entity error", deleteErrors[deleteEntityError].ToString());
            
            IsFalse(createError.Success);
            var createErrors = createError.Error.entityErrors;
            AreEqual(1,        createErrors.Count);
            AreEqual("WriteError: Customer 'create-entity-error', simulated write entity error", createErrors[createEntityError].ToString());
            
            IsFalse(updateError.Success);
            var updateErrors = updateError.Error.entityErrors;
            AreEqual(1,        updateErrors.Count);
            AreEqual("WriteError: Customer 'update-entity-error', simulated write entity error", updateErrors[updateEntityError].ToString());
        }

        private static async Task AssertEntityPatch(PocStore store, TestDatabase testDatabase) {
            testDatabase.ClearErrors();
            TestContainer testCustomers = testDatabase.GetTestContainer("Customer");
            const string patchReadEntityError   = "patch-read-entity-error";
            const string patchWriteEntityError  = "patch-write-entity-error";
            const string patchTaskException     = "patch-task-exception";
            const string patchTaskError         = "patch-task-error";
            const string readTaskError          = "read-task-error";
            const string readTaskException      = "read-task-exception";

            testCustomers.writeEntityErrors.Add(patchWriteEntityError,  () => testCustomers.WriteError(patchWriteEntityError));
            testCustomers.readEntityErrors. Add(patchReadEntityError,   (value) => value.SetError(testCustomers.ReadError(patchReadEntityError)));
            testCustomers.writeTaskErrors.  Add(patchTaskException,     () => throw new SimulationException("simulated write task exception"));
            testCustomers.writeTaskErrors.  Add(patchTaskError,         () => new CommandError {message = "simulated write task error"});
            testCustomers.readTaskErrors.   Add(readTaskError,          () => new CommandError{message = "simulated read task error"});
            testCustomers.readTaskErrors.   Add(readTaskException,      () => throw new SimulationException("simulated read task exception"));

            var customers = store.customers;
            const string unknownId = "unknown-id";
            
            var patchNotFound       = customers.Patch (new Customer{id = unknownId})            .TaskName("patchNotFound");
            
            var patchReadError      = customers.Patch (new Customer{id = patchReadEntityError}) .TaskName("patchReadError");
            
            var patchWriteError     = customers.Patch (new Customer{id = patchWriteEntityError}).TaskName("patchWriteError");
            
            var sync = await store.TrySync(); // -------- Sync --------
            
            AreEqual("tasks: 3, failed: 3", sync.ToString());
            AreEqual(@"Sync() failed with task errors. Count: 3
|- patchNotFound # EntityErrors ~ count: 1
|   PatchError: Customer 'unknown-id', patch target not found
|- patchReadError # EntityErrors ~ count: 1
|   ReadError: Customer 'patch-read-entity-error', simulated read entity error
|- patchWriteError # EntityErrors ~ count: 1
|   WriteError: Customer 'patch-write-entity-error', simulated write entity error", sync.Message);
            
            {
                IsFalse(patchNotFound.Success);
                AreEqual(TaskErrorType.EntityErrors, patchNotFound.Error.type);
                var patchErrors = patchNotFound.Error.entityErrors;
                AreEqual("PatchError: Customer 'unknown-id', patch target not found", patchErrors[unknownId].ToString());
            } {
                IsFalse(patchReadError.Success);
                AreEqual(TaskErrorType.EntityErrors, patchReadError.Error.type);
                var patchErrors = patchReadError.Error.entityErrors;
                AreEqual("ReadError: Customer 'patch-read-entity-error', simulated read entity error", patchErrors[patchReadEntityError].ToString());
            } {
                IsFalse(patchWriteError.Success);
                AreEqual(TaskErrorType.EntityErrors, patchWriteError.Error.type);
                var patchErrors = patchWriteError.Error.entityErrors;
                AreEqual("WriteError: Customer 'patch-write-entity-error', simulated write entity error", patchErrors[patchWriteEntityError].ToString());
            }
            
            // --- test read task error
            {
                var patchTaskReadError = customers.Patch(new Customer {id = readTaskError});

                sync = await store.TrySync(); // -------- Sync --------
                AreEqual("tasks: 1, failed: 1", sync.ToString());

                IsFalse(patchTaskReadError.Success);
                AreEqual("DatabaseError ~ simulated read task error", patchTaskReadError.Error.Message);
            }

            // --- test read task exception
            {
                var patchTaskReadException = customers.Patch(new Customer {id = readTaskException});

                sync = await store.TrySync(); // -------- Sync --------
                AreEqual("tasks: 1, failed: 1", sync.ToString());

                IsFalse(patchTaskReadException.Success);
                AreEqual("UnhandledException ~ SimulationException: simulated read task exception", patchTaskReadException.Error.Message);
            }
            
            // --- test write task error
            {
                var patchTaskWriteError = customers.Patch(new Customer {id = patchTaskError});

                sync = await store.TrySync(); // -------- Sync --------
                AreEqual("tasks: 1, failed: 1", sync.ToString());

                IsFalse(patchTaskWriteError.Success);
                AreEqual("DatabaseError ~ simulated write task error", patchTaskWriteError.Error.Message);
            }
            
            // --- test write task exception
            {
                var patchTaskWriteException = customers.Patch(new Customer {id = patchTaskException});

                sync = await store.TrySync(); // -------- Sync --------
                AreEqual("tasks: 1, failed: 1", sync.ToString());

                IsFalse(patchTaskWriteException.Success);
                AreEqual("UnhandledException ~ SimulationException: simulated write task exception", patchTaskWriteException.Error.Message);
            }
        }

        private static async Task AssertLogChangesPatch(PocStore store, TestDatabase testDatabase) {
            testDatabase.ClearErrors();
            TestContainer testCustomers = testDatabase.GetTestContainer("Customer");
            var customers = store.customers;
            
            // --- prepare precondition for log changes
            const string writeError = "log-patch-entity-write-error";
            const string readError  = "log-patch-entity-read-error";
            var readCustomers = customers.Read();
            var customerWriteError  = readCustomers.Find(writeError);
            var customerReadError   = readCustomers.Find(readError);

            await store.Sync();

            // --- setup simulation errors after preconditions are established
            {
                testCustomers.writeEntityErrors.Add(writeError, () => testCustomers.WriteError(writeError));
                testCustomers.readEntityErrors.Add(readError,   (value) => value.SetError(testCustomers.ReadError(readError)));

                customerWriteError.Result.name  = "<change write 1>";
                customerReadError.Result.name   = "<change read 1>";
                var logChanges = customers.LogSetChanges();

                var sync = await store.TrySync(); // -------- Sync --------
                AreEqual("tasks: 1, failed: 1", sync.ToString());

                IsFalse(logChanges.Success);
                AreEqual(TaskErrorType.EntityErrors, logChanges.Error.type);
                AreEqual(@"EntityErrors ~ count: 2
| ReadError: Customer 'log-patch-entity-read-error', simulated read entity error
| WriteError: Customer 'log-patch-entity-write-error', simulated write entity error", logChanges.Error.Message);
            } {
                testCustomers.readTaskErrors [readError]    = () => throw new SimulationException("simulated read task exception");;
                customerReadError.Result.name   = "<change read 2>";
                var logChanges = customers.LogSetChanges();

                var sync = await store.TrySync(); // -------- Sync --------
                AreEqual("tasks: 1, failed: 1", sync.ToString());

                AreEqual(TaskErrorType.EntityErrors, logChanges.Error.type);
                AreEqual(@"EntityErrors ~ count: 1
| PatchError: Customer 'log-patch-entity-read-error', UnhandledException - SimulationException: simulated read task exception", logChanges.Error.Message);
            } {
                testCustomers.readTaskErrors[readError]    = () => new CommandError{message = "simulated read task error"};
                customerReadError.Result.name   = "<change read 3>";
                var logChanges = customers.LogSetChanges();

                var sync = await store.TrySync(); // -------- Sync --------
                AreEqual("tasks: 1, failed: 1", sync.ToString());

                AreEqual(TaskErrorType.EntityErrors, logChanges.Error.type);
                AreEqual(@"EntityErrors ~ count: 1
| PatchError: Customer 'log-patch-entity-read-error', DatabaseError - simulated read task error", logChanges.Error.Message);
            } {
                testCustomers.readTaskErrors.Remove(readError);
                testCustomers.writeTaskErrors [writeError]    = () => throw new SimulationException("simulated write task exception");
                customerWriteError.Result.name   = "<change write 3>";
                customerReadError.Result.name   = "<change read 1>"; // restore original value
                var logChanges = customers.LogSetChanges();

                var sync = await store.TrySync(); // -------- Sync --------
                AreEqual("tasks: 1, failed: 1", sync.ToString());

                AreEqual(TaskErrorType.EntityErrors, logChanges.Error.type);
                AreEqual(@"EntityErrors ~ count: 1
| PatchError: Customer 'log-patch-entity-write-error', UnhandledException - SimulationException: simulated write task exception", logChanges.Error.Message);
            } {
                testCustomers.writeTaskErrors [writeError]    = () => new CommandError {message = "simulated write task error"} ;
                customerWriteError.Result.name   = "<change write 4>";
                var logChanges = customers.LogSetChanges();

                var sync = await store.TrySync(); // -------- Sync --------
                AreEqual("tasks: 1, failed: 1", sync.ToString());

                AreEqual(TaskErrorType.EntityErrors, logChanges.Error.type);
                AreEqual(@"EntityErrors ~ count: 1
| PatchError: Customer 'log-patch-entity-write-error', DatabaseError - simulated write task error", logChanges.Error.Message);
                
                customerWriteError.Result.name   = "<change write 1>";  // restore original value
            }
        }

        private static async Task AssertLogChangesCreate(PocStore store, TestDatabase testDatabase) {
            testDatabase.ClearErrors();
            TestContainer testProducers = testDatabase.GetTestContainer("Producer");
            var articles = store.articles;

            // --- prepare precondition for log changes
            var readArticles = articles.Read();
            var patchArticle = readArticles.Find("log-create-read-error");
            await store.Sync();

            {
                var createError = "create-error";
                testProducers.writeTaskErrors.Add(createError, () => new CommandError {message = "simulated write task error"});
                patchArticle.Result.producer = new Producer {id = createError};

                var logChanges = store.LogChanges();
                AreEqual("LogTask (patches: 1, creates: 1)", logChanges.ToString());

                var sync = await store.TrySync();

                AreEqual("tasks: 1, failed: 1", sync.ToString());
                AreEqual(TaskErrorType.EntityErrors, logChanges.Error.type);
                AreEqual(@"EntityErrors ~ count: 1
| WriteError: Producer 'create-error', DatabaseError - simulated write task error", logChanges.Error.Message);
            } {
                var createException = "create-exception";
                testProducers.writeTaskErrors.Add(createException, () => throw new SimulationException("simulated write task exception"));
                patchArticle.Result.producer = new Producer {id = createException};

                var logChanges = store.LogChanges();
                AreEqual("LogTask (patches: 1, creates: 1)", logChanges.ToString());

                var sync = await store.TrySync();

                AreEqual("tasks: 1, failed: 1", sync.ToString());
                AreEqual(TaskErrorType.EntityErrors, logChanges.Error.type);
                AreEqual(@"EntityErrors ~ count: 1
| WriteError: Producer 'create-exception', UnhandledException - SimulationException: simulated write task exception", logChanges.Error.Message);
            }

            /*  // not required as TestContainer as database dont mutate
                patchArticle.Result.producer = default; // restore precondition
                store.LogChanges();
                await store.Sync();
            */
        }
        
        private static async Task AssertSyncErrors(PocStore store, TestDatabase testDatabase) {
            testDatabase.ClearErrors();
            // use Echo to simulate error/exception
            const string echoSyncError = "echo-sync-error";
            const string echoSyncException      = "echo-sync-exception";
            testDatabase.syncErrors.Add(echoSyncError,       () => new SyncResponse{error = new SyncError{message = "simulated SyncError"}});
            testDatabase.syncErrors.Add(echoSyncException,   () => throw new SimulationException ("simulated SyncException"));
            
            var helloTask = store.Echo("Hello World");
            AreEqual("EchoTask (message: Hello World)", helloTask.ToString());
            
            await store.Sync(); // -------- Sync --------
            
            AreEqual("Hello World", helloTask.Result);

            // --- Sync error
            {
                var syncError = store.Echo(echoSyncError);
                
                // test throwing exception in case of Sync errors
                try {
                    await store.Sync(); // -------- Sync --------
                    
                    Fail("Sync() intended to fail - code cannot be reached");
                } catch (SyncResultException sre) {
                    AreEqual("simulated SyncError", sre.Message);
                    AreEqual(1, sre.failed.Count);
                    AreEqual("SyncError ~ simulated SyncError", sre.failed[0].Error.ToString());
                }
                AreEqual("SyncError ~ simulated SyncError", syncError.Error.ToString());
            }
            // --- Sync exception
            {
                var syncException = store.Echo(echoSyncException);
                
                var sync = await store.TrySync(); // -------- Sync --------
                
                IsFalse(sync.Success);
                AreEqual("SimulationException: simulated SyncException", sync.Message);
                AreEqual(1, sync.failed.Count);
                AreEqual("SyncError ~ SimulationException: simulated SyncException", sync.failed[0].Error.ToString());
                
                AreEqual("SyncError ~ SimulationException: simulated SyncException", syncException.Error.ToString());
            }
            
        }
    }
}