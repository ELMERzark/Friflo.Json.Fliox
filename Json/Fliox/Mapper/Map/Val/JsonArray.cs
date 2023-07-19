﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Runtime.InteropServices;
using Friflo.Json.Burst;
using Friflo.Json.Burst.Utils;

namespace Friflo.Json.Fliox.Mapper.Map.Val
{
    // ------------------------- PatchValueMatcher / PatchValueMapper -------------------------
    internal sealed class JsonArrayMatcher : ITypeMatcher {
        public static readonly JsonArrayMatcher Instance = new JsonArrayMatcher();
        
        internal static readonly Bytes True     = new Bytes("true");
        internal static readonly Bytes False    = new Bytes("false");
        internal static readonly Bytes Null     = new Bytes("null");
        
        public TypeMapper MatchTypeMapper(Type type, StoreConfig config) {
            if (type != typeof(JsonArray))
                return null;
            return new JsonArrayMapper (config, type);
        }
    }
    
    internal sealed class JsonArrayMapper : TypeMapper<JsonArray>
    {
        public override string  DataTypeName()              => "JsonArray";
        public override bool    IsNull(ref JsonArray value) => value == null;
        

        public JsonArrayMapper(StoreConfig config, Type type) : base (config, type, true, false) { }
        
        private static void WriteItems(ref Writer writer, JsonArray array)
        {
            int     pos         = 0;
            bool    isFirstItem = true;
            ref var bytes       = ref writer.bytes;
            
            while (true)
            {
                var itemType = array.GetItemType(pos, out int next);
                switch (itemType) {
                    case JsonItemType.Null:
                        bytes.AppendBytes(JsonArrayMatcher.Null);    
                        break;
                    case JsonItemType.True:
                    case JsonItemType.False: {
                        var value = array.ReadBool(pos);
                        if (value) {
                            bytes.AppendBytes(JsonArrayMatcher.True);
                        } else {
                            bytes.AppendBytes(JsonArrayMatcher.False);
                        }
                        break;
                    }
                    case JsonItemType.Uint8: {
                        var value = array.ReadUint8(pos);
                        writer.format.AppendLong(ref bytes, value);
                        break;
                    }
                    case JsonItemType.Int16: {
                        var value = array.ReadInt16(pos);
                        writer.format.AppendLong(ref bytes, value);
                        break;
                    }
                    case JsonItemType.Int32: {
                        var value = array.ReadInt32(pos);
                        writer.format.AppendLong(ref bytes, value);
                        break;
                    }
                    case JsonItemType.Int64: {
                        var value = array.ReadInt64(pos);
                        writer.format.AppendLong(ref bytes, value);
                        break;
                    }
                    case JsonItemType.Flt32: {
                        var value = array.ReadFlt32(pos);
                        writer.format.AppendFlt(ref bytes, value);
                        break;
                    }
                    case JsonItemType.Flt64: {
                        var value = array.ReadFlt64(pos);
                        writer.format.AppendFlt(ref bytes, value);
                        break;
                    }
                    case JsonItemType.ByteString: {
                        var value = array.ReadBytes(pos);
                        Utf8JsonWriter.AppendEscStringBytes(ref bytes, value);
                        break;
                    }
                    case JsonItemType.CharString: {
                        var value = array.ReadCharSpan(pos);
                        Utf8JsonWriter.AppendEscString(ref bytes, value);
                        break;
                    }
                    case JsonItemType.DateTime: {
                        var value = array.ReadDateTime(pos);
                        bytes.AppendChar('"');
                        bytes.AppendDateTime(value, writer.charBuf);
                        bytes.AppendChar('"');
                        break;
                    }
                    case JsonItemType.Guid: {
                        var value = array.ReadGuid(pos);
                        bytes.AppendChar('"');
                        bytes.AppendGuid(value);
                        bytes.AppendChar('"');
                        break;
                    }
                    case JsonItemType.End:
                        if (!isFirstItem) {
                            bytes.end--; // remove last terminator
                        }
                        return;
                    default:
                        throw new InvalidComObjectException($"unexpected itemType: {itemType}");
                }
                isFirstItem = false;
                bytes.AppendChar(',');
                pos = next;
            }
        }
        
        public override void Write(ref Writer writer, JsonArray array)
        {
            int startLevel = writer.IncLevel();
            writer.WriteArrayBegin();
            
            WriteItems(ref writer, array);
            
            writer.WriteArrayEnd();
            writer.DecLevel(startLevel);
        }
        
        private bool StartArray(ref Reader reader, out bool success) {
            ref var parser = ref reader.parser;
            var ev = parser.Event;
            switch (ev) {
                case JsonEvent.ValueNull:
                    reader.ErrorIncompatible<JsonArray>(DataTypeName(), this, out success);
                    return default;
                case JsonEvent.ArrayStart:
                    success = true;
                    return true;
                default:
                    success = false;
                    reader.ErrorIncompatible<JsonArray>(DataTypeName(), this, out success);
                    return false;
            }
        }

        public override JsonArray Read(ref Reader reader, JsonArray value, out bool success)
        {
            if (!StartArray(ref reader, out success)) {
                return default;
            }
            return ReadItems(ref reader, value, out success);
        }
        
        private static JsonArray ReadItems(ref Reader reader, JsonArray value, out bool success)
        {
            if (value == null) {
                value = new JsonArray();
            }
            ref var parser = ref reader.parser;
            while (true) {
                JsonEvent ev = reader.parser.NextEvent();
                switch (ev) {
                    case JsonEvent.ValueString:
                        var len = parser.value.Len;
                        var span = parser.value.AsSpan();
                        if (len == Bytes.GuidLength && Bytes.TryParseGuid(span, out var guid)) {
                            value.WriteGuid(guid);
                            break;
                        }
                        if (Bytes.TryParseDateTime(span, out var dateTime)) {
                            dateTime = dateTime.ToUniversalTime();
                            value.WriteDateTime(dateTime);
                            break;
                        }
                        value.WriteBytes(parser.value.AsSpan());
                        break;
                    case JsonEvent.ValueNumber:
                        if (parser.isFloat) {
                            var dbl = ValueParser.ParseDouble(parser.value.AsSpan(), ref reader.strBuf, out success);   // TODO - handle error
                            value.WriteFlt64(dbl);
                        } else {
                            var lng = ValueParser.ParseLong(parser.value.AsSpan(), ref reader.strBuf, out success);     // TODO - handle error
                            value.WriteInt64(lng); 
                        }
                        break;
                    case JsonEvent.ValueBool:
                        value.WriteBoolean(parser.boolValue);
                        break;
                    case JsonEvent.ArrayStart:
                        throw new NotImplementedException();
                    case JsonEvent.ObjectStart:
                        throw new NotImplementedException();
                    case JsonEvent.ValueNull:
                        value.WriteNull();
                        break;
                    case JsonEvent.ArrayEnd:
                        success = true;
                        return value;
                    case JsonEvent.Error:
                        success = false;
                        return default;
                    default:
                        return reader.ErrorMsg<JsonArray>("unexpected state: ", ev, out success);
                }
            }
        }
    }
}