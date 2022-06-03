// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Reflection;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.Mapper.Utils;

namespace Friflo.Json.Fliox.Mapper.Map.Obj.Reflect
{
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public sealed class PropField : IDisposable
    {
        public   readonly   string          name;
        public   readonly   JsonKey         key;
        public   readonly   string          jsonName;

        // ReSharper disable once UnassignedReadonlyField
        // field ist set via reflection to enable using a readonly field
        public   readonly   TypeMapper      fieldType;          // never null
        public   readonly   int             primIndex;
        public   readonly   int             objIndex;
        public   readonly   bool            required;
        public   readonly   string          docs;
        public   readonly   bool            isKey;
        public   readonly   string          relation;
        internal            Bytes           nameBytes;          // don't mutate
        public              Bytes           firstMember;        // don't mutate
        public              Bytes           subSeqMember;       // don't mutate
        //
        internal readonly   FieldInfo                           field;
        internal readonly   PropertyInfo                        property;
        private  readonly   IEnumerable<CustomAttributeData>    customAttributes;
        private  readonly   MethodInfo                          getMethod;
        private  readonly   Func<object, object>                getLambda;
        // private  readonly   Delegate                         getDelegate;
        private  readonly   MethodInfo                          setMethod;
        private  readonly   Action<object, object>              setLambda;

        internal PropField (string name, string jsonName, TypeMapper fieldType, FieldInfo field, PropertyInfo property,
            int primIndex, int objIndex, bool required, string docs)
        {
            this.name       = name;
            this.key        = new JsonKey(name);
            this.jsonName   = jsonName;
            this.fieldType  = fieldType;
            this.nameBytes  = new Bytes(jsonName,                   Untracked.Bytes);
            firstMember     = new Bytes($"{'{'}\"{jsonName}\":",    Untracked.Bytes);
            subSeqMember    = new Bytes($",\"{jsonName}\":",        Untracked.Bytes);
            //
            this.field      = field;
            this.property   = property;
            customAttributes= field != null ? field.CustomAttributes : property.CustomAttributes;
            this.getMethod  = property != null ? property.GetGetMethod(true) : null;
            this.setMethod  = property != null ? property.GetSetMethod(true) : null;
            if (property != null) {
                // var typeArray    = new [] {  property.DeclaringType, property.PropertyType  };
                // var delegateType = Expression.GetDelegateType(typeArray);
                // getDelegate      =  Delegate.CreateDelegate(delegateType, getMethod);
                var getLambdaExp    = DelegateUtils.CreateGetLambda<object,object>(property);
                var setLambdaExp    = DelegateUtils.CreateSetLambda<object,object>(property);
                getLambda           = getLambdaExp.Compile();
                setLambda           = setLambdaExp.Compile();
                isKey = FieldQuery.IsKey(customAttributes);
            } else {
                isKey = FieldQuery.IsKey(customAttributes);
            }
            this.primIndex  = primIndex;
            this.objIndex   = objIndex;
            this.required   = required;
            this.docs       = docs;
            this.relation   = GetRelationAttributeType();
        }
        
        public MemberInfo   Member { get {
            if (field != null)
                return field;
            return property;
        } }

        public void Dispose() {
            subSeqMember.Dispose(Untracked.Bytes);
            firstMember.Dispose(Untracked.Bytes);
            nameBytes.Dispose(Untracked.Bytes);
        }
        
        private static readonly bool useDirect = false; // Unity: System.NotImplementedException : GetValueDirect
        
        /// <paramref name="setMethodParams"/> need to be of Length 1
        public void SetField (object obj, object value, object[] setMethodParams)
        {
            if (field != null) {
                if (useDirect)
                    field.SetValueDirect(__makeref(obj), value);
                else
                    field.SetValue(obj, value); // todo use Expression
            } else {
                if (setLambda != null) {
                    setLambda(obj, value);
                } else {
                    setMethodParams[0] = value;
                    setMethod.Invoke(obj, setMethodParams);
                }
            }
        }
        
        // ReSharper disable PossibleNullReferenceException
        public object GetField (object obj)
        {
            if (field != null) {
                if (useDirect)
                    return field.GetValueDirect(__makeref(obj));
                return field.GetValue (obj); // todo use Expression
            }
            if (getLambda != null) {
                return getLambda(obj);
            }
            return getMethod.Invoke(obj, null);
        }

        public override string ToString() {
            return name;
        }
        
        private string GetRelationAttributeType() {
            foreach (var attr in customAttributes) {
                if (attr.AttributeType == typeof(RelationAttribute))
                    return (string)attr.ConstructorArguments[0].Value;
            }
            return null;
        }
    }
}
