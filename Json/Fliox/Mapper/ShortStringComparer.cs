// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Friflo.Json.Fliox.Mapper
{
    public sealed class ShortStringEqualityComparer : IEqualityComparer<ShortString>
    {
        public bool Equals(ShortString x, ShortString y) {
            return x.IsEqual(y);
        }

        public int GetHashCode(ShortString jsonKey) {
            return jsonKey.HashCode();
        }
    }
    
    public sealed class ShortStringComparer : IComparer<ShortString>
    {
        public int Compare(ShortString x, ShortString y) {
            return ShortString.StringCompare(x, y);
        }
    }
}