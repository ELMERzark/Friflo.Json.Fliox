// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER || SQLITE

using System;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.DB.Cluster;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.SQL;
using Friflo.Json.Fliox.Hub.Host.Utils;
using SQLitePCL;

namespace Friflo.Json.Fliox.Hub.SQLite
{
    public sealed class SQLiteDatabase : EntityDatabase, ISQLDatabase
    {
        public              bool        Pretty      { get; init; } = false;
        /// <summary>Set the execution mode of SQLite database commands.<br/>
        /// If true database commands are executed synchronously otherwise asynchronously.<br/>
        /// <br/>
        /// The use cases of sync vs async execution are explained in detail at:<br/>
        /// <a href="https://devblogs.microsoft.com/pfxteam/should-i-expose-asynchronous-wrappers-for-synchronous-methods/">
        /// Should I expose asynchronous wrappers for synchronous methods? - .NET Parallel Programming</a>
        /// </summary>
        public              bool        Synchronous             { get; init; } = false;
        public              bool        AutoCreateDatabase      { get; init; } = true;
        public              bool        AutoCreateTables        { get; init; } = true;
        public              bool        AutoAddVirtualColumns   { get; init; } = true;
        
        internal readonly   sqlite3     sqliteDB;

        
        public   override   string      StorageType => "SQLite " + SQLiteUtils.GetVersion(sqliteDB);
        
        /// <summary>
        /// Open or create a database with the given <paramref name="path"/>.<br/>
        /// Create an Im-Memory <paramref name="path"/> is <c>":memory:"</c><br/>
        /// See: <a href="https://www.sqlite.org/inmemorydb.html">SQLite - In-Memory Databases</a>
        /// </summary>
        /// <returns></returns>
        public SQLiteDatabase(string dbName, string path, DatabaseSchema schema, DatabaseService service = null)
            : base(dbName, AssertSchema<SQLiteDatabase>(schema), service)
        {
            var rc = raw.sqlite3_open(path, out sqliteDB);
            if (rc != raw.SQLITE_OK) throw new InvalidOperationException($"sqlite3_open failed. error: {rc}");
        }
        
        public override EntityContainer CreateContainer(in ShortString name, EntityDatabase database) {
            return new SQLiteContainer(name.AsString(), this, Pretty);
        }
        
        public override ISyncConnection GetConnectionSync() {
            return new SyncConnection(sqliteDB);
        }
        
        public override void ReturnConnection(ISyncConnection connection) { }

        static SQLiteDatabase() {
            raw.SetProvider(new SQLite3Provider_e_sqlite3());
        }
        
        public override Task<TransResult> Transaction(SyncContext syncContext, TransCommand command) {
            var result = TransactionSync(syncContext, command);
            return Task.FromResult(result);
        }
        
        private static TransResult TransactionSync(SyncContext syncContext, TransCommand command)
        {
            var syncConnection = (SyncConnection)syncContext.GetConnectionSync();
            switch (command) {
                case TransCommand.Begin: {
                    syncConnection.BeginTransaction(out var error);
                    if (error != null) {
                        return new TransResult(error.message);
                    }
                    return new TransResult(command);;
                }
                case TransCommand.Commit: {
                    if (!syncConnection.EndTransaction("COMMIT", out var error)) {
                        return new TransResult(error.message);
                    }
                    return new TransResult(command);
                }
                case TransCommand.Rollback: {
                    if (!syncConnection.EndTransaction("ROLLBACK", out var error)) {
                        return new TransResult(error.message);
                    }
                    return new TransResult(command);
                }
                default:
                    return new TransResult($"invalid transaction command {command}");
            }
        }
    }

    
    internal sealed class SQLiteQueryEnumerator : QueryEnumerator
    {
        internal readonly   sqlite3_stmt    stmt;
        internal readonly   string          sql;
        
        public   override   JsonKey         Current     => throw new NotImplementedException("not applicable");
        public   override   bool            MoveNext()  => throw new NotImplementedException("not applicable");
        
        internal SQLiteQueryEnumerator(sqlite3_stmt stmt, string sql) {
            this.stmt   = stmt;
            this.sql    = sql;
        }
        
        protected override void DisposeEnumerator() {
            raw.sqlite3_finalize(stmt);
        }
    }
}

namespace System.Runtime.CompilerServices
{
    // This is needed to enable following features in .NET framework and .NET core <= 3.1 projects:
    // - init only setter properties. See [Init only setters - C# 9.0 draft specifications | Microsoft Learn] https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-9.0/init
    // - record types
    internal static class IsExternalInit { }
}

#endif
