﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;

namespace Friflo.Json.Fliox.Mapper.Map.Key
{
    internal sealed class JsonKeyMapper : KeyMapper<JsonKey>
    {
        public override void WriteKey (ref Writer writer, in JsonKey key) {
            switch (key.type) {
                case JsonKeyType.LONG:
                    writer.bytes.AppendChar('\"');
                    writer.format.AppendLong(ref writer.bytes, key.lng);
                    writer.bytes.AppendChar('\"');
                    break;
                case JsonKeyType.STRING:
                    writer.WriteJsonKey(key);
                    break;
                case JsonKeyType.GUID:
                    writer.WriteGuid(key.Guid);
                    break;
                default:
                    throw new InvalidOperationException($"cannot write JsonKey: {key}");
            }
        }
        
        public override JsonKey ReadKey (ref Reader reader, out bool success) {
            ref var parser = ref reader.parser;
            success = true;
            return new JsonKey(ref parser.key, ref parser.valueParser, default);
        }
        
        public override JsonKey     ToJsonKey      (in JsonKey key) {
            return key;
        }
        
        public override JsonKey      ToKey          (in JsonKey key) {
            return key;
        }
    }
}