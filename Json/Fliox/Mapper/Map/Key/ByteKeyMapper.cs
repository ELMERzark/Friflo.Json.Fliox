﻿// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Burst.Utils;

namespace Friflo.Json.Fliox.Mapper.Map.Key
{
    internal sealed class ByteKeyMapper : KeyMapper<byte>
    {
        public override void WriteKey (ref Writer writer, in byte key) {
            writer.bytes.AppendChar('\"');
            writer.format.AppendLong(ref writer.bytes, key);
            writer.bytes.AppendChar('\"');
        }
        
        public override byte ReadKey (ref Reader reader, out bool success) {
            ref var parser = ref reader.parser;
            return (byte)ValueParser.ParseInt(parser.key.AsSpan(), ref parser.errVal, out success);
        }
        
        public override JsonKey     ToJsonKey      (in byte key) {
            return new JsonKey(key);
        }
        
        public override byte        ToKey          (in JsonKey key) {
            return (byte)key.AsLong();
        }
    }
}