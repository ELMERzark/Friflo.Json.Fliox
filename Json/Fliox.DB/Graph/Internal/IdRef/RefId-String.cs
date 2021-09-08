﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.DB.Graph.Internal.IdRef
{
    internal class RefKeyStringField<T> : RefKey<string, T> where T : class {
        private  readonly   FieldInfo           field;
        private  readonly   Func  <T, string>   fieldGet;
        private  readonly   Action<T, string>   fieldSet;
        
        internal override   Type                GetKeyType()                => typeof(string);
        internal override   string              GetKeyName()                => field.Name;
        internal override   bool                IsKeyNull (string key)      => key == null;

        internal RefKeyStringField(FieldInfo field) {
            this.field  = field;
            fieldGet    = GetFieldGet<T, string>(field);
            fieldSet    = GetFieldSet<T, string>(field);
        }

        internal override string IdToKey(in JsonKey id) {
            return id.AsString();
        }

        internal override JsonKey KeyToId(in string key) {
            return new JsonKey(key);
        }
        
        internal override   string  GetKey (T entity) {
            return fieldGet(entity);
        }
        
        internal override   void    SetKey (T entity, string id) {
            fieldSet(entity, id);
        }
    }
    

    internal class RefKeyStringProperty<T> : RefKey<string, T> where T : class {
        private  readonly   PropertyInfo        property;
        private  readonly   Func  <T, string>   propertyGet;
        private  readonly   Action<T, string>   propertySet;
        
        internal override   Type                GetKeyType()                => typeof(string);
        internal override   string              GetKeyName()                => property.Name;
        internal override   bool                IsKeyNull (string key)      => key == null;

        internal RefKeyStringProperty(PropertyInfo property, MethodInfo idGetMethod, MethodInfo idSetMethod) {
            this.property = property;
            propertyGet = (Func  <T, string>) Delegate.CreateDelegate (typeof(Func<T, string>),   idGetMethod);
            propertySet = (Action<T, string>) Delegate.CreateDelegate (typeof(Action<T, string>), idSetMethod);
        }

        internal override string IdToKey(in JsonKey id) {
            return id.AsString();
        }

        internal override JsonKey KeyToId(in string key) {
            return new JsonKey(key);
        }
        
        internal override   string  GetKey (T entity) {
            return propertyGet(entity);
        }
        
        internal override   void    SetKey (T entity, string id) {
            propertySet(entity, id);
        }
    }
}