// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using static System.Diagnostics.DebuggerBrowsableState;
using Browse = System.Diagnostics.DebuggerBrowsableAttribute;

// ReSharper disable ReturnTypeCanBeEnumerable.Global
namespace Friflo.Json.Fliox.Hub.DB.Cluster
{
    public class RawSqlResult
    {
        // --- public
                        public  int             RowCount   => values.Count / types.Length;
                        public  FieldType[]     types;
                        public  JsonArray       values;
                        public  int             ColumnCount => types.Length;
                        public  RawSqlRow[]     Rows        => rows ?? GetRows();
        
        // --- private / internal
        [Browse(Never)] private int[]           indexArray;
        [Browse(Never)] private RawSqlRow[]     rows;

        public override         string          ToString()  => $"rows: {RowCount}, columns; {ColumnCount}";

        public   RawSqlRow      GetRow(int row) {
            if (row < 0 || row >= RowCount) throw new IndexOutOfRangeException(nameof(row));
            var indexes = GetIndexes();
            return new RawSqlRow(this, indexes, row, ColumnCount);
        }
        
        private RawSqlRow[] GetRows() { 
            var indexes     = GetIndexes();
            var rowCount    = RowCount;
            var result      = new RawSqlRow[rowCount];
            for (int row = 0; row < rowCount; row++) {
                result[row] = new RawSqlRow(this, indexes, row, ColumnCount);
            }
            return result;
        }

        internal int[] GetIndexes() {
            if (indexArray != null) {
                return indexArray;
            }
            var indexes = new int[values.Count + 1];
            int n   = 0;
            int pos = 0;
            while (true)
            {
                var type = values.GetItemType(pos, out int next);
                if (type == JsonItemType.End) {
                    break;
                }
                indexes[n++] = pos;
                pos = next;
            }
            indexes[n] = pos;
            return indexArray = indexes;
        }
    }
    
    public readonly struct RawSqlRow
    {
        // --- public
                        public  readonly    int             index;
                        public  readonly    int             count;
        
        // --- private
        [Browse(Never)] private readonly    RawSqlResult    rawResult;

        public  override    string      ToString()          => GetString();
    //  public ReadOnlySpan<JsonKey>    Values              => values.AsSpan().Slice(index * count, count);
    //  public JsonKey                  this[int column]    => values[index * count + column];

        internal RawSqlRow(RawSqlResult rawResult, int[] indexes, int index, int count) {
            this.index      = index;
            this.count      = count;
            this.rawResult  = rawResult;
        }
        
        private string GetString() {
            var indexes = rawResult.GetIndexes();
            var first   = index * count;
            var start   = indexes[first];
            var end     = indexes[first + count];
            var array   = new JsonArray(count, rawResult.values, start, end);
            return array.AsString();
        }
    }
    
    public enum FieldType
    {
        Unknown     =  0,
        //
        Bool        =  1,
        //
        UInt8       =  2,
        Int16       =  3,
        Int32       =  4,
        Int64       =  5,
        //
        String      =  6,
        DateTime    =  7,
        Guid        =  8,
        //
        Float       =  9,
        Double      = 10,
        //
        JSON        = 11,
    }
}