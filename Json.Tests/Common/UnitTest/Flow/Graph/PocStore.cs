﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Friflo.Json.Flow.Database;
using Friflo.Json.Flow.Graph;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Sync;
using static NUnit.Framework.Assert;
using static Friflo.Json.Tests.Common.Utils.AssertUtils;

namespace Friflo.Json.Tests.Common.UnitTest.Flow.Graph
{
    // ------------------------------ models ------------------------------
    public class Order : Entity {
        public Ref<Customer>        customer;
        public List<OrderItem>      items = new List<OrderItem>();
    }

    public class OrderItem {
        public Ref<Article>         article;
        public int                  amount;
        public string               name;
    }

    public class Article : Entity
    {
        public string               name;
        public Ref<Producer>        producer;
    }

    public class Customer : Entity {
        public string               name;
    }
    
    public class Producer : Entity {
        public string               name;
        [Fri.Property(Name = "employees")]
        public List<Ref<Employee>>  employeeList;
    }
    
    public class Employee : Entity {
        public string               firstName;
        public string               lastName;
    }

    // --- store containers
    public class PocStore : EntityStore
    {
        public PocStore(EntityDatabase database, string clientId) : base (database, TestGlobals.typeStore, clientId) {
            orders      = new EntitySet<Order>       (this);
            customers   = new EntitySet<Customer>    (this);
            articles    = new EntitySet<Article>     (this);
            producers   = new EntitySet<Producer>    (this);
            employees   = new EntitySet<Employee>    (this);
        }

        public readonly EntitySet<Order>      orders;
        public readonly EntitySet<Customer>   customers;
        public readonly EntitySet<Article>    articles;
        public readonly EntitySet<Producer>   producers;
        public readonly EntitySet<Employee>   employees;
    }
    
    public static class TestGlobals {
        public static TypeStore typeStore;
        
        public static void Init() {
            SyncTypeStore.Init();
            typeStore = new TypeStore();
            // by new PocStore() all TypeMappers for model classes are created before leak tracking of LeakTestsFixture starts. 
            using (var _= new PocStore(new MemoryDatabase(), "TestGlobals")) { }
        }
        
        public static void Dispose() {
            typeStore.Dispose();
            typeStore = null;
            SyncTypeStore.Dispose();
        }
    }
        
