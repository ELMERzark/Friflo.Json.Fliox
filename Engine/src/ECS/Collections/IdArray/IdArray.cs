﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;

// ReSharper disable once CheckNamespace
namespace Friflo.Engine.ECS.Collections;

internal struct IdArray
{
    public              int     Count => count;
    /// <summary>
    /// Stores an id in case <see cref="count"/> == 1.<br/>
    /// The start index within <see cref="IdArrayPool.ids"/> if <see cref="count"/> > 1.
    /// </summary>
    internal            int     start;
    /// <summary>
    /// Number of array ids.
    /// </summary>
    internal readonly   int     count;

    public   override   string  ToString() => GetString();

    internal IdArray(int start, int count) {
        this.start = start;
        this.count = count;
    }
    
    private string GetString()
    {
        if (count == 0) {
            return "count: 0";    
        }
        if (count == 1) {
            return $"count: 1  id: {start}";    
        }
        return $"count: {count}  index: {IdArrayHeap.PoolIndex(count)}  start: {start}";
    }
}


internal static class IdArrayExtensions {

    internal static ReadOnlySpan<int> GetSpan(this ref IdArray array, IdArrayHeap heap, EntityStoreBase store)
    {
        var count = array.count;
        switch (count) {
            case 0:     return default;
            case 1:     return store.GetSpanId(array.start);
        }
        var curPoolIndex = IdArrayHeap.PoolIndex(count);
        return new ReadOnlySpan<int>(heap.GetPool(curPoolIndex).Ids, array.start, count);
    }
    
    public static void Add(this ref IdArray array, int id, IdArrayHeap heap)
    {
        var count = array.count; 
        if (count == 0) {
            array = new IdArray(id, 1);
            return;
        }
        var curStart = array.start;
        if (count == 1) {
            var pool        = heap.GetPool(1);
            var start       = pool.CreateArray(out var ids);
            ids[start]      = curStart;
            ids[start + 1]  = id;
            array = new IdArray(start, 2);
            return;
        }
        var newCount        = count + 1;
        var curPoolIndex    = IdArrayHeap.PoolIndex(count);
        var newPoolIndex    = IdArrayHeap.PoolIndex(newCount);
        var curPool         = heap.GetPool(curPoolIndex);
        if (newPoolIndex == curPoolIndex) {
            curPool.Ids[curStart + count] = id;
            array = new IdArray(curStart, newCount);
            return;
        }
        curPool.DeleteArray(curStart, out var curIds);
        var newPool     = heap.GetPool(newPoolIndex);
        var newStart    = newPool.CreateArray(out var newIds);
        
        new ReadOnlySpan<int> (curIds, curStart, count).CopyTo(
        new Span<int>         (newIds, newStart, count));

        newIds[newStart + count] = id;
        array = new IdArray(newStart, newCount);
    }
    
    public static void RemoveAt(this ref IdArray array, int index, IdArrayHeap heap, bool keepOrder = false)
    {
        var count = array.count;
        if (index < 0 || index >= count) throw new IndexOutOfRangeException();
        if (count == 1) {   // index is 0
            array = default;
            return;
        }
        var curStart = array.start;
        if (count == 2) {   // index is 0 or 1
            var pool = heap.GetPool(1);
            pool.DeleteArray(curStart, out var ids);
            array = new IdArray(ids[curStart + 1 - index], 1);
            return;
        }
        var newCount        = count - 1;
        var curPoolIndex    = IdArrayHeap.PoolIndex(count);
        var newPoolIndex    = IdArrayHeap.PoolIndex(newCount);
        var curPool         = heap.GetPool(curPoolIndex);
        var tail            = newCount - index;
        if (newPoolIndex == curPoolIndex) {
            var ids = curPool.Ids;
            if (keepOrder) {
                // remove id at index
                new ReadOnlySpan<int> (ids, curStart + index + 1, tail).CopyTo(
                new Span<int>         (ids, curStart + index,     tail));
            } else {
                // move last id to deleted index
                ids[curStart + index] = ids[curStart + count - 1];
            }
            array = new IdArray(curStart, newCount);
            return;
        }
        curPool.DeleteArray(curStart, out var curIds);
        var newPool     = heap.GetPool(newPoolIndex);
        var newStart    = newPool.CreateArray(out var newIds);
        
        new ReadOnlySpan<int> (curIds, curStart,             index).CopyTo(
        new Span<int>         (newIds, newStart,             index));
        
        new ReadOnlySpan<int> (curIds, curStart + index + 1, tail).CopyTo(
        new Span<int>         (newIds, newStart + index,     tail));
        
        array = new IdArray(newStart, newCount);
    }
    
