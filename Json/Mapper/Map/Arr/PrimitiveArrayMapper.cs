﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Reflection;
using Friflo.Json.Burst;
using Friflo.Json.Mapper.Map.Utils;
using Friflo.Json.Mapper.Types;
using Friflo.Json.Mapper.Utils;

namespace Friflo.Json.Mapper.Map.Arr
{
    public static class PrimitiveArray
    {
        public static readonly PrimitiveArrayMapper<double>   DoubleInterface =     new PrimitiveArrayMapper<double>  ();
        public static readonly PrimitiveArrayMapper<float>    FloatInterface =      new PrimitiveArrayMapper<float>   ();
        public static readonly PrimitiveArrayMapper<long>     LongInterface =       new PrimitiveArrayMapper<long>    ();
        public static readonly PrimitiveArrayMapper<int>      IntInterface =        new PrimitiveArrayMapper<int>     ();
        public static readonly PrimitiveArrayMapper<short>    ShortInterface =      new PrimitiveArrayMapper<short>   ();
        public static readonly PrimitiveArrayMapper<byte>     ByteInterface =       new PrimitiveArrayMapper<byte>    ();
        public static readonly PrimitiveArrayMapper<bool>     BoolInterface =       new PrimitiveArrayMapper<bool>    ();
        //
        public static readonly PrimitiveArrayMapper<double?>  DoubleNulInterface =  new PrimitiveArrayMapper<double?>  ();
        public static readonly PrimitiveArrayMapper<float?>   FloatNulInterface =   new PrimitiveArrayMapper<float?>   ();
        public static readonly PrimitiveArrayMapper<long?>    LongNulInterface =    new PrimitiveArrayMapper<long?>    ();
        public static readonly PrimitiveArrayMapper<int?>     IntNulInterface =     new PrimitiveArrayMapper<int?>     ();
        public static readonly PrimitiveArrayMapper<short?>   ShortNulInterface =   new PrimitiveArrayMapper<short?>   ();
        public static readonly PrimitiveArrayMapper<byte?>    ByteNulInterface =    new PrimitiveArrayMapper<byte?>    ();
        public static readonly PrimitiveArrayMapper<bool?>    BoolNulInterface =    new PrimitiveArrayMapper<bool?>    ();

        public static readonly PrimitiveArrayMapper<string>   StringInterface =     new PrimitiveArrayMapper<string>   ();

        public static void SetArrayItemNull(Array array, int index, VarType varType) {
            switch (varType) {
                case VarType.Object:    ((object[])  array)[index]= null;   return;
                case VarType.Double:    ((double?[]) array)[index]= null;   return;
                case VarType.Float:     ((float?[])  array)[index]= null;   return;
                case VarType.Long:      ((long?[])   array)[index]= null;   return;
                case VarType.Int:       ((int?[])    array)[index]= null;   return;
                case VarType.Short:     ((short?[])  array)[index]= null;   return;
                case VarType.Byte:      ((byte?[])   array)[index]= null;   return;
                case VarType.Bool:      ((bool?[])   array)[index]= null;   return;
                default:
                    throw new InvalidOperationException("varType not supported: " + varType);
            }
        }

        public static void SetArrayItem (Array array, ref Var item, VarType varType, int index, bool nullable) {
            if (nullable) {
                switch (varType) {
                    case VarType.Object:    ((object[])  array)[index]= item.Obj;    return;
                    case VarType.Double:    ((double?[]) array)[index]= item.Dbl;    return;
                    case VarType.Float:     ((float?[])  array)[index]= item.Flt;    return;
                    case VarType.Long:      ((long?[])   array)[index]= item.Lng;    return;
                    case VarType.Int:       ((int?[])    array)[index]= item.Int;    return;
                    case VarType.Short:     ((short?[])  array)[index]= item.Short;  return;
                    case VarType.Byte:      ((byte?[])   array)[index]= item.Byte;   return;
                    case VarType.Bool:      ((bool?[])   array)[index]= item.Bool;   return;
                    default:
                        throw new InvalidOperationException("varType not supported: " + varType);
                }
            } else {
                switch (varType) {
                    case VarType.Object:    ((object[]) array)[index]= item.Obj;    return; // remove - object is always nullable
                    case VarType.Double:    ((double[]) array)[index]= item.Dbl;    return;
                    case VarType.Float:     ((float[])  array)[index]= item.Flt;    return;
                    case VarType.Long:      ((long[])   array)[index]= item.Lng;    return;
                    case VarType.Int:       ((int[])    array)[index]= item.Int;    return;
                    case VarType.Short:     ((short[])  array)[index]= item.Short;  return;
                    case VarType.Byte:      ((byte[])   array)[index]= item.Byte;   return;
                    case VarType.Bool:      ((bool[])   array)[index]= item.Bool;   return;
                    default:
                        throw new InvalidOperationException("varType not supported: " + varType);
                }
            }
        }
        