    // --------------------------------------------------------------------
    public static class TestRelationPoC
    {
        public static async Task CreateStore(PocStore store) {
            
            AreSimilar("entities: 0",    store);    // initial state, empty store
            var orders      = store.orders;
            var articles    = store.articles;
            var producers   = store.producers;
            var employees   = store.employees;
            var customers   = store.customers;

            var samsung         = new Producer { id = "producer-samsung", name = "Samsung"};
            var galaxy          = new Article  { id = "article-galaxy",   name = "Galaxy S10", producer = samsung};
            var createGalaxy    = articles.Create(galaxy);
            AreSimilar("entities: 1, tasks: 1",                         store);
            AreSimilar("Article:  1, tasks: 1 >> create #1",            articles);

            var logStore1 = store.LogChanges();  AssertLog(logStore1, 0, 1);
            
            AreSimilar("entities: 2, tasks: 2",                         store);
            AreSimilar("Producer: 1, tasks: 1 >> create #1",            producers); // created samsung implicit

            var steveJobs       = new Employee { id = "apple-0001", firstName = "Steve", lastName = "Jobs"};
            var appleEmployees  = new List<Ref<Employee>>{ steveJobs };
            var apple           = new Producer { id = "producer-apple", name = "Apple", employeeList = appleEmployees};
            var ipad            = new Article  { id = "article-ipad",   name = "iPad Pro", producer = apple};
            var createIPad      = articles.Create(ipad);
            AreSimilar("Article:  2, tasks: 1 >> create #2",            articles);
            
            var deleteIPhone    = articles.Delete("article-iphone"); // delete if exist in database
            AreSimilar("Article:  2, tasks: 2 >> create #2, delete #1", articles);

            var logStore2 = store.LogChanges();  AssertLog(logStore2, 0, 2);
            AreSimilar("entities: 5, tasks: 4",                         store);
            AreSimilar("Article:  2, tasks: 2 >> create #2, delete #1", articles);
            AreSimilar("Employee: 1, tasks: 1 >> create #1",            employees); // created steveJobs implicit
            AreSimilar("Producer: 2, tasks: 1 >> create #2",            producers); // created apple implicit

            await store.Sync(); // -------- Sync --------
            AreSimilar("entities: 5",                                   store);   // tasks executed and cleared
            
            IsTrue(deleteIPhone.Success);
            IsTrue(logStore1.Success);
            IsTrue(logStore2.Success);
            IsTrue(createGalaxy.Success);
            IsTrue(createIPad.Success);
            IsNull(createIPad.Error); // error is null if successful
            
            var canon           = new Producer { id = "producer-canon", name = "Canon"};
            var createCanon     = producers.Create(canon);
            var order           = new Order { id = "order-1" };
            var cameraCreate    = new Article { id = "article-1", name = "Camera", producer = canon };
            var notebook        = new Article { id = "article-3", name = "Notebook", producer = samsung };
            var createCam1      = articles.Create(cameraCreate);
                                  articles.Create(notebook);
            var createCam2      = articles.Create(cameraCreate);   // Create new CreateTask for same entity
            AreNotSame(createCam1, createCam2);               
            AreEqual("CreateTask<Article> (#ids: 1)", createCam1.ToString());

            var newBulkArticles = new List<Article>();
            for (int n = 0; n < 2; n++) {
                var id = $"bulk-article-{n:D4}";
                var newArticle = new Article { id = id, name = id };
                newBulkArticles.Add(newArticle);
            }
            articles.CreateRange(newBulkArticles);

            var readArticles    = articles.Read();
            var cameraUnknown   = readArticles.Find("article-missing");
            var camera          = readArticles.Find("article-1");
            
            var camForDelete    = new Article { id = "article-delete", name = "Camera-Delete" };
            articles.Create(camForDelete);
            // StoreInfo is accessible via property an ToString()
            AreEqual(11, store.StoreInfo.peers);
            AreEqual(3,  store.StoreInfo.tasks); 
            AreSimilar("entities: 11, tasks: 3",                        store);
            AreSimilar("Article:   7, tasks: 2 >> create #5, reads: 1", articles);
            AreSimilar("Producer:  3, tasks: 1 >> create #1",           producers);
            AreSimilar("Employee:  1",                                  employees);
            
            await store.Sync(); // -------- Sync --------
            AreSimilar("entities: 11",                                  store); // tasks cleared
            
            IsTrue(createCam1.Success);
            IsTrue(createCanon.Success);
            

            articles.DeleteRange(newBulkArticles);
            AreSimilar("entities: 11, tasks: 1",                        store);
            AreSimilar("Article:   7, tasks: 1 >> delete #2",           articles);
            
            await store.Sync(); // -------- Sync --------
            AreSimilar("entities:  9",                                  store); // tasks cleared
            AreSimilar("Article:   5",                                  articles);

            cameraCreate.name = "Changed name";
            var logEntity = articles.LogEntityChanges(cameraCreate);    AssertLog(logEntity, 1, 0);
            var logSet =    articles.LogSetChanges();                   AssertLog(logSet,    1, 0);
            AreEqual("LogTask (patches: 1, creates: 0)", logSet.ToString());

            var logStore3 = store.LogChanges();  AssertLog(logStore3, 1, 0);
            var logStore4 = store.LogChanges();  AssertLog(logStore4, 1, 0);

            var deleteCamera = articles.Delete(camForDelete.id);
            
            await store.Sync(); // -------- Sync --------
            
            IsTrue(logEntity.Success);
            IsTrue(logSet.Success);
            IsTrue(logStore3.Success);
            IsTrue(logStore4.Success);
            IsTrue(deleteCamera.Success);
            AreSimilar("entities: 8",                           store);       // tasks executed and cleared

            AreSimilar("Article:  4",                           articles);
            var readArticles2   = articles.Read();
            var cameraNotSynced = readArticles2.Find("article-1");
            AreSimilar("entities: 8, tasks: 1",                 store);
            AreSimilar("Article:  4, tasks: 1 >> reads: 1",     articles);
            
            var e = Throws<TaskNotSyncedException>(() => { var res = cameraNotSynced.Result; });
            AreSimilar("Find.Result requires Sync(). Find<Article> (id: 'article-1')", e.Message);
            
            IsNull(cameraUnknown.Result);
            AreSame(camera.Result, cameraCreate);
            
            var customer    = new Customer { id = "customer-1", name = "Smith Ltd." };
            // customers.Create(customer);    // redundant - implicit tracked by order
            
            var smartphone  = new Article { id = "article-2", name = "Smartphone" };
            // articles.Create(smartphone);   // redundant - implicit tracked by order
            
            var item1 = new OrderItem { article = camera.Result, amount = 1, name = "Camera" };
            var item2 = new OrderItem { article = smartphone,    amount = 2, name = smartphone.name };
            var item3 = new OrderItem { article = camera.Result, amount = 3, name = "Camera" };
            order.items.AddRange(new [] { item1, item2, item3 });
            order.customer = customer;
            
            AreSimilar("entities:  8, tasks: 1",                        store);
            
            AreSimilar("Order:     0",                                  orders);
            orders.Create(order);
            AreSimilar("entities:  9, tasks: 2",                        store);
            AreSimilar("Order:     1, tasks: 1 >> create #1",           orders);     // created order
            
            AreSimilar("Article:   4, tasks: 1 >> reads: 1", articles);
            AreSimilar("Customer:  0",                                  customers);
            var logSet2 = orders.LogSetChanges();   AssertLog(logSet2, 0, 2);
            AreSimilar("entities: 11, tasks: 4",                        store);
            AreSimilar("Article:   5, tasks: 2 >> create #1, reads: 1", articles);   // created smartphone (implicit)
            AreSimilar("Customer:  1, tasks: 1 >> create #1",           customers);  // created customer (implicit)
            
            AreSimilar("entities: 11, tasks: 4",                        store);
            var logStore5 = store.LogChanges();     AssertLog(logStore5, 0, 0);
            var logStore6 = store.LogChanges();     AssertLog(logStore6, 0, 0);
            AreSimilar("entities: 11, tasks: 4",                        store);      // no new changes

            await store.Sync(); // -------- Sync --------
            
            IsTrue(logSet2.Success);
            IsTrue(logStore5.Success);
            IsTrue(logStore6.Success);
            AreSimilar("entities: 11",                                  store);      // tasks executed and cleared
            
            
            notebook.name = "Galaxy Book";
            var patchNotebook = articles.Patch(notebook);
            patchNotebook.Member(a => a.name);
            var patchArticles = articles.PatchRange(new Article[] {});
            patchArticles.Add(notebook);
            var producerPath = new MemberPath<Article>(a => a.producer);
            patchArticles.MemberPath(producerPath);
            
            AreEqual(".producer",                                       producerPath.ToString());
            AreEqual("PatchTask<Article> #ids: 1, members: [.name]",    patchNotebook.ToString());
            AreEqual("PatchTask<Article> #ids: 1, members: [.producer]",patchArticles.ToString());
            
            AreSimilar("Article:   5, tasks: 1 >> patch #2",            articles);
            AreSimilar("entities: 11, tasks: 1",                        store);      // tasks executed and cleared
            
            await store.Sync(); // -------- Sync --------
            AreSimilar("entities: 11",                                  store);      // tasks executed and cleared
            
            IsTrue(patchNotebook.Success);
            IsTrue(patchArticles.Success);

            customers.Create(new Customer{id = "log-patch-entity-read-error",   name = "used for successful read"});
            customers.Create(new Customer{id = "log-patch-entity-write-error",  name = "used for successful read"});
            
            customers.Create(new Customer{id = "patch-task-error",              name = "used for successful patch-read"});
            customers.Create(new Customer{id = "patch-task-exception",          name = "used for successful patch-read"});
            customers.Create(new Customer{id = "patch-write-entity-error",      name = "used for successful patch-read"});

            articles.Create (new Article {id = "log-create-read-error",         name = "used for successful read"});
            await store.Sync();
            
            var errorRefTask = new Customer{ id = "read-task-error" };
            var order2 = new Order{id = "order-2", customer = errorRefTask};
            orders.Create(order2);
            store.Echo(EndCreate);
            
            await store.Sync(); // -------- Sync --------
        }

        internal const string EndCreate = "EndCreate";

        static void AssertLog(LogTask logTask, int patches, int creates) {
            var patchCount  = logTask.GetPatchCount();
            var createCount = logTask.GetCreateCount();
            
            if (patchCount == patches && createCount == creates)
                return;
            Fail($"Expect:  patches: {patches}, creates: {creates}\nbut was: patches: {patchCount}, creates: {createCount}");
        }
    }
}