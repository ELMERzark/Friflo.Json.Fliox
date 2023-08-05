// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER || SQLSERVER

using System;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Protocol.Models;

namespace Friflo.Json.Fliox.Hub.Host.SQL
{
    public abstract partial class SyncDbConnection : ISyncConnection
    {
        private readonly   DbConnection   instance;
        
        public  TaskExecuteError    Error       => throw new InvalidOperationException();
        public  void                Dispose()   => instance.Dispose();
        public  bool                IsOpen      => instance.State == ConnectionState.Open;
        public  abstract void       ClearPool();
        
        protected SyncDbConnection (DbConnection instance) {
            this.instance = instance ?? throw new ArgumentNullException(nameof(instance));
        }
        
        // --------------------------------------- sync / async  --------------------------------------- 
        /// <summary>async version of <see cref="ExecuteNonQuerySync"/></summary>
        public async Task ExecuteNonQueryAsync (string sql, DbParameter parameter = null) {
            using var command = instance.CreateCommand();
            command.CommandText = sql;
            if (parameter != null) {
                command.Parameters.Add(parameter);
            }
            int tryCount = 0;
            while (true) {
                tryCount++;
                try {
                    await command.ExecuteNonQueryAsync().ConfigureAwait(false);
                    return;
                }
                catch (DbException) {
                    if (instance.State != ConnectionState.Open && tryCount == 1) {
                        await instance.OpenAsync().ConfigureAwait(false);
                        continue;
                    }
                    throw;
                }
            }
        }
        
        /// <summary>Counterpart of <see cref="ExecuteSync"/></summary>
        public async Task<SQLResult> ExecuteAsync(string sql) {
            using var command = instance.CreateCommand();
            command.CommandText = sql;
            try {
                using var reader = await ExecuteReaderAsync(sql).ConfigureAwait(false);
                while (await reader.ReadAsync().ConfigureAwait(false)) {
                    var value = reader.GetValue(0);
                    return SQLResult.Success(value); 
                }
                return default;
            }
            catch (DbException e) {
                return SQLResult.CreateError(e);
            }
        }
        
        /// <summary>
        /// Using asynchronous execution for SQL Server is significant slower.<br/>
        /// <see cref="DbCommand.ExecuteReaderAsync()"/> ~7x slower than <see cref="DbCommand.ExecuteReader()"/>.
        /// <summary>Counterpart of <see cref="ExecuteReaderSync"/></summary>
        /// </summary>
        public async Task<DbDataReader> ExecuteReaderAsync(string sql, DbParameter parameter = null) {
            using var command = instance.CreateCommand();
            command.CommandText = sql;
            if (parameter != null) {
                command.Parameters.Add(parameter);
            }
            int tryCount = 0;
            while (true) {
                tryCount++;
                try {
                    return await command.ExecuteReaderAsync().ConfigureAwait(false);
                }
                catch (DbException) {
                    if (instance.State != ConnectionState.Open && tryCount == 1) {
                        await instance.OpenAsync().ConfigureAwait(false);
                        continue;
                    }
                    throw;
                }
            }
        }
    }
}

#endif