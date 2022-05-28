﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Host.Utils;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Schema.Validation;
using Friflo.Json.Fliox.Transform;
using Friflo.Json.Fliox.Utils;

namespace Friflo.Json.Fliox.Hub.Host
{
    /// <summary>
    /// <see cref="Pool"/> is a set of pooled instances of various <see cref="Type"/>'s.
    /// To enable pooling instances of a specific class it needs to implement <see cref="IDisposable"/>.
    /// Pool for classes used commonly within <see cref="Host"/> are directly available. E.g. <see cref="ObjectMapper"/>.
    /// Custom types can also be managed by <see cref="Pool"/> by using <see cref="Type{T}"/>.
    /// Its typical use case is pooling a domain specific <see cref="Client.FlioxClient"/> implementation. 
    /// </summary>
    internal sealed class Pool
    {
        // Note: Pool does not expose sharedEnv.TypeStore by intention to avoid side effects by unexpected usage. 
        private   readonly  Dictionary<Type, IDisposable>   poolMap = new Dictionary<Type, IDisposable>(); // object = SharedPool<T>

        public  ObjectPool<JsonPatcher>     JsonPatcher     { get; }
        public  ObjectPool<ScalarSelector>  ScalarSelector  { get; }
        public  ObjectPool<JsonEvaluator>   JsonEvaluator   { get; }
        /// <summary> Returned <see cref="Mapper.ObjectMapper"/> doesnt throw Read() exceptions. To handle errors its
        /// <see cref="Mapper.ObjectMapper.reader"/> -> <see cref="ObjectReader.Error"/> need to be checked. </summary>
        public  ObjectPool<ObjectMapper>    ObjectMapper    { get; }
        public  ObjectPool<EntityProcessor> EntityProcessor { get; }
        public  ObjectPool<TypeValidator>   TypeValidator   { get; }
        /// <summary>
        /// Enable pooling instances of the given Type <typeparamref name="T"/>. In case no cached instance of <typeparamref name="T"/>
        /// is available the <paramref name="factory"/> method is called to create a new instance.
        /// After returning a pooled instance to its pool with <see cref="ObjectPool{T}.Return"/> it is cached and
        /// will be reused when calling <see cref="ObjectPool{T}.Get"/> anytime later.
        /// To ensure pooled instances are not leaking use the using directive. E.g.
        /// <code>
        /// using (var pooledMapper = syncContext.pool.ObjectMapper.Get()) {
        ///     ...
        /// }
        /// </code>
        /// </summary>
        public  ObjectPool<T>               Type<T>         (Func<T> factory) where T : IDisposable {
            if (poolMap.TryGetValue(typeof(T), out var pooled)) {
                return (ObjectPool<T>)pooled;
            }
            var pool = new ObjectPool<T>(factory);
            poolMap[typeof(T)] = pool;
            return pool;
        }

        internal Pool(SharedEnv sharedEnv) {
            JsonPatcher     = new ObjectPool<JsonPatcher>       (() => new JsonPatcher());
            ScalarSelector  = new ObjectPool<ScalarSelector>    (() => new ScalarSelector());
            JsonEvaluator   = new ObjectPool<JsonEvaluator>     (() => new JsonEvaluator());
            ObjectMapper    = new ObjectPool<ObjectMapper>      (() => new ObjectMapper(sharedEnv.TypeStore),  m => m.ErrorHandler = ObjectReader.NoThrow);
            EntityProcessor = new ObjectPool<EntityProcessor>   (() => new EntityProcessor());
            TypeValidator   = new ObjectPool<TypeValidator>     (() => new TypeValidator());
        }
        
        public void Dispose() {
            JsonPatcher.    Dispose();
            ScalarSelector. Dispose();
            JsonEvaluator.  Dispose();
            ObjectMapper.   Dispose();
            EntityProcessor.Dispose();
            TypeValidator.  Dispose();
            foreach (var pair in poolMap) {
                var pool = pair.Value;
                pool.Dispose();
            }
            poolMap.Clear();
        }

        public PoolUsage PoolUsage => new PoolUsage {
            jsonPatcher     = JsonPatcher       .Count,
            scalarSelector  = ScalarSelector    .Count,
            jsonEvaluator   = JsonEvaluator     .Count,
            objectMapper    = ObjectMapper      .Count,
            entityProcessor = EntityProcessor   .Count,
            typeValidator   = TypeValidator     .Count
        };
    }
    
    public struct PoolUsage {
        internal int    jsonPatcher;
        internal int    scalarSelector;
        internal int    jsonEvaluator;
        internal int    objectMapper;
        internal int    entityProcessor;
        internal int    typeValidator;

        public override string ToString() =>
            $"jsonPatcher: {jsonPatcher}, scalarSelector: {scalarSelector}, jsonEvaluator: {jsonEvaluator}, " +
            $"objectMapper: {objectMapper}, entityProcessor: {entityProcessor}, typeValidator: {typeValidator}";
    }
}