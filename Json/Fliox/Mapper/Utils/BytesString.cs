﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using Friflo.Json.Burst;

namespace Friflo.Json.Fliox.Mapper.Utils
{
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public sealed class BytesString
    {
        public Bytes value;
        
        public BytesString() {
        }
        
        public BytesString(ref Bytes str) {
            value = new Bytes(ref str);
        }

        public BytesString(string str) {
            value = new Bytes(str, Untracked.Bytes);
        }

        public override bool Equals(object obj) {
            if (obj == null)
                return false;
            if (obj is BytesString other)
                return value.IsEqualBytes(ref other.value);
            return false;
        }

        public override int GetHashCode() {
            return value.GetHashCode();
        }

        public override string ToString() {
            return value.AsString();
        }
    }
}