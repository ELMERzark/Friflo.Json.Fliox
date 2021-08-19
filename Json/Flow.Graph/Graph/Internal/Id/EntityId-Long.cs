﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Reflection;

namespace Friflo.Json.Flow.Graph.Internal.Id
{
    internal class EntityIdLongField<T> : EntityId<T, long> where T : class {
        private  readonly   FieldInfo           field;
        private  readonly   Func  <T, long>     fieldGet;
        private  readonly   Action<T, long>     fieldSet;
        
        internal override   Type                GetEntityIdType () => typeof(long);
        
        internal EntityIdLongField(FieldInfo field) {
            this.field  = field;
            fieldGet    = GetFieldGet<T, long>(field);
            fieldSet    = GetFieldSet<T, long>(field);
        }

        internal override long StringToKey(string id) {
            return long.Parse(id);
        }

        internal override string KeyToString(long id) {
            return id.ToString();
        }
        
        internal override   long  GetId (T entity) {
            return fieldGet(entity);
        }
        
        internal override   void    SetId (T entity, long id) {
            fieldSet(entity, id);
        }
    }
    
    
    internal class EntityIdLongProperty<T> : EntityId<T, long> where T : class {
        private  readonly   Func  <T, long>     propertyGet;
        private  readonly   Action<T, long>     propertySet;
        
        internal override   Type                GetEntityIdType () => typeof(long);
        
        internal EntityIdLongProperty(MethodInfo idGetMethod, MethodInfo idSetMethod) {
            propertyGet = (Func  <T, long>) Delegate.CreateDelegate (typeof(Func  <T, long>), idGetMethod);
            propertySet = (Action<T, long>) Delegate.CreateDelegate (typeof(Action<T, long>), idSetMethod);
        }

        internal override long StringToKey(string id) {
            return long.Parse(id);
        }

        internal override string KeyToString(long id) {
            return id.ToString();
        }
        
        internal override   long  GetId (T entity) {
            return propertyGet(entity);
        }
        
        internal override   void    SetId (T entity, long id) {
            propertySet(entity, id);
        }
    }
}