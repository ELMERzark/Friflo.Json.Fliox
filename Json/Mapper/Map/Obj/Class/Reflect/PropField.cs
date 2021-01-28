// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using Friflo.Json.Burst;

namespace Friflo.Json.Mapper.Map.Obj.Class.Reflect
{
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class PropField : IDisposable
    {
        internal readonly   String              name;

        // ReSharper disable once UnassignedReadonlyField
        // field ist set via reflection to enable using a readonly field
        public   readonly   TypeMapper      fieldType;          // never null
        public   readonly   bool            isValueType;
        public   readonly   int             fieldIndex;
        internal readonly   Type            fieldTypeNative;    // never null 
        internal            Bytes           nameBytes;          // dont mutate
        //
        internal readonly   FieldInfo       field;
        private  readonly   PropertyInfo    getter;
        private  readonly   PropertyInfo    setter;

        internal PropField (String name, Type fieldType, FieldInfo field, PropertyInfo getter, PropertyInfo setter, int fieldIndex)
        {
            this.name               = name;
            this.nameBytes          = new Bytes(name);
            this.fieldTypeNative    = fieldType;
            this.isValueType        = fieldType.IsValueType;
            //
            this.field              = field;
            this.getter             = getter;
            this.setter             = setter;
            this.fieldIndex         = fieldIndex;
            if (fieldType == null)
                throw new InvalidOperationException("Expect fieldType non null");
        }

        public void Dispose() {
            nameBytes.Dispose();
        }

        public void AppendName(ref Bytes bb)
        {
            bb.AppendBytes(ref nameBytes);
        }
        
        private static readonly bool useDirect = false; // Unity: System.NotImplementedException : GetValueDirect
        
        public void SetField (object obj, object value)
        {
            if (field != null) {
                if (useDirect)
                    field.SetValueDirect(__makeref(obj), value);
                else
                    field.SetValue(obj, value);
            } else {
                getter.SetValue(obj, value);
            }
        }
        
        // ReSharper disable PossibleNullReferenceException
        public object GetField (object obj)
        {
            if (field != null) {
                if (useDirect)
                    return field.GetValueDirect(__makeref(obj));
                return field.GetValue (obj);
            }
            return setter.GetValue(obj);
        }
    }
}
