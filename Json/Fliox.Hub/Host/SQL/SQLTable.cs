// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Data.Common;
using System.Text;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host.Utils;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;

namespace Friflo.Json.Fliox.Hub.Host.SQL
{
    public interface ISQLDatabase
    {
        Task CreateFunctions                (ISyncConnection connection);
    }
    
    public interface ISQLTable
    {
        Task<SQLResult> CreateTable         (ISyncConnection connection);
        Task<SQLResult> AddVirtualColumns   (ISyncConnection connection);
        Task<SQLResult> AddColumns          (ISyncConnection connection);
    }
    
    public static class SQLTable
    {
        public static void AppendColumnNames(StringBuilder sb, TableInfo tableInfo) {
            var isFirst = true;
            var columns = tableInfo.columns;
            foreach (var column in columns) {
                if (isFirst) isFirst = false; else sb.Append(',');
                sb.Append(tableInfo.colStart);
                sb.Append(column.name);
                sb.Append(tableInfo.colEnd);
            }
        }
        
        public static void AppendValuesSQL(
            StringBuilder       sb,
            List<JsonEntity>    entities,
            SQLEscape           escape,
            TableInfo           tableInfo,
            SyncContext         syncContext)
        {
            using var pooled = syncContext.pool.Json2SQL.Get();
            sb.Append(" (");
            AppendColumnNames(sb, tableInfo);
            sb.Append(")\nVALUES\n");
            var writer = new Json2SQLWriter (pooled.instance, sb, escape);
            pooled.instance.AppendColumnValues(writer, entities, tableInfo);
        }
        
        public static async Task<ReadEntitiesResult> ReadEntitiesAsync(
            DbDataReader    reader,
            ReadEntities    query,
            TableInfo       tableInfo,
            SyncContext     syncContext)
        {
            using var pooled = syncContext.pool.SQL2Json.Get();
            var mapper   = new SQL2JsonMapper(reader);
            var buffer   = syncContext.MemoryBuffer;
            var entities = await mapper.ReadEntitiesAsync(pooled.instance, tableInfo, buffer).ConfigureAwait(false);
            var array    = KeyValueUtils.EntityListToArray(entities, query.ids);
            return new ReadEntitiesResult { entities = array };
        }
        
        public static async Task<List<EntityValue>> QueryEntitiesAsync(
            DbDataReader    reader,
            TableInfo       tableInfo,
            SyncContext     syncContext)
        {
            using var pooled = syncContext.pool.SQL2Json.Get();
            var mapper   = new SQL2JsonMapper(reader);
            var buffer   = syncContext.MemoryBuffer;
            return await mapper.ReadEntitiesAsync(pooled.instance, tableInfo, buffer).ConfigureAwait(false);
        }
    }
}