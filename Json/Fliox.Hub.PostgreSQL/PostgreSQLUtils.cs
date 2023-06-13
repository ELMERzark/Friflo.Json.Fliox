// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER || POSTGRESQL

using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host.SQL;
using Npgsql;
using static Friflo.Json.Fliox.Hub.Host.SQL.SQLName;

// ReSharper disable UseAwaitUsing
namespace Friflo.Json.Fliox.Hub.PostgreSQL
{
    public static class PostgreSQLUtils
    {
        internal static NpgsqlCommand Command (string sql, SyncConnection connection) {
            return new NpgsqlCommand(sql, connection.instance);
        }
        
        internal static async Task<SQLResult> Execute(SyncConnection connection, string sql) {
            try {
                using var command = new NpgsqlCommand(sql, connection.instance);
                using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
                while (await reader.ReadAsync().ConfigureAwait(false)) {
                    var value = reader.GetValue(0);
                    return new SQLResult(value); 
                }
                return default;
            }
            catch (PostgresException e) {
                return new SQLResult(e.MessageText);
            }
        }
        
        internal static async Task AddVirtualColumn(SyncConnection connection, string table, ColumnInfo column) {
            var type = ConvertContext.GetSqlType(column.typeId);
            var path = ConvertContext.ConvertPath(DATA, column.name, 0);
            var sql =
$@"ALTER TABLE {table}
ADD COLUMN IF NOT EXISTS ""{column.name}"" {type} NULL
GENERATED ALWAYS AS (({path})::{type}) STORED;";
            await Execute(connection, sql).ConfigureAwait(false);
        }
        
        internal static async Task CreateDatabaseIfNotExistsAsync(string connectionString) {
            var dbmsConnectionString = GetDbmsConnectionString(connectionString, out var database);
            using var connection = new NpgsqlConnection(dbmsConnectionString);
            await connection.OpenAsync().ConfigureAwait(false);
            
            var sql = $"CREATE DATABASE {database}";
            using var cmd = new NpgsqlCommand(sql, connection);
            await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
        }
        
        private static string GetDbmsConnectionString(string connectionString, out string database) {
            var builder  = new NpgsqlConnectionStringBuilder(connectionString);
            database = builder.Database;
            builder.Remove("Database");
            return builder.ToString();
        }
    }
}

#endif