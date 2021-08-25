﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Flow.Mapper.Map.Key;

namespace Friflo.Json.Flow.Mapper.Map
{
    public class KeyMapper
    {
        internal static readonly Dictionary<Type, KeyMapper> KeyMappers  = new Dictionary<Type, KeyMapper>() {
            { typeof(string),   new StringKeyMapper() },
            { typeof(long),     new LongKeyMapper()   },
            { typeof(JsonKey),  new JsonKeyMapper()   },
        };
            
        public static KeyMapper GetKeyMapper<TKey>() {
            var keyType = typeof(TKey);
            if (KeyMappers.TryGetValue(keyType, out KeyMapper keyMapper))
                return keyMapper;
            throw new InvalidOperationException($"unsupported key Type: {keyType.FullName}");
        }
    }
    
    public abstract class KeyMapper<TKey> : KeyMapper
    {
        public abstract void        WriteKey       (ref Writer writer, in TKey key);
        public abstract TKey        ReadKey        (ref Reader reader, out bool success);
        public abstract JsonKey     ToJsonKey      (in TKey key);
        public abstract TKey        ToKey          (in JsonKey key);
    }
}