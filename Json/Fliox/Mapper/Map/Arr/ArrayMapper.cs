﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.Mapper.Diff;
using Friflo.Json.Fliox.Mapper.Utils;

namespace Friflo.Json.Fliox.Mapper.Map.Arr
{
    internal sealed class ArrayMatcher : ITypeMatcher {
        public static readonly ArrayMatcher Instance = new ArrayMatcher();
        
        public TypeMapper MatchTypeMapper(Type type, StoreConfig config) {
            if (!type.IsArray)
                return null;
            Type elementType = type.GetElementType();
            int rank = type.GetArrayRank();
            if (rank > 1)
                return null; // todo implement multi dimensional array support
            if (ReflectUtils.IsAssignableFrom(typeof(object), elementType)) {
                ConstructorInfo constructor = null; // For arrays Arrays.CreateInstance(componentType, length) is used
                // ReSharper disable once ExpressionIsAlwaysNull
                object[] constructorParams = {config, type, elementType, constructor};
                // new ArrayMapper<T>(config, type, elementType, constructor);
                var newInstance = TypeMapperUtils.CreateGenericInstance(typeof(ArrayMapper<>), new[] {elementType}, constructorParams);
                return (TypeMapper) newInstance;
            }
            return null;
        }
    }
    
    
    internal sealed class ArrayMapper<TElm> : CollectionMapper <TElm[], TElm>
    {
        public override string  DataTypeName()              => $"{typeof(TElm).Name}[]";
        public override bool    IsNull(ref TElm[] value)    => value == null;
        public override int     Count(object array)         => ((TElm[]) array).Length; 

        public ArrayMapper(StoreConfig config, Type type, Type elementType, ConstructorInfo constructor) :
            base(config, type, elementType, 1, typeof(string), constructor) {
        }
        
        public override DiffType Diff(Differ differ, TElm[] left, TElm[] right) {
            if (left.Length != right.Length)
                return differ.AddNotEqualObject(left, right);
            
            differ.PushParent(left, right);
            for (int n = 0; n < left.Length; n++) {
                TElm leftItem  = left [n];
                TElm rightItem = right[n];
                if (differ.DiffElement(elementType, n, leftItem, rightItem) == DiffType.Equal)
                    continue;
                if (differ.DiffElements)
                    continue;
                return differ.PopParentNotEqual();
            }
            return differ.PopParent();
        }
        
        public override void PatchObject(Patcher patcher, object obj) {
            var list = (TElm[])obj;
            int index = patcher.GetElementIndex(list.Length);
            var element = list[index];
            var action = patcher.DescendElement(elementType, element, out TElm value);
            if (action == NodeAction.Assign) {
                list[index] = value;
            }
        }

        public override void Write(ref Writer writer, TElm[] slot) {
            int startLevel = writer.IncLevel();
            var arr = slot;
            writer.WriteArrayBegin();
            for (int n = 0; n < arr.Length; n++) {
                writer.WriteDelimiter(n);
                
                var elemVar = arr[n];
                if (elementType.IsNull(ref elemVar)) {
                    writer.AppendNull();
                } else {
                    elementType.Write(ref writer, elemVar);
                    writer.FlushFilledBuffer();
                }
            }
            writer.WriteArrayEnd();
            writer.DecLevel(startLevel);
        }

        public override TElm[] Read(ref Reader reader, TElm[] slot, out bool success) {
            if (!reader.StartArray(this, out success))
                return default;
            
            int startLen;
            int len;
            TElm[] array = default;

            if (EqualityComparer<TElm[]>.Default.Equals(slot, default)) {
                startLen = 0;
                len = Reader.minLen;
            }
            else {
                array = slot;
                startLen = len = array.Length;
            }
            
            int index = 0;
            while (true) {
                JsonEvent ev = reader.parser.NextEvent();
                switch (ev) {
                    case JsonEvent.ValueString:
                    case JsonEvent.ValueNumber:
                    case JsonEvent.ValueBool:
                    case JsonEvent.ArrayStart:
                    case JsonEvent.ObjectStart:
                    case JsonEvent.ValueNull:
                        TElm elemVar;
                        if (index < startLen) {
                            elemVar = array[index];
                            elemVar = reader.ReadElement(elementType, ref elemVar, out success);
                            if (!success)
                                return default;
                            array[index] = elemVar;
                        } else {
                            elemVar = default;
                            elemVar = reader.ReadElement(elementType, ref elemVar, out success);
                            if (!success)
                                return default;
                            if (index >= len)
                                array = CopyArray(array, len = Reader.Inc(len));
                            array[index] = elemVar;
                        }
                        index++;
                        break;
                    case JsonEvent.ArrayEnd:
                        if (index != len)
                            array = CopyArray(array, index);
                        success = true;
                        return array ?? Array.Empty<TElm>();
                    case JsonEvent.Error:
                        success = false;
                        return default;
                    default:
                        return reader.ErrorMsg<TElm[]>("unexpected state: ", ev, out success);
                }
            }
        }
        
        public override void Copy(TElm[] src, ref TElm[] dst) {
            int len = src.Length; 
            if (len != dst.Length) {
                dst = new TElm[len];
            }
            for (int n = 0; n  < len; n++) {
                elementType.Copy(src[n], ref dst[n]);
            }
        }
    }
}
