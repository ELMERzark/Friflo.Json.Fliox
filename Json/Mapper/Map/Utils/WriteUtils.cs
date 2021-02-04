﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Runtime.CompilerServices;
using Friflo.Json.Burst;
using Friflo.Json.Mapper.Map.Obj.Reflect;

namespace Friflo.Json.Mapper.Map.Utils
{
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public static class WriteUtils
    {

        public static void WriteDiscriminator(JsonWriter writer, TypeMapper mapper) {
            ref var bytes = ref writer.bytes;
            bytes.AppendChar('{');
            bytes.AppendBytes(ref writer.discriminator);
            writer.typeCache.AppendDiscriminator(ref bytes, mapper);
            bytes.AppendChar('\"');
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteMemberKey(JsonWriter writer, PropField field, ref bool firstMember) {
            if (firstMember)
                writer.bytes.AppendBytes(ref field.firstMember);
            else
                writer.bytes.AppendBytes(ref field.subSeqMember);
            firstMember = false;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteObjectEnd(JsonWriter writer, bool emptyObject) {
            if (emptyObject)
                writer.bytes.AppendChar2('{', '}');
            else
                writer.bytes.AppendChar('}');
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteString(JsonWriter writer, String str) {
            JsonSerializer.AppendEscString(ref writer.bytes, ref str);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AppendNull(JsonWriter writer) {
            writer.bytes.AppendBytes(ref writer.@null);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IncLevel(JsonWriter writer) {
            if (writer.level++ < writer.maxDepth)
                return writer.level;
            throw new InvalidOperationException($"JsonParser: maxDepth exceeded. maxDepth: {writer.maxDepth}");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DecLevel(JsonWriter writer, int expectedLevel) {
            if (writer.level-- != expectedLevel)
                throw new InvalidOperationException($"Unexpected level in Write() end. Expect {expectedLevel}, Found: {writer.level + 1}");
        }


    }
}