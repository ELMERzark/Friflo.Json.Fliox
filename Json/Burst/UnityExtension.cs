﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System.Collections.Generic;

// ReSharper disable CheckNamespace
namespace System.Collections.Generic
{
    public static class Helper
    {
        public static HashSet<T> CreateHashSet<T>(int capacity) {
#if UNITY_5_3_OR_NEWER
            return new HashSet<T>();
#else
            return new HashSet<T>(capacity);
#endif
        }
        
        public static HashSet<T> CreateHashSet<T>(int capacity, IEqualityComparer<T> comparer) {
#if UNITY_5_3_OR_NEWER
            return new HashSet<T>(comparer);
#else
            return new HashSet<T>(capacity, comparer);
#endif
        }
        
        public static T First<T>(this HashSet<T> hashSet) {
            // ReSharper disable once GenericEnumeratorNotDisposed - HashSet<T>.Dispose() does nothing
            var enumerator = hashSet.GetEnumerator();
            enumerator.MoveNext();
            return enumerator.Current;
        }
    }
}

namespace System.Text
{
    public static class TextExtensions
    {
        public static StringBuilder AppendLF(this StringBuilder sb) {
            sb.Append('\n');
            return sb;
        }

        public static StringBuilder AppendLF(this StringBuilder sb, string value)
        {
            sb.Append(value);
            return sb.Append('\n');
        }
    }
}

#if UNITY_5_3_OR_NEWER

namespace System.Collections.Generic
{
    public static class UnityExtensionGeneric
    {
        public static bool TryAdd<TKey,TValue>(this IDictionary<TKey,TValue> dictionary, TKey key, TValue value) {
            if (dictionary.ContainsKey(key))
                return false;
            
            dictionary.Add(key, value);
            return true;
        }

        public static int EnsureCapacity<TKey,TValue>(this Dictionary<TKey,TValue> dictionary, int capacity) {
            return 0;
        }
        
        public static int EnsureCapacity<T>(this HashSet<T> hashSet, int capacity) {
            return 0;
        }
        
        public static bool TryPeek<T>(this Stack<T> stack, out T result) {
            if (stack.Count > 0) {
                result = stack.Peek();
                return true;
            }
            result = default;
            return false;
        }
    }
}

namespace System.Collections.Concurrent
{
    public static class UnityExtensionConcurrent
    {
        public static void Clear<T>(this ConcurrentQueue<T> queue) {
            while (queue.TryDequeue(out T _)) { }
        }
    }
}

namespace System.Linq
{
    public static class UnityExtensionLinq
    {
        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> source) {
            return new HashSet<T>(source, null);
            /*var hashSet = new HashSet<T>();
            foreach (var element in source) {
                hashSet.Add(element);
            }
            return hashSet;*/
        }

        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> source, IEqualityComparer<T> comparer)
        {
            return new HashSet<T>(source, comparer);
            /*
            var hashSet = new HashSet<T>(comparer);
            foreach (var element in source) {
                hashSet.Add(element);
            }
            return hashSet;
            */
        }
    }
}
#endif
