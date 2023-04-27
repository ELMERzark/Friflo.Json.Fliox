﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER || SQLITE

using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Utils;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using SQLitePCL;

namespace Friflo.Json.Fliox.Hub.SQLite
{
    public sealed class SQLiteContainer : EntityContainer
    {
        private  readonly   sqlite3     sqliteDB;
        private             bool        tableExists;
        public   override   bool        Pretty      { get; }
        
        internal SQLiteContainer(string name, SQLiteDatabase database, bool pretty)
            : base(name, database)
        {
            sqliteDB    = database.sqliteDB;
            Pretty      = pretty;
        }

        private bool EnsureContainerExists(out TaskExecuteError error) {
            if (tableExists) {
                error = null;
                return true;
            }
            var sql = $"CREATE TABLE IF NOT EXISTS {name} (id TEXT PRIMARY KEY, data TEXT NOT NULL)";
            var success = SQLiteUtils.Execute(sqliteDB, sql, out error);
            if (success) {
                tableExists = true;
            }
            return success;
        }
        
        public override Task<CreateEntitiesResult> CreateEntitiesAsync(CreateEntities command, SyncContext syncContext) {
            var result = CreateEntities(command, syncContext);
            return Task.FromResult(result);
        }
        
        public override CreateEntitiesResult CreateEntities(CreateEntities command, SyncContext syncContext) {
            if (!EnsureContainerExists(out var error)) {
                return new CreateEntitiesResult { Error = error };
            }
            if (!SQLiteUtils.Exec(sqliteDB, "BEGIN TRANSACTION", out  error)) {
                return new CreateEntitiesResult { Error = error };
            }
            var sql = $@"INSERT INTO {name} VALUES(?,?)";
            if (!SQLiteUtils.Prepare(sqliteDB, sql, out var stmt, out error)) {
                return new CreateEntitiesResult { Error = error };
            }
            if (!SQLiteUtils.AppendValues(stmt, command.entities, out error)) {
                return new CreateEntitiesResult { Error = error };
            }
            raw.sqlite3_finalize(stmt);
            if (!SQLiteUtils.Exec(sqliteDB, "END TRANSACTION", out error)) {
                return new CreateEntitiesResult { Error = error };
            }
            return new CreateEntitiesResult();
        }
        
        public override Task<UpsertEntitiesResult> UpsertEntitiesAsync(UpsertEntities command, SyncContext syncContext) {
            var result = UpsertEntities(command, syncContext);
            return Task.FromResult(result);
        }

        public override UpsertEntitiesResult UpsertEntities(UpsertEntities command, SyncContext syncContext) {
            if (!EnsureContainerExists(out var error)) {
                return new UpsertEntitiesResult { Error = error };
            }
            if (!SQLiteUtils.Exec(sqliteDB, "BEGIN TRANSACTION", out error)) {
                return new UpsertEntitiesResult { Error = error };
            }
            var sql = $@"INSERT INTO {name} VALUES(?,?) ON CONFLICT(id) DO UPDATE SET data=excluded.data";
            if (!SQLiteUtils.Prepare(sqliteDB, sql, out var stmt, out error)) {
                return new UpsertEntitiesResult { Error = error };
            }
            if (!SQLiteUtils.AppendValues(stmt, command.entities, out error)) {
                return new UpsertEntitiesResult { Error = error };
            }
            raw.sqlite3_finalize(stmt);
            if (!SQLiteUtils.Exec(sqliteDB, "END TRANSACTION", out error)) {
                return new UpsertEntitiesResult { Error = error };
            }
            return new UpsertEntitiesResult();
        }
        
        public override Task<ReadEntitiesResult> ReadEntitiesAsync(ReadEntities command, SyncContext syncContext) {
            var result = ReadEntities(command, syncContext);
            return Task.FromResult(result);
        }

        public override ReadEntitiesResult ReadEntities(ReadEntities command, SyncContext syncContext) {
            if (!EnsureContainerExists(out var error)) {
                return new ReadEntitiesResult { Error = error };
            }
            var sql = $"SELECT id, data FROM {name} WHERE id in (?)";
            if (!SQLiteUtils.Prepare(sqliteDB, sql, out var stmt, out error)) {
                return new ReadEntitiesResult { Error = error };
            }
            var values = new List<EntityValue>();
            if (!SQLiteUtils.ReadById(stmt, command.ids, values, syncContext.MemoryBuffer, out error)) {
                return new ReadEntitiesResult { Error = error };
            }
            return new ReadEntitiesResult { entities = values.ToArray() };
        }
        
        public override Task<QueryEntitiesResult> QueryEntitiesAsync(QueryEntities command, SyncContext syncContext) {
            var result = QueryEntities(command, syncContext);
            return Task.FromResult(result);
        }
        
