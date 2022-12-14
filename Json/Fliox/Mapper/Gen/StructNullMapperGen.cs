﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.Mapper.Map;
using Friflo.Json.Fliox.Mapper.Map.Object;
using Friflo.Json.Fliox.Mapper.Map.Object.Reflect;

namespace Friflo.Json.Fliox.Mapper.Gen
{
    internal sealed class StructNullMapperGen<T> : ClassMapper<T?> where T : struct {
        
        private readonly WriteDelegate<T>       write;
        private readonly ReadFieldDelegate<T>   readField;
        public  override bool                   IsNull(ref T? value) => !value.HasValue;

        internal StructNullMapperGen (
            StoreConfig             config,
            Type                    type,
            ConstructorInfo         constructor,
            InstanceFactory         instanceFactory,
            bool                    isValueType,
            Type                    genClass,
            MethodInfo              writeMethod,
            MethodInfo              readFieldMethod)
            : base (config, type, constructor, instanceFactory, isValueType, genClass)
        {
            write     = (WriteDelegate<T>)    Delegate.CreateDelegate(typeof(WriteDelegate<T>),     writeMethod);
            readField = (ReadFieldDelegate<T>)Delegate.CreateDelegate(typeof(ReadFieldDelegate<T>), readFieldMethod);
        }
        
        /// <see cref="ClassMapper{T}.Write"/>
        public override void Write(ref Writer writer, T? obj) {
            int startLevel = writer.IncLevel();
            
            bool firstMember    = true;
            if (!obj.HasValue) throw new InvalidOperationException($"expect Value of type: {type}");
            var objValue = obj.Value;
            write(ref objValue, propFields.fields, ref writer, ref firstMember);
            
            writer.WriteObjectEnd(firstMember);
            writer.DecLevel(startLevel);
        }
        
        /// <see cref="ClassMapper{T}.Read"/>
        public override T? Read(ref Reader reader, T? obj, out bool success)
        {
            // Ensure preconditions are fulfilled
            if (!reader.StartObject(this, out success))
                return default;
            if (!obj.HasValue) throw new InvalidOperationException($"expect Value of type: {type}");
            var objValue = obj.Value;
            
            var         ev      = reader.parser.NextEvent();
            Span<bool>  found   = reader.setMissingFields ? stackalloc bool [GetFoundCount()] : default;
            while (true) {
                switch (ev) {
                    case JsonEvent.ValueString:
                    case JsonEvent.ValueNumber:
                    case JsonEvent.ValueBool:
                    case JsonEvent.ArrayStart:
                    case JsonEvent.ObjectStart:
                    case JsonEvent.ValueNull:
                        PropField field;
                        if ((field = reader.GetField32(propFields)) == null)
                            break;
                        if (reader.setMissingFields) found[field.fieldIndex] = true;
                        success = readField(ref objValue, field, ref reader);
                        if (!success)
                            return default;
                        break;
                    case JsonEvent.ObjectEnd:
                        if (reader.setMissingFields) ClearReadToFields(obj, found);
                        success = true;
                        return objValue;
                    case JsonEvent.Error:
                        success = false;
                        return default;
                    default:
                        return reader.ErrorMsg<T>("unexpected state: ", ev, out success);
                }
                ev = reader.parser.NextEvent();
            }
        }
    }
}