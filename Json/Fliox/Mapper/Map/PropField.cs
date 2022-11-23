// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.Mapper.Map.Object;
using Friflo.Json.Fliox.Mapper.Map.Object.Reflect;

namespace Friflo.Json.Fliox.Mapper.Map
{
    /// <summary>
    /// May use generic type T in future to avoid casting object to T in <see cref="ClassMapper{T}"/> implementations. <br/>
    /// E.g. In calls <see cref="Var.Member.GetVar"/> in <see cref="ClassMapper{T}.Read"/>
    /// </summary>
    public sealed class PropField<T> : PropField // dont remove T - see docs
    {
        public PropField(string name, string jsonName, TypeMapper fieldType, FieldInfo field, PropertyInfo property, Var.Member member,
            int fieldIndex, int genIndex, bool required, string docs)
            : base(name, jsonName, fieldType, field, property, member, fieldIndex, genIndex, required, docs)
        {
        }
    }
        
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public abstract class PropField : IDisposable
    {
        public   readonly   string          name;
        public   readonly   JsonKey         key;
        public   readonly   string          jsonName;

        // ReSharper disable once UnassignedReadonlyField
        // field ist set via reflection to enable using a readonly field
        public   readonly   TypeMapper      fieldType;          // never null
        public   readonly   VarType         varType;            // never null
        public   readonly   Var             defaultValue;
        public   readonly   int             genIndex;
        public   readonly   int             fieldIndex;
        public   readonly   bool            required;
        public   readonly   string          docs;
        public   readonly   string          relation;
        internal            Bytes           nameBytes;          // don't mutate
        public              Bytes           firstMember;        // don't mutate
        public              Bytes           subSeqMember;       // don't mutate
        //
        private  readonly   FieldInfo                           field;
        private  readonly   PropertyInfo                        property;
        internal readonly   IEnumerable<CustomAttributeData>    customAttributes;
    //  private  readonly   MethodInfo                          getMethod;
    //  private  readonly   Func<object, object>                getLambda;
    //  private  readonly   Delegate                            getDelegate;
    //  private  readonly   MethodInfo                          setMethod;
    //  private  readonly   Action<object, object>              setLambda;
        internal readonly   Var.Member                          member;


        internal PropField (string name, string jsonName, TypeMapper fieldType, FieldInfo field, PropertyInfo property, Var.Member member,
            int fieldIndex, int genIndex, bool required, string docs)
        {
            this.name       = name;
            this.key        = new JsonKey(name);
            this.jsonName   = jsonName;
            this.fieldType  = fieldType;
            this.varType    = VarType.FromType(fieldType.type);
            defaultValue    = varType.DefaultValue;
            this.nameBytes  = new Bytes(jsonName,                   Untracked.Bytes);
            firstMember     = new Bytes($"{'{'}\"{jsonName}\":",    Untracked.Bytes);
            subSeqMember    = new Bytes($",\"{jsonName}\":",        Untracked.Bytes);
            //
            this.field      = field;
            this.property   = property;
            customAttributes= field != null ? field.CustomAttributes : property.CustomAttributes;
            // this.getMethod  = property != null ? property.GetGetMethod(true) : null;
            // this.setMethod  = property != null ? property.GetSetMethod(true) : null;
            /* if (property != null) {
                // var typeArray    = new [] {  property.DeclaringType, property.PropertyType  };
                // var delegateType = Expression.GetDelegateType(typeArray);
                // getDelegate      =  Delegate.CreateDelegate(delegateType, getMethod);
                var getLambdaExp    = DelegateUtils.CreateGetLambda<object,object>(property);
                var setLambdaExp    = DelegateUtils.CreateSetLambda<object,object>(property);
                getLambda           = getLambdaExp.Compile();
                setLambda           = setLambdaExp.Compile();
            } */
            this.member     = member;
            this.fieldIndex = fieldIndex;
            this.genIndex   = genIndex;
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
        
        internal static Var.Member CreateMember<T> (TypeMapper fieldType, FieldInfo field, PropertyInfo property) {
            if (field != null) {
                return new MemberField(fieldType.varType, field);
            }
            // Is struct?
            if (typeof(T).IsValueType) {
                return new MemberProperty(fieldType.varType, property);    
            }
            var getter          = property.GetGetMethod(true);
            var setter          = property.GetSetMethod(true);
            var memberMethods   = new Var.MemberMethods(getter, setter);
            var member          = fieldType.varType.CreateMember<T>(memberMethods);
            if (member != null)
                return member;
            // object (string, structs, classes) are using a generic MemberProperty  
            return new MemberProperty(fieldType.varType, property);
        }
    }
}
