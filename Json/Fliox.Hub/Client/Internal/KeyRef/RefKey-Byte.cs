﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.


namespace Friflo.Json.Fliox.Hub.Client.Internal.KeyRef
{
    internal sealed class RefKeyByte<T> : RefKey<byte, T> where T : class
    {
        internal override byte IdToKey(in JsonKey id) {
            return (byte)id.AsLong();
        }

        internal override JsonKey KeyToId(in byte key) {
            return new JsonKey(key);
        }
    }
    
    internal sealed class RefKeyByteNull<T> : RefKey<byte?, T> where T : class
    {
        internal override   bool                IsKeyNull (byte? key)       => key == null;
        
        internal override byte? IdToKey(in JsonKey id) {
            return (byte)id.AsLong();
        }

        internal override JsonKey KeyToId(in byte? key) {
            return new JsonKey(key);
        }
    }
}