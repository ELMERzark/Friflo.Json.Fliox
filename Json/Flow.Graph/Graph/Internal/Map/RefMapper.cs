﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using Friflo.Json.Burst;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Mapper.Diff;
using Friflo.Json.Flow.Mapper.Map;
using Friflo.Json.Flow.Mapper.Map.Utils;
using Friflo.Json.Flow.Mapper.Map.Val;
using Friflo.Json.Flow.Mapper.Utils;

namespace Friflo.Json.Flow.Graph.Internal.Map
{
    // -------------------------------------------------------------------------------------
    public class RefMatcher : ITypeMatcher {
        public static readonly RefMatcher Instance = new RefMatcher();
        
        public TypeMapper MatchTypeMapper(Type type, StoreConfig config) {
            if (TypeUtils.IsStandardType(type)) // doesnt handle standard types
                return null;
            Type[] args = ReflectUtils.GetGenericInterfaceArgs (type, typeof(Ref<,>) );
            if (args == null)
                return null;
            
            Type refType = args[0];
            Type keyType = args[1];
            ConstructorInfo constructor = ReflectUtils.GetDefaultConstructor(type);
            
            object[] constructorParams = {config, type, constructor};
            // new RefMapper<T>(config, type, constructor);
            return (TypeMapper) TypeMapperUtils.CreateGenericInstance(typeof(RefMapper<,>), new[] {refType, keyType}, constructorParams);
        }
    }

    internal class RefMapper<T, TKey> : TypeMapper<Ref<T, TKey>> where T : class
    {
        private             TypeMapper<T>   entityMapper;
        private readonly    TypeMapper      stringMapper;
        
        public  override    string          DataTypeName()          { return "Ref<>"; }
        public  override    TypeMapper      GetUnderlyingMapper()   => stringMapper;
        public  override    TypeSemantic    GetTypeSemantic     ()  => TypeSemantic.Reference;

        // ReSharper disable once UnusedParameter.Local
        public RefMapper(StoreConfig config, Type type, ConstructorInfo constructor) :
            base(config, type, false, true)
        {
            stringMapper = StringMatcher.Instance.MatchTypeMapper(typeof(string), config);
        }

        private TypeMapper<T> GetEntityMapper(TypeCache typeCache) {
            if (entityMapper == null)
                entityMapper = (TypeMapper<T>)typeCache.GetTypeMapper(typeof(T));
            return entityMapper;
        }

        public override DiffNode Diff (Differ differ, Ref<T, TKey> left, Ref<T, TKey> right) {
            if (left.key != right.key) // todo use left.id.Equals(right.id) 
                return differ.AddNotEqual(left.id, right.id);
            return null;
        }
        
        public override void Trace(Tracer tracer, Ref<T, TKey> value) {
            string id = value.key;
            if (id == null)
                return;
            var store = tracer.tracerContext.Store();
            var set = store.GetEntitySet<T, TKey>();
            PeerEntity<T> peer = set.GetPeerByRef(value);
            if (peer.assigned)
                return;
            // Track untracked entity
            var entity = peer.NullableEntity;
            if (entity == null)
                return;  // todo add test
            if (set.syncSet.AddCreate(peer))
                store._intern.tracerLogTask.AddCreate(set.syncSet, peer.id);
            var mapper = GetEntityMapper(tracer.typeCache);
            mapper.Trace(tracer, entity);
        }

        public override void Write(ref Writer writer, Ref<T, TKey> value) {
            string id = value.key;
            if (id != null) {
                writer.WriteString(id);
            } else {
                writer.AppendNull();
            }
        }

        public override Ref<T, TKey> Read(ref Reader reader, Ref<T, TKey> slot, out bool success) {
            var ev = reader.parser.Event;
            if (ev == JsonEvent.ValueString) {
                success = true;
                string id = reader.parser.value.ToString();
                if (reader.tracerContext != null) {
                    var store = reader.tracerContext.Store();
                    var set = store.GetEntitySet<T, TKey>();
                    var peer = set.GetPeerById(id);
                    slot = new Ref<T, TKey> (peer);
                    return slot;
                }
                var key = Ref<T, TKey>.StaticEntityId.StringToKey(id);
                slot = new Ref<T, TKey> (key);
                return slot;
            }
            if (ev == JsonEvent.ValueNull) {
                success = true;
                return default;
            }
            return reader.HandleEvent(this, out success);
        }
    }
}