// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Burst;
using Friflo.Json.Fliox;
using Friflo.Json.Fliox.Mapper;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Base
{
    internal enum ReadArrayType {
        Binary,
        Json
    }
        
    public static class TestJsonArray
    {
        private static readonly DateTime    DateTime    = DateTime.Parse("2023-07-19T12:58:57.448575Z").ToUniversalTime();
        private static readonly Guid        Guid        = Guid.Parse("af82dcf5-8664-4b4e-8072-6cb43b335364");
        private static readonly Bytes       JSONArray   = new Bytes("[1,2,3]");
        private static readonly Bytes       JSONObject  = new Bytes("{\"key\":42}");
        private static readonly Bytes       ByteString  = new Bytes("byte-string");
        
        
        private static void WriteTestData (JsonTable array) {
            array.WriteNull();                              // [0]
            array.WriteBoolean      (true);                 // [1]
            array.WriteByte         (255);                  // [2]
            array.WriteInt16        (short.MaxValue);       // [3]
            array.WriteInt32        (int.MaxValue);         // [4]
            array.WriteInt64        (long.MaxValue);        // [5]
            
            array.WriteFlt32        (float.MaxValue);       // [6]  3.4028235E+38f;
            array.WriteFlt64        (double.MaxValue);      // [7]  1.7976931348623157E+308;

            array.WriteJSON         (JSONArray.AsSpan());   // [8]
            array.WriteJSON         (JSONObject.AsSpan());  // [9]
            array.WriteByteString   (ByteString.AsSpan());  // [10]
            array.WriteCharString   ("test".AsSpan());      // [11]
            array.WriteCharString   ("chars".AsSpan());     // [12]
            array.WriteDateTime     (DateTime);             // [13]
            array.WriteGuid         (Guid);                 // [14]
            array.WriteNewRow       ();                     // [15]
            array.WriteCharString   ("new-row".AsSpan());   // [16]
        }
            
        private static void ReadTestData (JsonTable array, ReadArrayType readArrayType)
        {
            int n   = 0;
            int pos = 0;
            var idx = new int[18];
            while (true) {
                var type = array.GetItemType(pos, out int next);
                if (type == JsonItemType.End) {
                    break;
                }
                idx[n++] = pos;
                pos = next;
            }
            AreEqual(0,                                      idx[0]); // null
            IsTrue(                     array.ReadBool      (idx[1]));
            AreEqual(255,               array.ReadUint8     (idx[2]));
            AreEqual(short.MaxValue,    array.ReadInt16     (idx[3]));
            AreEqual(int.MaxValue,      array.ReadInt32     (idx[4]));
            AreEqual(long.MaxValue,     array.ReadInt64     (idx[5]));
#if !UNITY_5_3_OR_NEWER
            AreEqual(float.MaxValue,    array.ReadFlt32     (idx[6]));
            AreEqual(double.MaxValue,   array.ReadFlt64     (idx[7]));
#endif
            var bytes =                 array.ReadByteSpan  (idx[8]);
            IsTrue(bytes.SequenceEqual(JSONArray.AsSpan()));
            
            bytes =                     array.ReadByteSpan  (idx[9]);
            IsTrue(bytes.SequenceEqual(JSONObject.AsSpan()));
            
            bytes =                     array.ReadByteSpan  (idx[10]);
            IsTrue(bytes.SequenceEqual(ByteString.AsSpan()));
            
            if (readArrayType == ReadArrayType.Binary) {
                var chars =             array.ReadCharSpan  (idx[11]);
                IsTrue(chars.SequenceEqual("test".AsSpan()));
                
                chars =                 array.ReadCharSpan  (idx[12]);
                IsTrue(chars.SequenceEqual("chars".AsSpan()));
            } else {
                bytes =                 array.ReadByteSpan (idx[11]);
                IsTrue(bytes.SequenceEqual(new Bytes("test").AsSpan()));
                
                bytes =                 array.ReadByteSpan (idx[12]);
                IsTrue(bytes.SequenceEqual(new Bytes("chars").AsSpan()));
            }
            
            AreEqual(DateTime,          array.ReadDateTime  (idx[13]));
            AreEqual(Guid,              array.ReadGuid      (idx[14]));
            // AreEqual(132,                                    idx[15]); // new row
            if (readArrayType == ReadArrayType.Binary) {
                var newRow =            array.ReadCharSpan  (idx[16]).ToString();
                AreEqual("new-row", newRow);
            } else {
                var newRow =            array.ReadByteSpan (idx[16]);
                IsTrue(newRow.SequenceEqual(new Bytes("new-row").AsSpan()));
            }
        }
        
        [Test]
        public static void TestJsonArray_ReadWrite ()
        {
            var data = new JsonTable();
            WriteTestData(data);
            AreEqual(2,  data.RowCount);
            AreEqual(16, data.ItemCount);
            ReadTestData(data, ReadArrayType.Binary);
        }

        // Note! Unity format floating point numbers with lower precision
        private const string ExpectJson =
            @"[
[null,true,255,32767,2147483647,9223372036854775807,3.4028235E+38,1.7976931348623157E+308,[1,2,3],{""key"":42},""byte-string"",""test"",""chars"",""2023-07-19T12:58:57.448575Z"",""af82dcf5-8664-4b4e-8072-6cb43b335364""],
[""new-row""]
]";
        
        private const string ExpectToString =
            @"rows: 2
[null, true, 255, 32767, 2147483647, 9223372036854775807, 3.4028235E+38, 1.7976931348623157E+308, [1,2,3], {""key"":42}, 'byte-string', 'test', 'chars', 2023-07-19 12:58:57, af82dcf5-8664-4b4e-8072-6cb43b335364],
['new-row']";


        [Test]
        public static void TestJsonArray_MapperWrite () 
        {
            var typeStore = new TypeStore();
            var mapper = new ObjectMapper(typeStore);
            
            // --- empty table
            var data = new JsonTable();
            AreEqual(0, data.RowCount);
            AreEqual(0, data.ColumnCount);
            AreEqual(0, data.ItemCount);
            var json = mapper.Write(data);
            AreEqual("[]", json);
            AreEqual("rows: 0, columns: 0\n[]", data.TableString);
            
            // --- [[]]
            data.WriteNewRow();
            AreEqual(1, data.RowCount);
            AreEqual(0, data.ColumnCount);
            AreEqual(0, data.ItemCount);
            json = mapper.Write(data);
            AreEqual("[\n[]\n]", json);
            AreEqual("rows: 1, columns: 0\n[]", data.TableString);
            
            // --- [[],[]] -> trailing new rows are ignored
            data.WriteNewRow();
            AreEqual(2, data.RowCount);
            AreEqual(0, data.ColumnCount);
            AreEqual(0, data.ItemCount);
            json = mapper.Write(data);
            AreEqual("[\n[],\n[]\n]", json);
            AreEqual("rows: 2, columns: 0\n[],\n[]", data.TableString);
            
            
            data = new JsonTable();
            // --- [1]
            data.WriteInt16(1);
            AreEqual(1, data.RowCount);
            AreEqual(1, data.ColumnCount);
            AreEqual(1, data.ItemCount);
            json = mapper.Write(data);
            AreEqual("[\n[1]\n]", json);
            AreEqual("rows: 1, columns: 1\n[1]", data.TableString);
            
            // --- [1,2]
            data.WriteInt16(2);
            AreEqual(1, data.RowCount);
            AreEqual(2, data.ColumnCount);
            AreEqual(2, data.ItemCount);
            json = mapper.Write(data);
            AreEqual("[\n[1,2]\n]", json);
            AreEqual("rows: 1, columns: 2\n[1, 2]", data.TableString);
            
            // --- [1,2] -> trailing new rows are ignored
            data.WriteNewRow();
            AreEqual(1, data.RowCount);
            AreEqual(2, data.ColumnCount);
            AreEqual(2, data.ItemCount);
            json = mapper.Write(data);
            AreEqual("[\n[1,2]\n]", json);
            AreEqual("rows: 1, columns: 2\n[1, 2]", data.TableString);
            
            // --- [1,2],[3]
            data.WriteInt16(3);
            AreEqual(2, data.RowCount);
            AreEqual(-1,data.ColumnCount);
            AreEqual(3, data.ItemCount);
            json = mapper.Write(data);
            AreEqual("[\n[1,2],\n[3]\n]", json);
            AreEqual("rows: 2\n[1, 2],\n[3]", data.TableString);

            // --- [1,2],[3,4]
            data.WriteInt16(4);
            AreEqual(2, data.RowCount);
            AreEqual(2, data.ColumnCount);
            AreEqual(4, data.ItemCount);
            json = mapper.Write(data);
            AreEqual("[\n[1,2],\n[3,4]\n]", json);
            AreEqual("rows: 2, columns: 2\n[1, 2],\n[3, 4]", data.TableString);
            
            // --- [1,2],[3,4]
            data.WriteNewRow();
            AreEqual(2, data.RowCount);
            AreEqual(2, data.ColumnCount);
            AreEqual(4, data.ItemCount);
            json = mapper.Write(data);
            AreEqual("[\n[1,2],\n[3,4]\n]", json);
            AreEqual("rows: 2, columns: 2\n[1, 2],\n[3, 4]", data.TableString);

            data.Init();
            WriteTestData(data);
            json = mapper.Write(data);
            var tableString = data.TableString;
#if !UNITY_5_3_OR_NEWER
            AreEqual(ExpectToString, tableString);
            AreEqual(ExpectJson, json);
#endif
        }
        
        [Test]
        public static void TestJsonArray_MapperRead () 
        {
            var typeStore = new TypeStore();
            var mapper = new ObjectMapper(typeStore);

            var data = mapper.Read<JsonTable>(ExpectJson);
            AreEqual(2,  data.RowCount);
            AreEqual(16, data.ItemCount);
            
            ReadTestData(data, ReadArrayType.Json);
        }
        
        [Test]
        public static void TestJsonArray_ReadErrors () 
        {
            var typeStore = new TypeStore();
            var mapper = new ObjectMapper(typeStore);

            var e = Throws<JsonReaderException>(() => {
                 mapper.Read<JsonTable>("[[123-]]");
            });
            IsNotNull(e);
            AreEqual("JsonReader/error: invalid integer: 123- path: '[0][0]' at position: 6", e.Message);

            e = Throws<JsonReaderException>(() => {
                mapper.Read<JsonTable>("[[123e+38.999]]");
            });
            IsNotNull(e);
            AreEqual("JsonReader/error: invalid floating point number: 123e+38.999 path: '[0][0]' at position: 13", e.Message);
        }
    }
}