﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System.Collections.Generic;
using System.Linq;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Tests.Common.Utils;
using Friflo.Json.Tests.Unity.Utils;
using NUnit.Framework;
// using static NUnit.Framework.Assert;
using static Friflo.Json.Tests.Common.Utils.SimpleAssert;

#if !UNITY_5_3_OR_NEWER

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Mapper
{

    public class TestILClassMapper : LeakTestsFixture
    {
        readonly string boxedStr = $@"
{{
    ""bigInt""      : ""123"",
    ""dateTime""    : ""2021-01-14T09:59:40.101Z"",
    ""enumIL""      : ""two""
}}";
        [Test]
        public void ReadWriteBoxed() {
            string payloadTrimmed = string.Concat(boxedStr.Where(c => !char.IsWhiteSpace(c)));
            
            using (var typeStore   = new TypeStore(new StoreConfig(TypeAccess.IL)))
            using (var reader      = new ObjectReader(typeStore) { ErrorHandler =  ObjectReader.NoThrow} )
            using (var writer      = new ObjectWriter(typeStore))
            using (var json        = new Bytes(payloadTrimmed))
            {
                var result = reader.Read<BoxedIL>(json);
                if (reader.Error.ErrSet)
                    Fail(reader.Error.msg.ToString());
                var jsonResult = writer.Write(result);
                AreEqual(payloadTrimmed, jsonResult);
            }
        }
        
        static readonly string structJson = $@"
{{
    ""structInt"": 200,
    ""child1"" : {{
        ""val2"": 201
    }},
    ""child2"" : null,
    ""childClass1"": null,
    ""childClass2"": {{
        ""val"": 202
    }}
}}
";

        [Test] public void  WriteStructReflect()   { WriteStruct(TypeAccess.Reflection); }
        [Test] public void  WriteStructIL()        { WriteStruct(TypeAccess.IL); }

        private void        WriteStruct(TypeAccess typeAccess) {
            string payloadTrimmed = string.Concat(structJson.Where(c => !char.IsWhiteSpace(c)));
            
            using (var typeStore   = new TypeStore(new StoreConfig(typeAccess)))
            using (var writer      = new ObjectWriter(typeStore))
            {
                var sample = new StructIL();
                sample.Init();
                var jsonResult = writer.Write(sample);
                AreEqual(payloadTrimmed, jsonResult);
            }
        }
        
        private void AssertStructIL(ref StructIL structIL) {
            AreEqual(200,   structIL.structInt);
            
            AreEqual(201,   structIL.child1.Value.val2);
            AreEqual(false, structIL.child2.HasValue);
            
            AreEqual(null,  structIL.childClass1);
            AreEqual(202,   structIL.childClass2.val);
        }
        
        [Test] public void  ReadStructReflect()   { ReadStruct(TypeAccess.Reflection); }
        [Test] public void  ReadStructIL()        { ReadStruct(TypeAccess.IL); }
        
        private void        ReadStruct(TypeAccess typeAccess) {
            using (var typeStore   = new TypeStore(new StoreConfig(typeAccess)))
            using (var reader      = new ObjectReader(typeStore) { ErrorHandler =  ObjectReader.NoThrow} )
            {
                var result = reader.Read<StructIL>(structJson);
                if (reader.Error.ErrSet)
                    Fail(reader.Error.msg.ToString());

                AssertStructIL(ref result);
            }
        }

        readonly string payloadStr = $@"
{{
    ""enumIL1""          : ""three"",
    ""enumIL2""          : null,
    ""childStructNull1"" : null,
    ""childStructNull2"" : {{
        ""val2"": 19
    }},
    ""nulDouble""       : 20.5,
    ""nulDoubleNull""   : null,
    ""nulFloat""        : 21.5,
    ""nulFloatNull""    : null,
    ""nulLong""         : 22,
    ""nulLongNull""     : null,
    ""nulInt""          : 23,
    ""nulIntNull""      : null,
    ""nulShort""        : 24,
    ""nulShortNull""    : null,
    ""nulByte""         : 25,
    ""nulByteNull""     : null,
    ""nulBool""         : true,
    ""nulBoolNull""     : null,

    ""childStruct1"": {{
        ""val2"": 111
    }},
    ""childStruct2"": {{
        ""val2"": 112
    }},
    ""child"": {{
        ""val"": 42
    }},
    ""childNull"": null,
    ""structIL"": {structJson},
    ""dbl"":   22.5,
    ""flt"":   33.5,

    ""int64"": 10,
    ""int32"": 11,
    ""int16"": 12,
    ""int8"":  13,

    ""bln"":   true
}}
";
        private void AssertSampleIL(SampleIL sample) {
            // ReSharper disable PossibleInvalidOperationException
            AreEqual(null,  sample.enumIL2);
            AreEqual(false, sample.childStructNull1.HasValue);
            AreEqual(19,    sample.childStructNull2.Value.val2);
            

            AreEqual(20.5d, sample.nulDouble.Value);
            AreEqual(null,  sample.nulDoubleNull);
            AreEqual(21.5f, sample.nulFloat.Value);
            AreEqual(null,  sample.nulFloatNull);
            AreEqual(22L,   sample.nulLong.Value);
            AreEqual(null,  sample.nulLongNull);
            AreEqual(23,    sample.nulInt.Value);
            AreEqual(null,  sample.nulIntNull);
            AreEqual(24,    sample.nulShort.Value);
            AreEqual(null,  sample.nulShortNull);
            AreEqual(25,    sample.nulByte.Value);
            AreEqual(null,  sample.nulByteNull);
            AreEqual(true,  sample.nulBool.Value);
            AreEqual(null,  sample.nulBoolNull);
            
            AreEqual(111,   sample.childStruct1.val2);
            AreEqual(112,   sample.childStruct2.val2);
            
            AreEqual(42,    sample.child.val);
            AreEqual(null,  sample.childNull);
            AssertStructIL(ref sample.structIL);
                
            AreEqual(22.5,  sample.dbl);
            AreEqual(33.5,  sample.flt);
                
            AreEqual(10,    sample.int64);
            AreEqual(11,    sample.int32);
            AreEqual(12,    sample.int16);
            AreEqual(13,    sample.int8);
            AreEqual(true,  sample.bln);

            AreEqual(42,    sample.child.val);
            AreEqual(null,  sample.childNull);
            AreEqual(111,   sample.childStruct1.val2);
            AreEqual(112,   sample.childStruct2.val2);
        }

        [Test] public void  WriteJsonReflect()   { WriteJson(TypeAccess.Reflection); }
        [Test] public void  WriteJsonIL()        { WriteJson(TypeAccess.IL); }

        private void        WriteJson(TypeAccess typeAccess) {
            string payloadTrimmed = string.Concat(payloadStr.Where(c => !char.IsWhiteSpace(c)));
            
            using (var typeStore   = new TypeStore(new StoreConfig(typeAccess)))
            using (var writer      = new ObjectWriter(typeStore))
            {
                var sample = new SampleIL();
                sample.Init();
                var jsonResult = writer.Write(sample);
                AreEqual(payloadTrimmed, jsonResult);
            }
        }
        
        [Test] public void  ReadClassReflect()   { ReadClassIL(TypeAccess.Reflection); }
        [Test] public void  ReadClassIL()        { ReadClassIL(TypeAccess.IL); }
        
        private void        ReadClassIL(TypeAccess typeAccess) {
            using (var typeStore   = new TypeStore(new StoreConfig(typeAccess)))
            using (var reader      = new ObjectReader(typeStore) { ErrorHandler =  ObjectReader.NoThrow} )
            {
                var result = reader.Read<SampleIL>(payloadStr);
                if (reader.Error.ErrSet)
                    Fail(reader.Error.msg.ToString());

                AssertSampleIL(result);
            }
        }
        
        [Test]  public void  NoAllocWriteClassReflect()   { NoAllocWriteClass(TypeAccess.Reflection); }
                public void  NoAllocWriteClassIL()        { NoAllocWriteClass(TypeAccess.IL); }
        
        private void        NoAllocWriteClass (TypeAccess typeAccess) {

            var memLog      = new MemoryLogger(100, 100, MemoryLog.Enabled);

            using (var typeStore   = new TypeStore(new StoreConfig(typeAccess)))
            using (var writer      = new ObjectWriter(typeStore))
            using (var dst         = new TestBytes())
            {
                var obj = new SampleIL();
                int iterations = 1000;
                for (int n = 0; n < iterations; n++) {
                    memLog.Snapshot();
                    writer.Write(obj, ref dst.bytes);
                }
            }
            if (typeAccess == TypeAccess.IL)
                memLog.AssertNoAllocations();
        }
        
        [Test]  public void  NoAllocReadClassReflect()   { NoAllocReadClass(TypeAccess.Reflection); }
                public void  NoAllocReadClassIL()        { NoAllocReadClass(TypeAccess.IL); }
        
        private void        NoAllocReadClass (TypeAccess typeAccess) {
            var memLog      = new MemoryLogger(100, 100, MemoryLog.Enabled);

            using (var typeStore   = new TypeStore(new StoreConfig(typeAccess)))
            using (var reader      = new ObjectReader(typeStore) { ErrorHandler =  ObjectReader.NoThrow} )
            using (var json        = new Bytes(payloadStr))
            {
                var obj = new SampleIL();
                obj.Init();
                int iterations = 1000;
                for (int n = 0; n < iterations; n++) {
                    memLog.Snapshot();
                    obj = reader.ReadTo(json, obj, false);
                    if (reader.Error.ErrSet)
                        Fail(reader.Error.msg.ToString());
                    AssertSampleIL(obj);
                }
            }
            if (typeAccess == TypeAccess.IL)
                memLog.AssertNoAllocations();
        }
        
        [Test] public void  ReadWriteStructReflect()   { ReadWriteStruct(TypeAccess.Reflection); }
        [Test] public void  ReadWriteStructIL()        { ReadWriteStruct(TypeAccess.IL); }
        
        private void        ReadWriteStruct (TypeAccess typeAccess) {
            using (var typeStore   = new TypeStore(new StoreConfig(typeAccess)))
            using (var reader      = new ObjectReader(typeStore) { ErrorHandler =  ObjectReader.NoThrow} )
            using (var writer      = new ObjectWriter(typeStore))
            using (var dst         = new TestBytes())
            {
                var obj = new ChildStructIL();
                writer.Write(obj, ref dst.bytes);
                var result = reader.Read<ChildStructIL>(dst.bytes);
                if (reader.Error.ErrSet)
                    Fail(reader.Error.msg.ToString());
                AreEqual(obj, result);
            }
        }
        
        [Test]  public void NoAllocListClassReflect()   { NoAllocListClass(TypeAccess.Reflection); }
                public void NoAllocListClassIL()        { NoAllocListClass(TypeAccess.IL); }
        
        private void        NoAllocListClass (TypeAccess typeAccess) {
            var memLog      = new MemoryLogger(100, 100, MemoryLog.Enabled);

            using (var typeStore   = new TypeStore(new StoreConfig(typeAccess)))
            using (var reader      = new ObjectReader(typeStore) { ErrorHandler =  ObjectReader.NoThrow} )
            using (var writer      = new ObjectWriter(typeStore))
            using (var dst         = new TestBytes())
            {
                var list = new List<SampleIL>() { new SampleIL() };
                list[0].Init();
                int iterations = 1000;
                for (int n = 0; n < iterations; n++) {
                    memLog.Snapshot();
                    writer.Write(list, ref dst.bytes);
                    list = reader.ReadTo(dst.bytes, list, false);
                    if (reader.Error.ErrSet)
                        Fail(reader.Error.msg.ToString());
                    AssertSampleIL(list[0]);
                }
            }
            if (typeAccess == TypeAccess.IL)
                memLog.AssertNoAllocations();
        }
        
        [Test]  public void NoAllocListStructReflect()   { NoAllocListStruct(TypeAccess.Reflection); }
                public void NoAllocListStructIL()        { NoAllocListStruct(TypeAccess.IL); } 
        
        private void        NoAllocListStruct (TypeAccess typeAccess) {
            var memLog      = new MemoryLogger(100, 100, MemoryLog.Enabled);

            using (var typeStore   = new TypeStore(new StoreConfig(typeAccess)))
            using (var reader      = new ObjectReader(typeStore) { ErrorHandler =  ObjectReader.NoThrow} )
            using (var writer      = new ObjectWriter(typeStore))
            using (var dst         = new TestBytes())
            {
                var list = new List<ChildStructIL>() { new ChildStructIL{val2 = 42} };
                int iterations = 1000;
                for (int n = 0; n < iterations; n++) {
                    memLog.Snapshot();
                    writer.Write(list, ref dst.bytes);
                    list[0] = new ChildStructIL { val2 = 999 };
                    list = reader.ReadTo(dst.bytes, list, false);
                    AreEqual(42, list[0].val2);   // ensure List element being a struct is updated
                    if (reader.Error.ErrSet)
                        Fail(reader.Error.msg.ToString());
                }
            }
            if (typeAccess == TypeAccess.IL)
                memLog.AssertNoAllocations();
        }

        [Test]  public void NoAllocArrayClassReflect()   { NoAllocArrayClass(TypeAccess.Reflection); }
                public void NoAllocArrayClassIL()        { NoAllocArrayClass(TypeAccess.IL); } 
        
        private void        NoAllocArrayClass (TypeAccess typeAccess) {
            var memLog      = new MemoryLogger(100, 100, MemoryLog.Enabled);

            using (var typeStore   = new TypeStore(new StoreConfig(typeAccess)))
            using (var reader      = new ObjectReader(typeStore) { ErrorHandler =  ObjectReader.NoThrow} )
            using (var writer      = new ObjectWriter(typeStore))
            using (var dst         = new TestBytes())
            {
                var arr = new [] { new SampleIL() };
                arr[0].Init();
                int iterations = 1000;
                for (int n = 0; n < iterations; n++) {
                    memLog.Snapshot();
                    writer.Write(arr, ref dst.bytes);
                    arr = reader.ReadTo(dst.bytes, arr, false);
                    if (reader.Error.ErrSet)
                        Fail(reader.Error.msg.ToString());
                    AssertSampleIL(arr[0]);
                }
            }
            if (typeAccess == TypeAccess.IL)
                memLog.AssertNoAllocations();
        }
        
        [Test]  public void NoAllocArrayStructReflect()   { NoAllocArrayStruct(TypeAccess.Reflection); }
                public void NoAllocArrayStructIL()        { NoAllocArrayStruct(TypeAccess.IL); }
        
        private void        NoAllocArrayStruct (TypeAccess typeAccess) {
            var memLog      = new MemoryLogger(100, 100, MemoryLog.Enabled);

            using (var typeStore   = new TypeStore(new StoreConfig(typeAccess)))
            using (var reader      = new ObjectReader(typeStore) { ErrorHandler =  ObjectReader.NoThrow} )
            using (var writer      = new ObjectWriter(typeStore))
            using (var dst         = new TestBytes())
            {
                var arr = new [] { new ChildStructIL{val2 = 42} };
                int iterations = 1000;
                for (int n = 0; n < iterations; n++) {
                    memLog.Snapshot();
                    writer.Write(arr, ref dst.bytes);
                    arr[0] = new ChildStructIL { val2 = 999 };
                    arr = reader.ReadTo(dst.bytes, arr, false);
                    AreEqual(42, arr[0].val2);   // ensure array element being a struct is updated
                    if (reader.Error.ErrSet)
                        Fail(reader.Error.msg.ToString());
                }
            }
            if (typeAccess == TypeAccess.IL)
                memLog.AssertNoAllocations();
        }

    }
}

#endif



