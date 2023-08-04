// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Data.Common;
using Friflo.Json.Fliox.Hub.DB.Cluster;
using Friflo.Json.Fliox.Hub.Host.Utils;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;

// ReSharper disable ConvertTypeCheckPatternToNullCheck
namespace Friflo.Json.Fliox.Hub.Host.SQL
{
    public static partial class SQLTable
    {
        // ---------------------------------------- sync / async ----------------------------------------
        
        /// <summary> counterpart <see cref="ReadEntitiesAsync"/></summary>
        public static ReadEntitiesResult ReadEntitiesSync(
            DbDataReader    reader,
            ISQL2JsonMapper mapper,
            ReadEntities    query,
            TableInfo       tableInfo,
            SyncContext     syncContext)
        {
            var typeMapper = query.typeMapper;
            if (typeMapper == null) {
                using var pooled = syncContext.pool.SQL2Json.Get();
                var buffer   = syncContext.MemoryBuffer;
                var entities = mapper.ReadEntitiesSync(pooled.instance, tableInfo, buffer);
                var array    = KeyValueUtils.EntityListToArray(entities, query.ids);
                return new ReadEntitiesResult { entities = new Entities(array) };
            }
            var binaryReader    = new BinaryDbDataReader();
            binaryReader.Init(reader);
            var objects         = new EntityObject[query.ids.Count];
            int count           = 0;
            while (reader.Read())
            {
                binaryReader.NextRow();
                var obj = typeMapper.ReadBinary(binaryReader, null, out bool _);
                var key = query.ids[count];
                objects[count++] = new EntityObject(key, obj);
            }
            return new ReadEntitiesResult { entities = new Entities(objects) };
        }
        
        /// <summary> counterpart <see cref="ReadObjectsAsync"/></summary>
        public static  ReadEntitiesResult ReadObjectsSync(
            DbDataReader    reader,
            ReadEntities    query,
            SQL2Object      sql2Object)
        {
            var typeMapper      = query.typeMapper;
            var binaryReader    = sql2Object.reader;
            binaryReader.Init(reader);
            var objects         = new EntityObject[query.ids.Count];
            int count           = 0;
            while (reader.Read())
            {
                binaryReader.NextRow();
                var obj = typeMapper.ReadBinary(binaryReader, null, out bool _);
                var key = query.ids[count];
                objects[count++] = new EntityObject(key, obj);
            }
            return new ReadEntitiesResult { entities = new Entities(objects) };
        }
        
        /// <summary> counterpart <see cref="QueryEntitiesAsync"/></summary>
        public static List<EntityValue> QueryEntitiesSync(
            DbDataReader    reader,
            TableInfo       tableInfo,
            SyncContext     syncContext)
        {
            using var pooled = syncContext.pool.SQL2Json.Get();
            var mapper   = new SQL2JsonMapper(reader);
            var buffer   = syncContext.MemoryBuffer;
            return mapper.ReadEntitiesSync(pooled.instance, tableInfo, buffer);
        }
        
        /// <summary> counterpart <see cref="ReadRowsAsync"/></summary>
        public static RawSqlResult ReadRowsSync(DbDataReader reader) {
            var columns     = GetFieldTypes(reader);
            var data        = new JsonTable();
            var readRawSql  = new ReadRawSql(reader);
            var rowCount    = 0;
            while (reader.Read())
            {
                rowCount++;
                AddRow(reader, columns, data, readRawSql);
                data.WriteNewRow();
            }
            return new RawSqlResult(columns, data, rowCount);
        }
        
        // -------------------------------------- end: sync / async --------------------------------------
    }
}