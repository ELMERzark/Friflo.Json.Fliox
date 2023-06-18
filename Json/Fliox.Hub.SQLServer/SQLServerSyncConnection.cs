// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER || SQLSERVER

using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host.SQL;
using Friflo.Json.Fliox.Hub.Protocol.Models;

namespace Friflo.Json.Fliox.Hub.SQLServer
{
    internal sealed class SyncConnection : ISyncConnection
    {
        internal readonly    SqlConnection         instance;
        
        public  TaskExecuteError    Error       => throw new InvalidOperationException();
        public  void                Dispose()   => instance.Dispose();
        public  bool                IsOpen      => instance.State == ConnectionState.Open;
        
        public SyncConnection (SqlConnection instance) {
            this.instance = instance ?? throw new ArgumentNullException(nameof(instance));
        }
        
        internal async Task ExecuteNonQueryAsync (string sql, SqlParameter parameter = null) {
            using var cmd = new SqlCommand(sql, instance);
            if (parameter != null) {
                cmd.Parameters.Add(parameter);
            }
            int tryCount = 0;
            while (true) {
                tryCount++;
                try {
                    await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                    return;
                }
                catch (SqlException) {
                    if (instance.State != ConnectionState.Open && tryCount == 1) {
                        await instance.OpenAsync().ConfigureAwait(false);
                        continue;
                    }
                    throw;
                }
            }
        }
        
        internal async Task<SqlDataReader> ExecuteReaderAsync(string sql, SqlParameter parameter = null) {
            using var command = new SqlCommand(sql, instance);
            if (parameter != null) {
                command.Parameters.Add(parameter);
            }
            int tryCount = 0;
            while (true) {
                tryCount++;
                try {
                    return await command.ExecuteReaderAsync().ConfigureAwait(false);
                }
                catch (SqlException) {
                    if (instance.State != ConnectionState.Open && tryCount == 1) {
                        await instance.OpenAsync().ConfigureAwait(false);
                        continue;
                    }
                    throw;
                }
            }
        }
        
        internal async Task<SqlDataReader> ExecuteReaderSync(string sql) {
            using var command = new SqlCommand(sql, instance);
            int tryCount = 0;
            while (true) {
                tryCount++;
                try {
                    // ReSharper disable once MethodHasAsyncOverload
                    return command.ExecuteReader();
                }
                catch (SqlException) {
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