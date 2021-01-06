// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;

namespace Friflo.Json.Managed.Utils
{
    // HashMapOpen
    public class HashMapOpen <K,V> : FFMap<K,V>
    {
        private                 Object[]    key;
        private                 V[]         val;
        private                 int[]       used;
        private                 int         capacity;
        private                 int         size;
        private                 int         threshold;
        private static readonly Removed     RemovedKey = new Removed();
        private                 int         removes;
        private                 int         thresholdRemoves;
        private                 Object[]    rehashKey;
        private                 V[]         rehashVal;
        private                 int[]       rehashUsed;

        public HashMapOpen()
        :
            this (11) {
        }


        public HashMapOpen(int capacity)
        {
            key =   new Object  [ capacity ];
            val =   new V       [ capacity ];
            used =  new int     [capacity];
            //
            this.capacity = capacity;
            threshold = (int)(0.7 * capacity);
            thresholdRemoves    = (int)(0.15 * capacity);
        }

        public int Size()
        {
            return size;
        }

        public bool Remove (K k)
        {
            // does not support null key (same as Dictionary)
            int hash = k. GetHashCode() & 0x7FFFFFFF;
            int idx = hash % capacity;
            Object e = key[idx];
            while (e != null)
            {
                if (e. Equals( k ))
                {
                    key[idx] = RemovedKey;
                    val[idx] = default(V);
                    if (removes++ >= thresholdRemoves )
                        Rehash(capacity);
                    return true;
                }
                idx = (idx + 1) % capacity;
                e = key[idx];
            }
            return false;
        }

        public void Put(K k, V v)
        {
            // does not support null key (same as Dictionary)
            int hash = k. GetHashCode() & 0x7FFFFFFF;
            // NOTE: check for rehashing slow down performance by factor 2.5
            if (size >= threshold)
                Rehash ( 2 * capacity + 1);
            int idx = hash % capacity;
            Object e = key[idx];
            while (e != null)
            {
                if (e. Equals( k ))
                {
                    val[idx] = v;
                    return;
                }
                idx = (idx + 1) % capacity;
                e = key[idx];
            }
            key[idx] = k;
            val[idx] = v;
            used[size++] = idx;     
        }
    
        public V Get (K k)
        {
            // does not support null key (same as Dictionary)
            int hash = k. GetHashCode() & 0x7FFFFFFF;
            int idx = hash % capacity;
            Object e = key[idx];
            while (e != null)
            {
                if (e. Equals( k ))
                    return val[idx];
                idx = (idx + 1) % capacity;
                e = key[idx];           
            }
            return default(V);
        }
    
        public bool ContainsKey (K k)
        {
            // does not support null key (same as Dictionary)
            int hash = k. GetHashCode() & 0x7FFFFFFF;
            int idx = hash % capacity;
            Object e = key[idx];
            while (e != null)
            {
                if (e. Equals( k ))
                    return true;
                idx = (idx + 1) % capacity;
                e = key[idx];           
            }
            return false;
        }

        public void Rehash (int newCap)
        {
            capacity = newCap;
            threshold           = (int)(0.7 * capacity);
            thresholdRemoves    = (int)(0.15 * capacity);
            bool reuse = rehashKey != null && rehashKey. Length >= capacity;
            Object[]    newKey  = reuse ? rehashKey     : new Object    [ capacity ];
            // @SuppressWarnings("unchecked")
            V[]         newVal  = reuse ? rehashVal     : new V         [ capacity ];
            int[]       newUsed = reuse ? rehashUsed    :new int        [ capacity ];
            int         newSize = 0;
        
            for (int n = 0; n < size; n++)
            {
                int pos = used[n];
                Object k = key[pos];
                // ReSharper disable once PossibleUnintendedReferenceComparison
                if (k != RemovedKey)
                {
                    int hash = k. GetHashCode() & 0x7FFFFFFF;
                    int idx = hash % capacity;
                    Object e = newKey[idx];
                    while (e != null)
                    {
                        idx = (idx + 1) % capacity;
                        e = newKey[idx];
                    }
                    newKey[idx] = k;
                    newVal[idx] = val[pos];
                    newUsed[newSize++] = idx;
                }
                key[pos]= null;
                val[pos]= default(V);
            }
            // Array.Clear( key, 0, _key.Length);
            // Array.Clear( val, 0, _val.Length);
            rehashKey = key;
            rehashVal = val;
            rehashUsed = used;
            key = newKey;
            val = newVal;
            used = newUsed;
            size = newSize;
            removes = 0;
        }

        public void Clear()
        {
            for (int n = 0; n < size; n++)
                key[used[n]] = default(K);
            size = 0;
        }

        private class Removed
        {
            public override int GetHashCode()
            {
                return -1;
            }
            
            public override bool Equals (Object o)
            {
                return false;
            }
        }
    }
}