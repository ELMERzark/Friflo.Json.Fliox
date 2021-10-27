﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.Hub.Client.Internal.KeyRef
{
    internal sealed class RefKeyInt<T> : RefKey<int, T> where T : class
    {
        internal override int IdToKey(in JsonKey id) {
            return (int)id.AsLong();
        }

        internal override JsonKey KeyToId(in int key) {
            return new JsonKey(key);
        }
    }
    
    internal sealed class RefKeyIntNull<T> : RefKey<int?, T> where T : class
    {
        internal override   bool                IsKeyNull (int? key)       => key == null;
        
        internal override int? IdToKey(in JsonKey id) {
            return (int)id.AsLong();
        }

        internal override JsonKey KeyToId(in int? key) {
            return new JsonKey(key);
        }
    }
}