    public static void SetArray(this ref IdArray array, ReadOnlySpan<int> idSpan, IdArrayHeap heap)
    {
        var count       = array.count;
        var curStart    = array.start;
        if (count > 1 ) {
            var curPoolIndex    = IdArrayHeap.PoolIndex(count);
            var curPool         = heap.GetPool(curPoolIndex);
            curPool.DeleteArray(curStart, out _);
        }
        var newCount = idSpan.Length;
        switch (newCount) {
            case 0:
                array = default;
                return;
            case 1:
                array = new IdArray(idSpan[0], 1);
                return;
        }
        var newPoolIndex    = IdArrayHeap.PoolIndex(newCount);
        var newPool         = heap.GetPool(newPoolIndex);
        var newStart        = newPool.CreateArray(out var newIds);
        idSpan.CopyTo(new Span<int>(newIds, newStart, newCount));
        array = new IdArray(newStart, newCount);
    }
    
    public static void InsertAt(this ref IdArray array, int index, int id, IdArrayHeap heap)
    {
        var count = array.count; 
        if (index < 0 || index > count) throw new IndexOutOfRangeException();
        if (count == 0) {
            array = new IdArray(id, 1);
            return;
        }
        var curStart = array.start;
        if (count == 1) {
            var pool        = heap.GetPool(1);
            var start       = pool.CreateArray(out var ids);
            if (index == 0) {
                ids[start]      = id;
                ids[start + 1]  = curStart;
            } else {
                ids[start]      = curStart;
                ids[start + 1]  = id;
            }
            array = new IdArray(start, 2);
            return;
        }
        var newCount        = count + 1;
        var curPoolIndex    = IdArrayHeap.PoolIndex(count);
        var newPoolIndex    = IdArrayHeap.PoolIndex(newCount);
        var curPool         = heap.GetPool(curPoolIndex);
        var tail            = count - index;
        var curIndex        = curStart + index;
        if (newPoolIndex == curPoolIndex) {
            var ids         = curPool.Ids;
            new ReadOnlySpan<int> (ids, curIndex,     tail).CopyTo(
            new Span<int>         (ids, curIndex + 1, tail));
            curPool.Ids[curIndex] = id;
            array = new IdArray(curStart, newCount);
            return;
        }
        curPool.DeleteArray(curStart, out var curIds);
        var newPool     = heap.GetPool(newPoolIndex);
        var newStart    = newPool.CreateArray(out var newIds);
  
        new ReadOnlySpan<int> (curIds, curStart, index).CopyTo(
        new Span<int>         (newIds, newStart, index));
        
        new ReadOnlySpan<int> (curIds, curStart + index,     tail).CopyTo(
        new Span<int>         (newIds, newStart + index + 1, tail));

        newIds[newStart + index] = id;
        array = new IdArray(newStart, newCount);
    }
    
    /*
    internal static void Clear(this ref IdArray array, IdArrayHeap heap)
    {
        int count = array.count;
        if (count <= 1) {   // index is 0
            array = default;
            return;
        }
        var curPoolIndex    = IdArrayHeap.PoolIndex(count);
        var curPool         = heap.GetPool(curPoolIndex);
        curPool.DeleteArray(array.start, out _);
        array = default;
    } */

    internal static void SetAt(this ref IdArray array, int positionIndex, int value, IdArrayHeap heap)
    {
        int count = array.count;
        if (count == 1) {   // index is 0
            array.start = value;
            return;
        }
        var curPoolIndex    = IdArrayHeap.PoolIndex(count);
        var curPool         = heap.GetPool(curPoolIndex);
        curPool.Ids[array.start + positionIndex] = value;
    }
    
    internal static int GetAt(this IdArray array, int positionIndex, IdArrayHeap heap)
    {
        int count = array.count;
        if (count == 1) {   // index is 0
            return array.start;
        }
        var curPoolIndex    = IdArrayHeap.PoolIndex(count);
        var curPool         = heap.GetPool(curPoolIndex);
        return curPool.Ids[array.start + positionIndex];
    }
} 