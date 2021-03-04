﻿using System;
using System.Reflection;
using Friflo.Json.Burst;
using Friflo.Json.Mapper.Map;
using Friflo.Json.Mapper.Map.Utils;
using Friflo.Json.Mapper.Utils;

namespace Friflo.Json.Mapper.ER
{
    // -------------------------------------------------------------------------------------
    public class RefMatcher : ITypeMatcher {
        public static readonly RefMatcher Instance = new RefMatcher();
        
        public TypeMapper MatchTypeMapper(Type type, StoreConfig config) {
            if (TypeUtils.IsStandardType(type)) // dont handle standard types
                return null;
            Type[] args = ReflectUtils.GetGenericInterfaceArgs (type, typeof(Ref<>) );
            if (args == null)
                return null;
            
            Type refType = args[0];
            ConstructorInfo constructor = ReflectUtils.GetDefaultConstructor(type);
            
            object[] constructorParams = {config, type, constructor};
            // new RefMapper<T>(config, type, constructor);
            return (TypeMapper) TypeMapperUtils.CreateGenericInstance(typeof(RefMapper<>), new[] {refType}, constructorParams);
        }
    }

    public class RefMapper<T> : TypeMapper<Ref<T>> where T : Entity
    {
        private TypeMapper entityMapper;
        
        public override string DataTypeName() { return "Ref<>"; }

        public RefMapper(StoreConfig config, Type type, ConstructorInfo constructor) :
            base(config, type, true, true)
        {
        }

        public override void Write(ref Writer writer, Ref<T> value) {
            string id = value.Id;
            if (id != null)
                writer.WriteString(id);
            else
                writer.AppendNull();
        }

        public override Ref<T> Read(ref Reader reader, Ref<T> slot, out bool success) {
            if (reader.parser.Event == JsonEvent.ValueString) {
                success = true;
                string id = reader.parser.value.ToString();
                var container = reader.entityStore.GetContainer(typeof(T));
                var entity = (T)container.GetEntity(id);
                slot = new Ref<T>();
                if (entity != null)
                    slot.Entity = entity;
                else
                    slot.Id = id;
                return slot;
            }
            success = false;
            return null;
        }
    }
}