        public static void GetArrayItem(Array array, ref Var item, VarType varType, int index, bool nullable) {
            if (!nullable) {
                switch (varType) {
                    case VarType.Object:    item.Obj =  ((object[])array)[index];    return; // remove - object is always nullable
                    case VarType.Double:    item.Dbl =  ((double[])array)[index];    return;
                    case VarType.Float:     item.Flt =  ((float[]) array)[index];    return;
                    case VarType.Long:      item.Lng =  ((long[])  array)[index];    return;
                    case VarType.Int:       item.Int =  ((int[])   array)[index];    return;
                    case VarType.Short:     item.Short =((short[]) array)[index];    return;
                    case VarType.Byte:      item.Byte = ((byte[])  array)[index];    return;
                    case VarType.Bool:      item.Bool=  ((bool[])  array)[index];    return;
                    default:
                        throw new InvalidOperationException("varType not supported: " + varType);
                }
            } else {
                switch (varType) {
                    case VarType.Object: item.Obj =      ((object []) array)[index];   return;
                    case VarType.Double: item.NulDbl =   ((double?[]) array)[index];   return;
                    case VarType.Float:  item.NulFlt =   ((float? []) array)[index];   return;
                    case VarType.Long:   item.NulLng =   ((long?  []) array)[index];   return;
                    case VarType.Int:    item.NulInt =   ((int?   []) array)[index];   return;
                    case VarType.Short:  item.NulShort = ((short? []) array)[index];   return;
                    case VarType.Byte:   item.NulByte =  ((byte?  []) array)[index];   return;
                    case VarType.Bool:   item.NulBool =  ((bool?  []) array)[index];   return;

                    default:
                        throw new InvalidOperationException("varType not supported: " + varType);
                }
            }  
        }
    }
    
    
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class PrimitiveArrayMapper<T> : ITypeMapper
    {
        private readonly Type       elemType;
        private readonly VarType    elemVarType;
        
        public string DataTypeName() { return "array"; }
        
        public PrimitiveArrayMapper () {
            elemType            = typeof(T);
            elemVarType         = Var.GetVarType(elemType);
        }
        
        public StubType CreateStubType(Type type) {
            if (type. IsArray) {
                Type elementType = type.GetElementType();
                int rank = type.GetArrayRank();
                if (rank > 1)
                    return null; // todo implement multi dimensional array support
                if (elementType == elemType) {
                    ConstructorInfo constructor = null; // For arrays Arrays.CreateInstance(componentType, length) is used
                    // ReSharper disable once ExpressionIsAlwaysNull
                    return new CollectionType(type, elementType, this, type.GetArrayRank(), null, constructor);
                }
            }
            return null;
        }

        public void Write(JsonWriter writer, ref Var slot, StubType stubType) {
            T[] array = (T[]) slot.Obj;
            CollectionType collectionType = (CollectionType) stubType;
            writer.bytes.AppendChar('[');
            var elementType = collectionType.ElementType;
            Var elemVar = new Var();
            bool nullable = collectionType.ElementType.isNullable;
            for (int n = 0; n < array.Length; n++) {
                if (n > 0)
                    writer.bytes.AppendChar(',');
                PrimitiveArray.GetArrayItem(array, ref elemVar, elemVarType, n, nullable);
                if (elemVar.IsNull)
                    WriteUtils.AppendNull(writer);
                else
                    elementType.map.Write(writer, ref elemVar, elementType);
            }
            writer.bytes.AppendChar(']');
        }
        

        public bool Read(JsonReader reader, ref Var slot, StubType stubType) {
            if (!ArrayUtils.StartArray(reader, ref slot, stubType, out bool startSuccess))
                return startSuccess;
            
            ref var parser = ref reader.parser;
            CollectionType collectionType = (CollectionType) stubType;
            T[] array = (T[]) slot.Obj;
            StubType elementType = collectionType.ElementType;
            if (array == null)
                array = (T[])Arrays.CreateInstance(elementType.type, ReadUtils.minLen);
            bool nullable = elementType.isNullable;

            int len = array.Length;
            int index = 0;
            Var elemVar = new Var();
            while (true) {
                JsonEvent ev = parser.NextEvent();
                switch (ev) {
                    case JsonEvent.ValueString:
                    case JsonEvent.ValueNumber:
                    case JsonEvent.ValueBool:
                        if (elementType.expectedEvent != ev)
                            return ReadUtils.ErrorIncompatible(reader, "array element", elementType, ref parser);
                        elemVar.Clear();
                        if (!elementType.map.Read(reader, ref elemVar, elementType))
                            return false;
                        if (index >= len)
                            array = Arrays.CopyOf(array, len = ReadUtils.Inc(len));
                        PrimitiveArray.SetArrayItem(array, ref elemVar, elemVarType, index++, nullable);
                        break;
                    case JsonEvent.ValueNull:
                        if (!nullable)
                            return ReadUtils.ErrorIncompatible(reader, "array element", elementType, ref parser);
                        PrimitiveArray.SetArrayItemNull(array, index++, elemVarType);
                        break;
                    case JsonEvent.ArrayStart:
                    case JsonEvent.ObjectStart:
                        elemVar.Clear();
                        if (!elementType.map.Read(reader, ref elemVar, elementType))
                            return false;
                        if (index >= len)
                            array = Arrays.CopyOf(array, len = ReadUtils.Inc(len));
                        PrimitiveArray.SetArrayItem(array, ref elemVar, elemVarType, index++,  nullable);
                        break;
                    case JsonEvent.ArrayEnd:
                        if (index != len)
                            array = Arrays.CopyOf(array, index);
                        slot.Obj = array;
                        return true;
                    case JsonEvent.Error:
                        return false;
                    default:
                        return ReadUtils.ErrorMsg(reader, "unexpected state: ", ev);
                }
            }
        }
    }
}