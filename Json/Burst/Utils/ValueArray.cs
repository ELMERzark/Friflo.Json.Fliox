﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;

namespace Friflo.Json.Burst.Utils
{
    // JSON_BURST_TAG
    public struct ValueArray<T> : IDisposable where T : struct
    {
#if JSON_BURST
        public Unity.Collections.NativeArray<T> array;

        public ValueArray(int size) {
            array = new Unity.Collections.NativeArray<T>(size, Unity.Collections.Allocator.Persistent);
        }
        
        public int Length {
            get { return array.Length; }
        }
        
        public void Dispose() {
            array.Dispose();
        }

        public bool IsCreated() {
            return array.IsCreated;
        }
        
#else // MANAGED

        public T[] array;

        public ValueArray(int size) {
            array = new T[size];
            DebugUtils.TrackAllocationObsolete(array);
        }
        
        public int Length {
            get { return array.Length; }
        }

        public void Dispose() {
            if (array == null)
                throw new InvalidOperationException("Friflo.Json.Burst.Utils.ValueArray has been disposed. Mimic NativeArray behavior");
            DebugUtils.UntrackAllocationObsolete(array);
            array = null;
        }
        
        public bool IsCreated() {
            return array != null;
        }
#endif
    }
}