﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.DB.Graph.Internal.IdRef
{
    internal class RefKeyShortField<T> : RefKey<short, T> where T : class {
        private  readonly   FieldInfo           field;
        private  readonly   Func  <T, short>    fieldGet;
        private  readonly   Action<T, short>    fieldSet;
        
        internal override   Type                GetKeyType() => typeof(short);
        internal override   string              GetKeyName() => field.Name;

        internal RefKeyShortField(FieldInfo field) {
            this.field  = field;
            fieldGet    = GetFieldGet<T, short>(field);
            fieldSet    = GetFieldSet<T, short>(field);
        }

        internal override short IdToKey(in JsonKey id) {
            return (short)id.AsLong();
        }

        internal override JsonKey KeyToId(in short key) {
            return new JsonKey(key);
        }
        
        internal override   short  GetKey (T entity) {
            return fieldGet(entity);
        }
        
        internal override   void    SetKey (T entity, short id) {
            fieldSet(entity, id);
        }
    }
    
    
    internal class RefKeyShortProperty<T> : RefKey<short, T> where T : class {
        private  readonly   PropertyInfo        property;
        private  readonly   Func  <T, short>    propertyGet;
        private  readonly   Action<T, short>    propertySet;
        
        internal override   Type                GetKeyType() => typeof(short);
        internal override   string              GetKeyName() => property.Name;

        internal RefKeyShortProperty(PropertyInfo property, MethodInfo idGetMethod, MethodInfo idSetMethod) {
            this.property = property;
            propertyGet = (Func  <T, short>) Delegate.CreateDelegate (typeof(Func  <T, short>), idGetMethod);
            propertySet = (Action<T, short>) Delegate.CreateDelegate (typeof(Action<T, short>), idSetMethod);
        }

        internal override short IdToKey(in JsonKey id) {
            return (short)id.AsLong();
        }

        internal override JsonKey KeyToId(in short key) {
            return new JsonKey(key);
        }
        
        internal override   short  GetKey (T entity) {
            return propertyGet(entity);
        }
        
        internal override   void    SetKey (T entity, short id) {
            propertySet(entity, id);
        }
    }
}