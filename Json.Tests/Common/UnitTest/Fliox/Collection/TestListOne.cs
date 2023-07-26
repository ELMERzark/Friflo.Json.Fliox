// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Fliox.Collections;
using Friflo.Json.Fliox.Mapper;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Collection
{
    public static class TestListOne
    {
        [Test]
        public static void TestListOne_Add() {
            var list = new ListOne<int>();
            // --- items: 0
            AreEqual(0, list.Count);
            AreEqual(1, list.Capacity);
            var span = list.GetSpan();
            AreEqual(0,  span.Length);
            Throws<IndexOutOfRangeException>(() => { var _ = list[0]; });
            Throws<IndexOutOfRangeException>(() => { var _ = list.GetSpan()[0]; });

            // --- items: 1
            list.Add(20);
            AreEqual(1, list.Count);
            AreEqual(1, list.Capacity);
            AreEqual(20, list[0]);
            span = list.GetSpan();
            AreEqual(1,  span.Length);
            AreEqual(20, span[0]);
            Throws<IndexOutOfRangeException>(() => { var _ = list[1]; });
            Throws<IndexOutOfRangeException>(() => { var _ = list.GetSpan()[1]; });

            // --- items: 2
            list.Add(21);
            AreEqual(2, list.Count);
            AreEqual(4, list.Capacity);
            AreEqual(20, list[0]);
            AreEqual(21, list[1]);
            span = list.GetSpan();
            AreEqual(2,  span.Length);
            AreEqual(20, span[0]);
            AreEqual(21, span[1]);

            // --- items: 2 - changed capacity
            list.Capacity = 10;
            AreEqual(2, list.Count);
            AreEqual(10, list.Capacity);
            AreEqual(20, list[0]);
            AreEqual(21, list[1]);
            span = list.GetSpan();
            AreEqual(2,  span.Length);
            AreEqual(20, span[0]);
            AreEqual(21, span[1]);
            Throws<IndexOutOfRangeException>(() => { var _ = list[2]; });
            Throws<IndexOutOfRangeException>(() => { var _ = list.GetSpan()[2]; });
            
            // --- items: 1 - RemoveRange()

        }
        
        [Test]
        public static void TestListOne_RemoveRange() {
            var list = new ListOne<int>();
            list.Add(20);
            list.Add(21);
            
            list.RemoveRange(0, 1);
            AreEqual(1,  list.Count);
            AreEqual(4,  list.Capacity);
            AreEqual(21, list[0]);
            var span = list.GetSpan();
            AreEqual(1,  span.Length);
            AreEqual(21, span[0]);
        }
        
        [Test]
        public static void TestListOne_Mapper()
        {
            var mapper  = new ObjectMapper(new TypeStore());
            var list    = new ListOne<int>();
            
            var json = mapper.writer.WriteAsBytes(list);
            AreEqual("[]", json.AsString());
            
            list.Add(1);
            json = mapper.writer.WriteAsBytes(list);
            AreEqual("[1]", json.AsString());
            
            list.Add(2);
            json = mapper.writer.WriteAsBytes(list);
            AreEqual("[1,2]", json.AsString());
            
            var start = Mem.GetAllocatedBytes();
            mapper.writer.WriteAsBytes(list);
            var diff = Mem.GetAllocationDiff(start);
            Mem.AreEqual(0, diff);
        }
    }
}