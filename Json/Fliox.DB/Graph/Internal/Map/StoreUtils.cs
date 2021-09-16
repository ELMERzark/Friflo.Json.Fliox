﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Friflo.Json.Fliox.DB.Graph.Internal.Map
{
    internal static class StoreUtils
    {
        private static readonly Dictionary<Type, EntityInfo[]> EntityInfoCache = new Dictionary<Type, EntityInfo[]>();
        
        private static EntityInfo[] GetEntityInfos(Type type) {
            if (EntityInfoCache.TryGetValue(type, out  EntityInfo[] result)) {
                return result;
            }
            var entityInfos = new List<EntityInfo>();
            var flags       = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            PropertyInfo[] properties = type.GetProperties(flags);
            for (int n = 0; n < properties.Length; n++) {
                var  property       = properties[n];
                Type propType       = property.PropertyType;
                bool isEntitySet    = IsEntitySet(propType);
                if (!isEntitySet)
                    continue;
                var genericArgs = propType.GetGenericArguments();
                var info        = new EntityInfo (propType, genericArgs[1], null, property );
                entityInfos.Add(info);
            }
            FieldInfo[] fields = type.GetFields(flags);
            for (int n = 0; n < fields.Length; n++) {
                var  field          = fields[n];
                Type fieldType      = field.FieldType;
                bool isEntitySet    = IsEntitySet(fieldType);
                if (!isEntitySet || IsAutoGeneratedBackingField(field))
                    continue;
                var genericArgs = fieldType.GetGenericArguments();
                var info        = new EntityInfo (fieldType, genericArgs[1], field, null);
                entityInfos.Add(info);
            }
            result = entityInfos.ToArray();
            EntityInfoCache.Add(type, result);
            return result;
        }
        
        internal static Type[] GetEntityTypes<TEntityStore>() where TEntityStore : EntityStore {
            var entityInfos = GetEntityInfos (typeof(TEntityStore));
            var types       = new Type[entityInfos.Length];
            for (int n = 0; n < entityInfos.Length; n++) {
                types[n] = entityInfos[n].entityType;
            }
            return  types;
        }

        internal static void InitEntitySets(EntityStore store) {
            var entityInfos = GetEntityInfos (store.GetType());
            foreach (var entityInfo in entityInfos) {
                var setMapper   = (IEntitySetMapper)store._intern.typeStore.GetTypeMapper(entityInfo.entitySetType);
                var entitySet   = setMapper.CreateEntitySet();
                entitySet.Init(store);
                if (entityInfo.field != null) {
                    entityInfo.field.   SetValue(store, entitySet);
                } else {
                    entityInfo.property.SetValue(store, entitySet);
                }
            }
        }
        
        internal static bool IsEntitySet (Type type) {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(EntitySet<,>);
        }
        
        private static bool IsAutoGeneratedBackingField(FieldInfo field) {
            foreach (CustomAttributeData attr in field.CustomAttributes) {
                if (attr.AttributeType == typeof(CompilerGeneratedAttribute))
                    return true;
            }
            return false;
        }
    }
    
    internal readonly struct EntityInfo {
        internal readonly   Type           entitySetType;
        internal readonly   Type           entityType;
        internal readonly   FieldInfo      field;
        internal readonly   PropertyInfo   property;
        
        internal EntityInfo (
            Type           entitySetType,
            Type           entityType,
            FieldInfo      field,
            PropertyInfo   property)
        {
            this.entitySetType  = entitySetType;
            this.entityType     = entityType;
            this.field          = field;
            this.property       = property;
        }
    }
}