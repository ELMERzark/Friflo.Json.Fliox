﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.Hub.Client.Internal.KeyRef
{
    internal sealed class RefKeyShort<T> : RefKey<short, T> where T : class
    {
        internal override short IdToKey(in JsonKey id) {
            return (short)id.AsLong();
        }

        internal override JsonKey KeyToId(in short key) {
            return new JsonKey(key);
        }
    }
    
    internal sealed class RefKeyShortNull<T> : RefKey<short?, T> where T : class
    {
        internal override   bool                IsKeyNull (short? key)       => key == null;
        
        internal override short? IdToKey(in JsonKey id) {
            return (short)id.AsLong();
        }

        internal override JsonKey KeyToId(in short? key) {
            return new JsonKey(key);
        }
    }
}