        public override QueryEntitiesResult QueryEntities(QueryEntities command, SyncContext syncContext) {
            if (!EnsureContainerExists(out var error)) {
                return new QueryEntitiesResult { Error = error };
            }
            sqlite3_stmt    stmt;
            QueryEnumerator enumerator = null;
            var             maxCount   = command.maxCount;
            if (command.cursor != null) {
                if (!FindCursor(command.cursor, syncContext, out enumerator, out error)) {
                    return new QueryEntitiesResult { Error = error };
                }
                stmt = ((SQLiteQueryEnumerator)enumerator).stmt;
            } else {
                var filter  = command.GetFilter();
                var where   = filter.IsTrue ? "" : $" WHERE {filter.SQLiteFilter()}";
                var limit   = command.limit == null ? "" : $" LIMIT {command.limit}";
                var sql     = $"SELECT id, data FROM {name}{where}{limit}";
                if (!SQLiteUtils.Prepare(sqliteDB, sql, out stmt, out error)) {
                    return new QueryEntitiesResult { Error = error };
                }
            }
            var values = new List<EntityValue>();
            if (!SQLiteUtils.ReadValues(stmt, maxCount, values, syncContext.MemoryBuffer, out error)) {
                return new QueryEntitiesResult { Error = error };
            }
            var result = new QueryEntitiesResult { entities = values.ToArray() };
            if (maxCount != null) {
                if (values.Count == maxCount) {
                    enumerator ??= new SQLiteQueryEnumerator(stmt);
                    result.cursor = StoreCursor(enumerator, syncContext.User.userId);
                } else {
                    RemoveCursor(enumerator);
                }
            }
            return result;
        }
        
        public override Task<AggregateEntitiesResult> AggregateEntitiesAsync (AggregateEntities command, SyncContext syncContext) {
            var result = AggregateEntities(command, syncContext);
            return Task.FromResult(result);
        }
        
        private AggregateEntitiesResult AggregateEntities (AggregateEntities command, SyncContext syncContext) {
            if (!EnsureContainerExists(out var error)) {
                return new AggregateEntitiesResult { Error = error };
            }
            if (command.type == AggregateType.count) {
                var filter  = command.GetFilter();
                var where   = filter.IsTrue ? "" : $" WHERE {filter.SQLiteFilter()}";
                var sql     = $"SELECT COUNT(*) from {name}{where}";
                if (!SQLiteUtils.Prepare(sqliteDB, sql, out var stmt, out error)) {
                    return new AggregateEntitiesResult { Error = error };
                }
                var rc      = raw.sqlite3_step(stmt);
                if (rc != raw.SQLITE_ROW) {
                    var msg = $"step failed. sql: {sql}. error: {rc}";
                    return new AggregateEntitiesResult { Error = new TaskExecuteError(TaskErrorType.DatabaseError, msg) };
                }
                var count   = raw.sqlite3_column_int64(stmt, 0);
                raw.sqlite3_finalize(stmt);
                return new AggregateEntitiesResult { value = count };
            }
            var typeErr = $"command.type: {command.type}";
            return new AggregateEntitiesResult { Error = new TaskExecuteError(TaskErrorType.NotImplemented, typeErr) };
        }
        
        public override Task<DeleteEntitiesResult> DeleteEntitiesAsync(DeleteEntities command, SyncContext syncContext) {
            var result = DeleteEntities(command, syncContext);
            return Task.FromResult(result);
        }
        
        public override DeleteEntitiesResult DeleteEntities(DeleteEntities command, SyncContext syncContext) {
            if (!EnsureContainerExists(out var error)) {
                return new DeleteEntitiesResult { Error = error };
            }
            if (command.all == true) {
                var sql = $"DELETE from {name}";
                if (!SQLiteUtils.Exec(sqliteDB, sql, out error)) {
                    return new DeleteEntitiesResult { Error = error };    
                }
                return new DeleteEntitiesResult();
            } else {
                if (!SQLiteUtils.Exec(sqliteDB, "BEGIN TRANSACTION", out error)) {
                    return new DeleteEntitiesResult { Error = error };
                }
                var sql = $"DELETE from {name} WHERE id in (?)";
                if (!SQLiteUtils.Prepare(sqliteDB, sql, out var stmt, out error)) {
                    return new DeleteEntitiesResult { Error = error };
                }
                if (!SQLiteUtils.AppendKeys(stmt, command.ids, out error)) {
                    return new DeleteEntitiesResult { Error = error };
                }
                raw.sqlite3_finalize(stmt);
                if (!SQLiteUtils.Exec(sqliteDB, "END TRANSACTION", out error)) {
                    return new DeleteEntitiesResult { Error = error };
                }
                return new DeleteEntitiesResult();
            }
        }
    }
}

#endif