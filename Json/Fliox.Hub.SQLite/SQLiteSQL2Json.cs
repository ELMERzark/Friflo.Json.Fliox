using System;
using System.Collections.Generic;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.Hub.Host.SQL;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Utils;
using SQLitePCL;

namespace Friflo.Json.Fliox.Hub.SQLite
{
    public class SQLiteSQL2Json : ISQL2JsonMapper
    {
        private readonly    sqlite3_stmt        stmt;
        private readonly    SQL2Json            sql2Json;
        private readonly    TableInfo           tableInfo;
        
        public SQLiteSQL2Json(SQL2Json sql2Json, sqlite3_stmt stmt, TableInfo tableInfo) {
            this.sql2Json   = sql2Json;
            this.stmt       = stmt;
            this.tableInfo  = tableInfo;
        }
        
        internal bool ReadValues(
            int?                    maxCount,                     
            MemoryBuffer            buffer,
            out TaskExecuteError    error)
        {
            int count   = 0;
            var columns = tableInfo.columns;
            var cells   = sql2Json.cells;
            sql2Json.InitMapper(this, tableInfo, buffer);
            while (true) {
                var rc = raw.sqlite3_step(stmt);
                if (rc == raw.SQLITE_ROW) {
                    for (int n = 0; n < columns.Length; n++) {
                        var column = columns[n]; 
                        if (raw.sqlite3_column_type(stmt, column.ordinal) == raw.SQLITE_NULL) {
                            continue;
                        } 
                        ReadCell(column, ref cells[n]);
                    }
                    sql2Json.AddRow();
                } else if (rc == raw.SQLITE_DONE) {
                    break;
                } else {
                    return SQLiteUtils.Error("step failed", out error);
                }
            }
            sql2Json.Cleanup();
            return SQLiteUtils.Success(out error);
        }
        
        private static int BindKey(sqlite3_stmt stmt, in JsonKey key, ref Bytes bytes) {
            var encoding = key.GetEncoding();
            switch (encoding) {
                case JsonKeyEncoding.LONG:
                    return raw.sqlite3_bind_int64(stmt, 1, key.AsLong());
                case JsonKeyEncoding.STRING:
                    return raw.sqlite3_bind_text (stmt, 1, key.AsString());
                case JsonKeyEncoding.STRING_SHORT:
                case JsonKeyEncoding.GUID:
                    key.ToBytes(ref bytes);
                    return raw.sqlite3_bind_text (stmt, 1, bytes.AsSpan());
                default:
                    throw new InvalidOperationException("unhandled case");
            }
        }
        
        public void ReadEntities(List<JsonKey> keys, MemoryBuffer buffer)
        {
            var columns = tableInfo.columns;
            var cells   = sql2Json.cells;
            var values  = sql2Json.result;
            var bytes   = new Bytes(36);        // TODO - OPTIMIZE: reuse
            sql2Json.InitMapper(this, tableInfo, buffer);
            foreach (var key in keys) {
                var rc  = BindKey(stmt, key, ref bytes);
                if (rc != raw.SQLITE_OK) {
                    return; // return Error($"bind key failed. error: {rc}, key: {key}", out error);
                }
                rc = raw.sqlite3_step(stmt);
                switch (rc) {
                    case raw.SQLITE_DONE: 
                        values.Add(new EntityValue(key));
                        break;
                    case raw.SQLITE_ROW:
                        for (int n = 0; n < columns.Length; n++) {
                            var column = columns[n];
                            ReadCell(column, ref cells[n]);
                        }
                        sql2Json.AddRow();
                        var data    = raw.sqlite3_column_blob(stmt, 1);
                        var value   = buffer.Add(data);
                        values.Add(new EntityValue(key, value));
                        break;
                    default:
                        return; // return Error($"step failed. error: {rc}, key: {key}", out error);
                }
                rc = raw.sqlite3_reset(stmt);
                if (rc != raw.SQLITE_OK) {
                    return; // return Error($"reset failed. error: {rc}, key: {key}", out error);
                }
            }
            sql2Json.Cleanup();
        }
    
        private void ReadCell(ColumnInfo column, ref ReadCell cell)
        {
            switch (column.type) {
                case ColumnType.Boolean:
                case ColumnType.Uint8:
                case ColumnType.Int16:
                case ColumnType.Int32:
                case ColumnType.Int64:
                case ColumnType.Object:
                    cell.lng = raw.sqlite3_column_int64(stmt, column.ordinal);
                    break;
                case ColumnType.Float:
                case ColumnType.Double:
                    cell.dbl = raw.sqlite3_column_double(stmt, column.ordinal);
                    break;
                case ColumnType.Guid: {
                    var data = raw.sqlite3_column_blob(stmt, column.ordinal);
                    if (!Bytes.TryParseGuid(data, out cell.guid)) {
                        throw new InvalidOperationException("invalid guid");
                    }
                    break;
                }
                case ColumnType.DateTime: {
                    var data = raw.sqlite3_column_blob(stmt, column.ordinal);
                    if (!Bytes.TryParseDateTime(data, out cell.date)) {
                        throw new InvalidOperationException("invalid datetime");
                    }
                    break;
                }
                case ColumnType.BigInteger:
                case ColumnType.String:
                case ColumnType.Enum:
                case ColumnType.Array: {
                    var data = raw.sqlite3_column_blob(stmt, column.ordinal);
                    sql2Json.CopyBytes(data, ref cell);
                    break;
                }
            }
        }
        
        public void WriteColumn(SQL2Json sql2Json, ColumnInfo column)
        {
            ref var cell    = ref sql2Json.cells[column.ordinal];
            ref var writer  = ref sql2Json.writer;
            var key         = column.nameBytes;
            if (cell.isNull) {
                // writer.MemberNul(key); // omit writing member with value null
                return;
            }
            cell.isNull = true;
            switch (column.type) {
                case ColumnType.Boolean:    writer.MemberBln    (key, cell.lng != 0);   break;
                //
                case ColumnType.String:
                case ColumnType.Enum:
                case ColumnType.BigInteger: writer.MemberStr    (key, cell.bytes);      break;
                //
                case ColumnType.Uint8:
                case ColumnType.Int16:
                case ColumnType.Int32:
                case ColumnType.Int64:      writer.MemberLng    (key, cell.lng);        break;
                //
                case ColumnType.Float:
                case ColumnType.Double:     writer.MemberDbl    (key, cell.dbl);        break;
                //
                case ColumnType.Guid:       writer.MemberGuid   (key, cell.guid);       break;
                case ColumnType.DateTime:   writer.MemberDate   (key, cell.date);       break;
                case ColumnType.Array:      writer.MemberArr    (key, cell.bytes);      break;
                default:
                    throw new InvalidOperationException($"unexpected type: {column.type}");
            }
        }
    }
}