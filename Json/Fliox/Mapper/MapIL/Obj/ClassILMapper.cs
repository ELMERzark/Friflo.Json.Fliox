﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Reflection;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.Mapper.Map;
using Friflo.Json.Fliox.Mapper.Map.Object;
using Friflo.Json.Fliox.Mapper.Map.Object.Reflect;

#if !UNITY_5_3_OR_NEWER

namespace Friflo.Json.Fliox.Mapper.MapIL.Obj
{
   
    internal class ClassILMapper<T> : ClassMapper<T> {
        
        public ClassILMapper (StoreConfig config, Type type, ConstructorInfo constructor, InstanceFactory instanceFactory, bool isValueType) :
            base (config, type, constructor, instanceFactory, isValueType)
        {
        }

        public override void InitTypeMapper(TypeStore typeStore) {
            base.InitTypeMapper(typeStore);
            layout = new ClassLayout<T>(this, typeStore.config);
        }
        
        internal override bool IsValueNullIL(ClassMirror mirror, int primPos, int objPos) {
            return mirror.LoadObj(objPos) == null;
        }
        
        internal override void WriteValueIL(ref Writer writer, ClassMirror mirror, int primPos, int objPos) {
            object obj = mirror.LoadObj(objPos);
#if DEBUG
            if (obj == null)
                throw new InvalidOperationException("Expect non null object. Type: " + typeof(T));
#endif
            Write(ref writer, (T) obj);
        }

        internal override bool ReadValueIL(ref Reader reader, ClassMirror mirror, int primPos, int objPos) {
            T src = (T) mirror.LoadObj(objPos);
            T value = Read(ref reader, src, out bool success);
            mirror.StoreObj(objPos, value);
            return success;
        }
        
        // ----------------------------------- Write / Read -----------------------------------
        public override void Write(ref Writer writer, T slot) {
            int startLevel = writer.IncLevel();
            T obj = slot;
            TypeMapper classMapper = this;
            bool firstMember = true;
            
            ClassMirror mirror = writer.InstanceLoad(this, ref classMapper, ref obj);

            if (this != classMapper)
                writer.WriteDiscriminator(this, classMapper, ref firstMember);
            
            PropField[] fields = classMapper.PropFields.fields;
            for (int n = 0; n < fields.Length; n++) {
                PropField field = fields[n];
                var fieldType   = field.fieldType;
                // check for JSON value: null is done in WriteValueIL() struct's requires different handling than reference types
                if (fieldType.isValueType) {
                    if (fieldType.IsValueNullIL(mirror, field.primIndex, field.objIndex)) {
                        if (writer.writeNullMembers) {
                            writer.WriteFieldKey(field, ref firstMember);
                            writer.AppendNull();  
                        }
                    } else {
                        writer.WriteFieldKey(field, ref firstMember);
                        fieldType.WriteValueIL(ref writer, mirror, field.primIndex, field.objIndex);
                    }
                } else {
                    object fieldObj = mirror.LoadObj(field.objIndex);
                    bool isNull     = fieldType.IsNullObject(fieldObj);
                    if (isNull) {
                        if (writer.writeNullMembers) {
                            writer.WriteFieldKey(field, ref firstMember);
                            writer.AppendNull();
                        }
                    } else {
                        writer.WriteFieldKey(field, ref firstMember);
                        fieldType.WriteObject(ref writer, fieldObj);
                    }
                    writer.FlushFilledBuffer();
                }
            }
            writer.InstancePop();
            writer.WriteObjectEnd(firstMember);

            writer.DecLevel(startLevel);
        }

        public override T Read(ref Reader reader, T slot, out bool success) {
            // Ensure preconditions are fulfilled
            if (!reader.StartObject(this, out success))
                return default;

            var subType = GetPolymorphType(ref reader, this, ref slot, out success);
            if (!success)
                return default;
            if (subType != null) {
                return (T)subType.ReadObjectTyped(ref reader, slot, out success);
            }
            return (T)ReadObjectTyped(ref reader, slot, out success);
        }
            
        internal override object ReadObjectTyped(ref Reader reader, object slot, out bool success)
        {
            var objRef = (T)slot;
            TypeMapper classType = this;
            ClassMirror mirror = reader.InstanceLoad(this, ref classType, ref objRef);
            if (!ReadClassMirror(ref reader, mirror, classType, 0, 0)) {
                success = false;
                return default;
            }
            reader.InstanceStore(mirror, ref objRef);
            success = true;
            return objRef;
        }

        internal static bool ReadClassMirror(ref Reader reader, ClassMirror mirror, TypeMapper classType, int primPos, int objPos) {
            JsonEvent ev    = reader.parser.Event;
            var propFields  = classType.PropFields;

            while (true) {
                bool success;
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
                        TypeMapper fieldType = field.fieldType;
                        if (fieldType.isValueType) {
                            if (!fieldType.ReadValueIL(ref reader, mirror, primPos + field.primIndex, objPos + field.objIndex))
                                return default;
                        } else {
                            object fieldVal = mirror.LoadObj(objPos + field.objIndex);
                            Var fieldVar = new Var(fieldVal);
                            fieldVar = fieldType.ReadVar(ref reader, fieldVar, out success);
                            if (!success)
                                return false;
                            fieldVal = fieldVar.Object;
                            mirror.StoreObj(objPos + field.objIndex, fieldVal);
                            if (!fieldType.isNullable && fieldVal == null)
                                return reader.ErrorIncompatible<bool>(classType, field, out success);
                        }
                        break;
                    case JsonEvent.ObjectEnd:
                        return true;
                    case JsonEvent.Error:
                        return false;
                    default:
                        return reader.ErrorMsg<bool>("unexpected state: ", ev, out success);
                }
                ev = reader.parser.NextEvent();
            }
        }

    }
}

#endif
