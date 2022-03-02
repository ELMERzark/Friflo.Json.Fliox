﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Friflo.Json.Burst;
using Friflo.Json.Burst.Utils;
using Friflo.Json.Fliox.Mapper.Map.Obj.Reflect;
using Friflo.Json.Fliox.Mapper.MapIL.Obj;
using Friflo.Json.Fliox.Mapper.Utils;

namespace Friflo.Json.Fliox.Mapper.Map
{
    enum OutputType {
        ByteList,
        ByteWriter,
    }
    
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public partial struct Writer : IDisposable
    {
        /// <summary>Caches type meta data per thread and provide stats to the cache utilization</summary>
        public readonly     TypeCache           typeCache;
        public              Bytes               bytes;
        /// <summary>Can be used for custom mappers append a number while creating the JSON payload</summary>
        public              ValueFormat         format;
        internal            Bytes               @null;
        public              char[]              charBuf;
        internal            int                 level;
        public              int                 maxDepth;
        public              bool                pretty;
        public              bool                writeNullMembers;
        public              ITracerContext      tracerContext;
#if !UNITY_5_3_OR_NEWER
        private             int                 classLevel;
        private  readonly   List<ClassMirror>   mirrorStack;
#endif

        internal            OutputType          outputType;
#if JSON_BURST
        public              int                 writerHandle;
#else
        public              IBytesWriter        bytesWriter;
#endif
        public              int                 Level => level;

        public Writer(TypeStore typeStore) {
            bytes           = new Bytes(128);
            format          = new ValueFormat();
            format. InitTokenFormat();
            @null           = new Bytes("null");
            charBuf         = new char[128];
            typeCache       = new TypeCache(typeStore);
            level           = 0;
            maxDepth        = Utf8JsonParser.DefaultMaxDepth;
            outputType      = OutputType.ByteList;
            pretty          = false;
            writeNullMembers= true;
            tracerContext     = null;
#if JSON_BURST
            writerHandle    = -1;
#else
            bytesWriter     = null;
#endif
#if !UNITY_5_3_OR_NEWER
            classLevel      = 0;
            mirrorStack     = new List<ClassMirror>(16);
#endif
        }
        
        public void Dispose() {
            typeCache.Dispose();
            @null.Dispose();
            format.Dispose();
            bytes.Dispose();
            DisposeMirrorStack();
        }
        
        // --- WriteUtils
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteString(string str) {
            Utf8JsonWriter.AppendEscString(ref bytes, in str);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AppendNull() {
            bytes.AppendBytes(ref @null);
            FlushFilledBuffer();
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IncLevel() {
            if (level++ < maxDepth)
                return level;
            throw new InvalidOperationException($"JsonParser: maxDepth exceeded. maxDepth: {maxDepth}");
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DecLevel(int expectedLevel) {
            if (level-- != expectedLevel)
                throw new InvalidOperationException($"Unexpected level in Write() end. Expect {expectedLevel}, Found: {level + 1}");
        }
        
        // --- indentation
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteDelimiter(int pos) {
            if (pos > 0) {
                bytes.EnsureCapacityAbs(bytes.end + 1);
                bytes.buffer.array[bytes.end++] = (byte)',';
            }
            if (pretty)
                IndentBegin();
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteArrayBegin() {
            bytes.EnsureCapacityAbs(bytes.end + 1);
            bytes.buffer.array[bytes.end++] = (byte)'[';
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteArrayEnd() {
            if (pretty)
                IndentEnd();
            bytes.EnsureCapacityAbs(bytes.end + 1);
            bytes.buffer.array[bytes.end++] = (byte)']';
        }
        
        public void IndentBegin() {
            Utf8JsonWriter.IndentJsonNode(ref bytes, this.level);
        }
        
        public void IndentEnd() {
            int decLevel = this.level - 1;
            Utf8JsonWriter.IndentJsonNode(ref bytes, decLevel);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteObjectEnd(bool emptyObject) {
            if (pretty)
                IndentEnd();

            if (emptyObject) {
                bytes.EnsureCapacityAbs(bytes.end + 2);
                bytes.buffer.array[bytes.end++] = (byte)'{';
                bytes.buffer.array[bytes.end++] = (byte)'}';
            } else {
                bytes.EnsureCapacityAbs(bytes.end + 1);
                bytes.buffer.array[bytes.end++] = (byte)'}';
            }
        }
        
        // --- member keys
        public void WriteDiscriminator(TypeMapper baseMapper, TypeMapper mapper, ref bool firstMember) {
            var factory = baseMapper.instanceFactory;
            if (factory != null && factory.discriminator == null)
                return;
            
            bytes.AppendChar('{');
            if (pretty)
                IndentBegin();
            // --- discriminator
            bytes.AppendChar('"');
            bytes.AppendString(factory.discriminator);
            bytes.AppendChar('"');
            bytes.AppendChar(':');
            
            // --- discriminant
            bytes.AppendChar('"');
            bytes.AppendString(mapper.discriminant);
            bytes.AppendChar('\"');
            FlushFilledBuffer();
            firstMember = false;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void WriteKey<T>(KeyMapper<T> keyMapper, T key, int pos) {
            WriteDelimiter(pos);
            keyMapper.WriteKey(ref this, key);
            if (!pretty)
                bytes.AppendChar(':');
            else
                bytes.AppendChar2(':', ' ');
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteFieldKey(PropField field, ref bool firstMember) {
            if (!pretty) {
                if (firstMember)
                    bytes.AppendBytes(ref field.firstMember);
                else
                    bytes.AppendBytes(ref field.subSeqMember);
            } else {
                bytes.AppendChar(firstMember ? '{' : ',');
                IndentBegin();
                bytes.AppendChar('"');
                bytes.AppendBytes(ref field.nameBytes);
                bytes.AppendChar('"');
                bytes.AppendChar2(':', ' ');
            } 
            firstMember = false;
        }
        
        // --- Flush
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FlushFilledBuffer() {
            if (bytes.end < 4096)
                return;
            Flush();
        }
        
        public void Flush() {
            switch (outputType) {
                case OutputType.ByteList:
                    return; // ByteList mode does not support streaming
                case OutputType.ByteWriter:
#if JSON_BURST
                    NonBurstWriter.WriteNonBurst(writerHandle, ref bytes.buffer, bytes.end);
#else
                    bytesWriter.Write(ref bytes.buffer, bytes.end);
#endif                    
                    bytes.Clear();
                    break;
            }
        }
        
        // --- array element
        public void WriteElement<T>(TypeMapper<T> mapper, ref T value) {
#if !UNITY_5_3_OR_NEWER
            if (mapper.useIL) {
                TypeMapper typeMapper = mapper;
                ClassMirror mirror = InstanceLoad(mapper, ref typeMapper, ref value);
                mapper.WriteValueIL(ref this, mirror, 0, 0);
                return;
            }
#endif
            mapper.Write(ref this, value);
            
        }
        
        public void WriteGuid (in Guid guid) {
            bytes.AppendChar('\"');
            bytes.AppendGuid(guid, charBuf);
            bytes.AppendChar('\"');
        }
    }
}
