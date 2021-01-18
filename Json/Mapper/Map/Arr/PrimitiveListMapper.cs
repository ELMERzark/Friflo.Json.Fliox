﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Friflo.Json.Burst;
using Friflo.Json.Mapper.Types;
using Friflo.Json.Mapper.Utils;

namespace Friflo.Json.Mapper.Map.Arr
{
    
    public class PrimitiveListMapper<T> : IJsonMapper
    {
        public static readonly PrimitiveListMapper<object>   ObjectInterface =   new PrimitiveListMapper<object>  (typeof(object));
        public static readonly PrimitiveListMapper<double>   DoubleInterface =   new PrimitiveListMapper<double>  (typeof(double));
        public static readonly PrimitiveListMapper<float>    FloatInterface =    new PrimitiveListMapper<float>   (typeof(float));
        public static readonly PrimitiveListMapper<long>     LongInterface =     new PrimitiveListMapper<long>    (typeof(long));
        public static readonly PrimitiveListMapper<int>      IntInterface =      new PrimitiveListMapper<int>     (typeof(int));
        public static readonly PrimitiveListMapper<short>    ShortInterface =    new PrimitiveListMapper<short>   (typeof(short));
        public static readonly PrimitiveListMapper<byte>     ByteInterface =     new PrimitiveListMapper<byte>    (typeof(byte));
        public static readonly PrimitiveListMapper<bool>     BoolInterface =     new PrimitiveListMapper<bool>    (typeof(bool));

        private readonly Type       elemType;
        private readonly VarType    elemVarType;
        
        public PrimitiveListMapper (Type type) {
            elemType = type;
            elemVarType = Var.GetVarType(elemType);
        }
        
        public StubType CreateStubType(Type type) {
            if (StubType.IsStandardType(type)) // dont handle standard types
                return null;
            Type[] args = Reflect.GetGenericInterfaceArgs (type, typeof( IList<>) );
            if (args != null) {
                Type elementType = args[0];
                if (elemVarType == VarType.Object)
                    return null;
                if (elemType != elementType)
                    return null;
                
                ConstructorInfo constructor = Reflect.GetDefaultConstructor(type);
                if (constructor == null)
                    constructor = Reflect.GetDefaultConstructor( typeof(List<>).MakeGenericType(elementType) );
                return new CollectionType  (type, elementType, this, 1, null, constructor);
            }
            return null;
        }

        public void Write(JsonWriter writer, ref Var slot, StubType stubType) {
            List<T> list = (List<T>) slot.Obj;
            CollectionType collectionType = (CollectionType) stubType;
            writer.bytes.AppendChar('[');
            StubType elementType = collectionType.ElementType;
            Var elemVar = new Var();
            for (int n = 0; n < list.Count; n++) {
                if (n > 0) writer.bytes.AppendChar(',');
                Object item = list[n];
                if (item != null) {
                    // todo: implement missing element types
                    switch (collectionType.elementVarType) {
                        case VarType.Object:
                            elemVar.Obj = item;
                            elementType.map.Write(writer, ref elemVar, elementType);
                            break;
                        default:
                            throw new FrifloException("List element type not supported: " +
                                                      collectionType.ElementType.type.Name);
                    }
                }
                else
                    writer.bytes.AppendBytes(ref writer.@null);
            }
            writer.bytes.AppendChar(']');
        }
        

        public bool Read(JsonReader reader, ref Var slot, StubType stubType) {
            if (!ArrayUtils.StartArray(reader, ref slot, stubType, out bool startSuccess))
                return startSuccess;
            
            ref var parser = ref reader.parser;
            CollectionType collectionType = (CollectionType) stubType;
            List<T> list = (List<T>) slot.Obj;
            if (list == null)
                list = (List<T>) collectionType.CreateInstance();
            StubType elementType = collectionType.ElementType;

            int startLen = list.Count;
            int index = 0;
            Var elemVar = new Var();
            while (true) {
                JsonEvent ev = parser.NextEvent();
                switch (ev) {
                    case JsonEvent.ValueString:
                        if (elementType.typeCat != TypeCat.String)
                            return reader.ErrorIncompatible("List element", elementType, ref parser);
                        elemVar.Clear();
                        if (!elementType.map.Read(reader, ref elemVar, elementType))
                            return false;
                        ArrayUtils.AddListItem(list, ref elemVar, elemVarType, index++, startLen);
                        break;
                    case JsonEvent.ValueNumber:
                        if (elementType.typeCat != TypeCat.Number)
                            return reader.ErrorIncompatible("List element", elementType, ref parser);
                        elemVar.Clear();
                        if (!elementType.map.Read(reader, ref elemVar, elementType))
                            return false;
                        ArrayUtils.AddListItem(list, ref elemVar, elemVarType, index++, startLen);
                        break;
                    case JsonEvent.ValueBool:
                        if (elementType.typeCat != TypeCat.Bool)
                            return reader.ErrorIncompatible("List element", elementType, ref parser);
                        elemVar.Clear();
                        if (!elementType.map.Read(reader, ref elemVar, elementType))
                            return false;
                        ArrayUtils.AddListItem(list, ref elemVar, elemVarType, index++, startLen);
                        break;
                    case JsonEvent.ValueNull:
                        if (!elementType.isNullable)
                            return reader.ErrorIncompatible("List element", elementType, ref parser);
                        elemVar.Obj = null;
                        ArrayUtils.AddListItem(list, ref elemVar, elemVarType, index++, startLen);
                        break;
                    case JsonEvent.ArrayStart:
                        StubType subElementType = collectionType.ElementType;
                        if (index < startLen) {
                            elemVar.Obj = list[index];
                            if (!subElementType.map.Read(reader, ref elemVar, subElementType))
                                return false;
                            ArrayUtils.AddListItem(list, ref elemVar, elemVarType, index++, startLen);
                        }
                        else {
                            elemVar.Clear();
                            if (!subElementType.map.Read(reader, ref elemVar, subElementType))
                                return false;
                            ArrayUtils.AddListItem(list, ref elemVar, elemVarType, index++, startLen);
                        }
                        break;
                    case JsonEvent.ObjectStart:
                        if (index < startLen) {
                            elemVar.Obj = list[index];
                            if (!elementType.map.Read(reader, ref elemVar, elementType))
                                return false;
                            ArrayUtils.AddListItem(list, ref elemVar, elemVarType, index++, startLen);
                        }
                        else {
                            elemVar.Clear();
                            if (!elementType.map.Read(reader, ref elemVar, elementType))
                                return false;
                            ArrayUtils.AddListItem(list, ref elemVar, elemVarType, index++, startLen);
                        }
                        break;
                    case JsonEvent.ArrayEnd:
                        if (startLen - index > 0)
                            list.RemoveRange(index, startLen - index);
                        slot.Obj = list;
                        return true;
                    case JsonEvent.Error:
                        return false;
                    default:
                        return reader.ErrorNull("unexpected state: ", ev);
                }
            }
        }
    }
}