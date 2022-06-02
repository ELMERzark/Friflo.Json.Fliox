﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

namespace Friflo.Json.Fliox.Hub.Client.Internal.Key
{
    internal sealed class RefKeyLong : RefKey<long>
    {
        internal override long IdToKey(in JsonKey id) {
            return id.AsLong();
        }

        internal override JsonKey KeyToId(in long key) {
            return new JsonKey(key);
        }
    }
    
    internal sealed class RefKeyLongNull : RefKey<long?>
    {
        internal override   bool                IsKeyNull (long? key)       => key == null;
        
        internal override long? IdToKey(in JsonKey id) {
            return id.AsLong();
        }

        internal override JsonKey KeyToId(in long? key) {
            return new JsonKey(key);
        }
    }
}