using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.DB.Cluster;
using Friflo.Json.Tests.Provider.Client;
using NUnit.Framework;
using static NUnit.Framework.Assert;
using static Friflo.Json.Tests.Provider.Env;

// ReSharper disable CompareOfFloatsByEqualityOperator
namespace Friflo.Json.Tests.Provider.Test
{
    // ReSharper disable once InconsistentNaming
    public static class Test_5_Transaction
    {
        
        private static bool SupportTransaction(string db) => IsSQLite(db) || IsMySQL(db) || IsMariaDB(db) || IsPostgres(db) || IsSQLServer(db);

        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestTransaction_Commit_Implicit(string db) {
            var client  = await GetClient(db);
            client.testMutate.DeleteAll();
            await client.SyncTasks();
            
            var begin = client.std.TransactionBegin();
            client.testMutate.Create(new TestMutate { id = "op-1", val1 = 1, val2 = 1 });
            client.testMutate.Create(new TestMutate { id = "op-2", val1 = 2, val2 = 2 });
            await client.SyncTasks();
            NotNull(begin.Result);
            
            var count = client.testMutate.CountAll();
            await client.SyncTasks();
            AreEqual(2, count.Result);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestTransaction_Commit_Explicit(string db) {
            var client  = await GetClient(db);
            client.testMutate.DeleteAll();
            await client.SyncTasks();
            
            var begin = client.std.TransactionBegin();
            client.testMutate.Create(new TestMutate { id = "op-1", val1 = 1, val2 = 1 });
            client.testMutate.Create(new TestMutate { id = "op-2", val1 = 2, val2 = 2 });
            var end = client.std.TransactionEnd(new TransactionEnd { command = TransactionCommand.Commit} );
            await client.SyncTasks();
            NotNull(begin.Result);
            NotNull(end.Result);
            
            var count = client.testMutate.CountAll();
            await client.SyncTasks();
            AreEqual(2, count.Result);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestTransaction_Rollback(string db) {
            var client  = await GetClient(db);
            client.testMutate.DeleteAll();
            await client.SyncTasks();
            
            var begin = client.std.TransactionBegin();
            client.testMutate.Create(new TestMutate { id = "op-1", val1 = 1, val2 = 1 });
            client.testMutate.Create(new TestMutate { id = "op-2", val1 = 2, val2 = 2 });
            var end = client.std.TransactionEnd(new TransactionEnd { command = TransactionCommand.Rollback } );
            await client.SyncTasks();
            NotNull(begin.Result);
            NotNull(end.Result);
            
            var count = client.testMutate.CountAll();
            await client.SyncTasks();
            if (SupportTransaction(db)) {
                AreEqual(0, count.Result);
                return;
            }
            AreEqual(2, count.Result);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestTransaction_Commit_Error(string db) {
            var client  = await GetClient(db);
            
            var end = client.std.TransactionEnd();
            await client.TrySyncTasks();
            
            AreEqual("CommandError ~ Missing begin transaction", end.Error.Message);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestTransaction_Rollback_Error(string db) {
            var client  = await GetClient(db);
            
            var end = client.std.TransactionEnd(new TransactionEnd { command = TransactionCommand.Rollback });
            await client.TrySyncTasks();
            
            AreEqual("CommandError ~ Missing begin transaction", end.Error.Message);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestTransaction_Commit_Nested_Error(string db) {
            var client  = await GetClient(db);
            
            var begin1 = client.std.TransactionBegin();
            var begin2 = client.std.TransactionBegin();
            await client.TrySyncTasks();
            
            IsTrue(begin1.Success);
            AreEqual("CommandError ~ Transaction already started", begin2.Error.Message);
        }
    }
}
