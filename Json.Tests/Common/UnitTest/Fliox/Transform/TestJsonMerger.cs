﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Fliox;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Mapper.Diff;
using Friflo.Json.Fliox.Transform.Tree;
using NUnit.Framework;
using static NUnit.Framework.Assert;

// ReSharper disable NotAccessedField.Local
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Transform
{
    public class TestJsonMerger
    {
        private  class MergePrimitives {
            public  int         intEqual;
            public  int         intNotEqual;
            public  bool        boolEqual;
            public  bool        boolNotEqual;
            public  string      strEqual;
            public  string      strEqualNull;
            public  string      strNotEqual;
            public  string      strOnlyLeft;
            public  string      strOnlyRight;
        }
        
        [Test]
        public void TestJsonMergePrimitives() {
            using (var typeStore        = new TypeStore()) 
            using (var differ           = new ObjectDiffer(typeStore))
            using (var jsonDiff         = new JsonDiff(typeStore))
            using (var writer           = new ObjectWriter(typeStore))
            using (var merger           = new JsonMerger())
            {
                var left    = new MergePrimitives {
                    intEqual        = 1,
                    intNotEqual     = 2,
                    boolEqual       = true,
                    boolNotEqual    = true,
                    strEqual        = "Test",
                    strEqualNull    = null, 
                    strNotEqual     = "Str-1",
                    strOnlyLeft     = "only left",
                    strOnlyRight    = null
                };
                var right   = new MergePrimitives {
                    intEqual        = 1,
                    intNotEqual     = 22,
                    boolEqual       = true,
                    boolNotEqual    = false,
                    strEqual        = "Test",
                    strEqualNull    = null,
                    strNotEqual     = "Str-2",
                    strOnlyLeft     = null,
                    strOnlyRight    = "only right"
                };
                PrepareMerge(left, right, differ, jsonDiff, writer, out var value, out var patch);
                
                var merge       = merger.Merge(value, patch);
                var expect      =
"{'intEqual':1,'intNotEqual':22,'boolEqual':true,'boolNotEqual':false,'strEqual':'Test','strNotEqual':'Str-2','strOnlyRight':'only right'}".Replace('\'', '"');
                AreEqual(expect, merge.AsString());
                
                AssertAlloc(value, patch, 1, merger);
            }
        }
        
        private class MergeChild {
            public  int     val;
        }
        
        private  class MergeClasses {
            public  MergeChild  childEqual;
            public  MergeChild  childNotEqual;
            public  MergeChild  childEqualNull;
            public  MergeChild  childOnlyLeft;
            public  MergeChild  childOnlyRight;
        }
        
        [Test]
        public void TestJsonMergeClasses() {
            using (var typeStore        = new TypeStore()) 
            using (var differ           = new ObjectDiffer(typeStore))
            using (var jsonDiff         = new JsonDiff(typeStore))
            using (var writer           = new ObjectWriter(typeStore))
            using (var merger           = new JsonMerger())
            {
                var left    = new MergeClasses {
                    childEqual      = new MergeChild { val = 1 },
                    childNotEqual   = new MergeChild { val = 2 },
                    childEqualNull  = null,
                    childOnlyLeft   = new MergeChild { val = 4 },
                    childOnlyRight  = null
                };
                var right   = new MergeClasses {
                    childEqual      = new MergeChild { val = 1 },
                    childNotEqual   = new MergeChild { val = 3 },
                    childEqualNull  = null,
                    childOnlyLeft   = null,
                    childOnlyRight  = new MergeChild { val = 5 },
                };
                PrepareMerge(left, right, differ, jsonDiff, writer, out var value, out var patch);
                
                var merge       = merger.Merge(value, patch);
                var expect      =
"{'childEqual':{'val':1},'childNotEqual':{'val':3},'childOnlyRight':{'val':5}}".Replace('\'', '"');
                AreEqual(expect, merge.AsString());
                
                AssertAlloc(value, patch, 1, merger);
            }
        }
        
        private  class MergeArray {
            public  int[]       int1;
            public  int[]       int2;
            public  bool[]      bool1;
            public  bool[]      bool2;
            public  string[]    str1;
            public  string[]    str2;
        }
        
        [Test]
        public void TestJsonMergeArrays() {
            using (var typeStore        = new TypeStore()) 
            using (var differ           = new ObjectDiffer(typeStore))
            using (var jsonDiff         = new JsonDiff(typeStore))
            using (var writer           = new ObjectWriter(typeStore))
            using (var merger           = new JsonMerger())
            {
                var left = new MergeArray {
                    int1  = new [] { 10 },
                    int2  = new [] { 20 },
                    bool1 = new [] { true },
                    bool2 = new [] { true },
                    str1  = new [] { "A" },
                    str2  = new [] { "B" },
                };
                var right = new MergeArray {
                    int1  = new [] { 10 },
                    int2  = new [] { 21 },
                    bool1 = new [] { true },
                    bool2 = new [] { false },
                    str1  = new [] { "A" },
                    str2  = new [] { "C" },
                };
                PrepareMerge(left, right, differ, jsonDiff, writer, out var value, out var patch);
                
                var merge       = merger.Merge(value, patch);
                var expect      =
"{'int1':[10],'int2':[21],'bool1':[true],'bool2':[false],'str1':['A'],'str2':['C']}".Replace('\'', '"');
                AreEqual(expect, merge.AsString());
                
                AssertAlloc(value, patch, 1, merger);
            }
        }
        
        private static void PrepareMerge<T>(
                T               left,
                T               right,
                ObjectDiffer    differ,
                JsonDiff        jsonDiff,
                ObjectWriter    writer,
            out JsonValue       value,  // left as JSON
            out JsonValue       patch)  // the merge patch - when merging to left the result is right
        {
            var diff    = differ.GetDiff(left, right, DiffKind.DiffArrays);
            patch       = jsonDiff.CreateJsonDiff(diff);

            writer.Pretty           = true;
            writer.WriteNullMembers = false;
            value        = writer.WriteAsValue(left);
        }
        
        private static void AssertAlloc(JsonValue value, JsonValue patch, int count, JsonMerger merger) {
            merger.MergeBytes(value, patch);

            var start   = GC.GetAllocatedBytesForCurrentThread();
            for (int n = 0; n < count; n++) {
                merger.MergeBytes(value, patch);
            }
            var dif     = GC.GetAllocatedBytesForCurrentThread() - start;
            AreEqual(0, dif);
        }
    }
}