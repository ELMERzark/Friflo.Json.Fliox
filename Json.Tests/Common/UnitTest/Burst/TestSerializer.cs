﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System; // for DEBUG
using Friflo.Json.Burst;
using Friflo.Json.Tests.Common.Utils;
using Friflo.Json.Tests.Unity.Utils;
using NUnit.Framework;

using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Burst
{
    public class TestSerializer : LeakTestsFixture
    {
        [Test]
        public void TestBasics() {
            Utf8JsonWriter serializer = new Utf8JsonWriter();
            try {
                RunSerializer(ref serializer);
            }
            finally {
                serializer.Dispose();
            }
        }

        private void RunSerializer(ref Utf8JsonWriter s) {
            {
                s.InitSerializer();
                s.ObjectStart();
                s.ObjectEnd();
                AreEqual("{}", s.json.AsString());
                AreEqual(0, s.Level);
            } {
                s.InitSerializer();
                s.ArrayStart(true);
                s.ArrayEnd();
                AreEqual("[]", s.json.AsString());
            } {
                s.InitSerializer();
                s.ArrayStart(true);
                    s.ArrayStart(true);
                    s.ArrayEnd();
                    s.ObjectStart();
                    s.ObjectEnd();
                    s.ElementStr("hello");
                    s.ElementDbl(10.5);
                    s.ElementLng(42);
                    s.ElementBln(true);
                    s.ElementNul();
                s.ArrayEnd();
                AreEqual("[[],{},\"hello\",10.5,42,true,null]", s.json.AsString());
            } {
                s.InitSerializer();
                s.ObjectStart();
                    s.MemberStr ("string 👋", "World 🌎");
                    s.MemberDbl ("double 👋", 10.5);
                    s.MemberLng ("long 👋", 42);
                    s.MemberBln ("bool 👋", true);
                    s.MemberNul ("null 👋");
                s.ObjectEnd();
                AreEqual("{\"string 👋\":\"World 🌎\",\"double 👋\":10.5,\"long 👋\":42,\"bool 👋\":true,\"null 👋\":null}", s.json.AsString());
            } {
                // Escape string (or FixedString32 when compiling with JSON_BURST)
                s.InitSerializer();
                s.ObjectStart();
                s.MemberStr ("1-\r", "World 🌎");
                s.MemberDbl ("2-\n", 10.5);
                s.MemberLng ("3-\t", 42);
                s.MemberBln ("4-\"", true);
                s.MemberNul ("5-\\");
                s.MemberNul ("6-\b");
                s.MemberNul ("7-\f");
                s.ObjectEnd();
                AreEqual("{\"1-\\r\":\"World 🌎\",\"2-\\n\":10.5,\"3-\\t\":42,\"4-\\\"\":true,\"5-\\\\\":null,\"6-\\b\":null,\"7-\\f\":null}", s.json.AsString());
            } {
                using (var key1 = new Bytes("1-\r"))
                using (var key2 = new Bytes("2-\n"))
                using (var key3 = new Bytes("3-\t"))
                using (var key4 = new Bytes("4-\""))
                using (var key5 = new Bytes("5-\\"))
                using (var key6 = new Bytes("6-\b"))
                using (var key7 = new Bytes("7-\f"))
                {
                    s.InitSerializer();
                    s.ObjectStart();
                    s.MemberStr(key1.AsSpan(), "World 🌎");
                    s.MemberDbl(key2.AsSpan(), 10.5);
                    s.MemberLng(key3.AsSpan(), 42);
                    s.MemberBln(key4.AsSpan(), true);
                    s.MemberNul(key5.AsSpan());
                    s.MemberNul(key6.AsSpan());
                    s.MemberNul(key7.AsSpan());
                    s.ObjectEnd();
                }
                AreEqual("{\"1-\\r\":\"World 🌎\",\"2-\\n\":10.5,\"3-\\t\":42,\"4-\\\"\":true,\"5-\\\\\":null,\"6-\\b\":null,\"7-\\f\":null}", s.json.AsString());
            } {
                s.InitSerializer();
                s.ObjectStart();
                    s.MemberArrayStart ("array", true);
                    s.ArrayEnd();
                    
                    s.MemberObjectStart ("object");
                    s.ObjectEnd();
                s.ObjectEnd();
                AreEqual("{\"array\":[],\"object\":{}}", s.json.AsString());
            }
            // --- ensure coverage of methods using Bytes as parameter
            using (var textValue = new Bytes("textValue"))
            using (var str = new Bytes("str"))
            using (var dbl = new Bytes("dbl"))
            using (var lng = new Bytes("lng"))
            {
                // - array
                s.InitSerializer();
                s.ArrayStart(true);
                s.ElementStr(textValue.AsSpan());
                s.ArrayEnd();
                AreEqual("[\"textValue\"]", s.json.AsString());
                
                // - object
                s.InitSerializer();
                s.ObjectStart();
                s.MemberStr(str.AsSpan(), "hello");
                s.MemberDbl(dbl.AsSpan(), 10.5);
                s.MemberLng(lng.AsSpan(), 42);
                s.ObjectEnd();
                AreEqual("{\"str\":\"hello\",\"dbl\":10.5,\"lng\":42}", s.json.AsString());
            }

            // --- Primitives on root level ---
            {
                s.InitSerializer();
                s.ElementStr("hello");
                AreEqual("\"hello\"", s.json.AsString());
            } {
                s.InitSerializer();
                s.ElementLng(42);
                AreEqual("42", s.json.AsString());
            } {
                s.InitSerializer();
                s.ElementDbl(10.5);
                AreEqual("10.5", s.json.AsString());
            } {
                s.InitSerializer();
                s.ElementBln(true);
                AreEqual("true", s.json.AsString());
            } {
                s.InitSerializer();
                s.ElementNul();
                AreEqual("null", s.json.AsString());
            }
#if DEBUG
            Utf8JsonWriter ser = s; // capture
            
            // --- test DEBUG safety guard exceptions ---
            {
                var e = Throws<InvalidOperationException>(() =>
                {
                    ser.InitSerializer();
                    ser.ObjectStart();
                    ser.ObjectStart(); // object can only start in an object via MemberObjectStart();
                });
                AreEqual("ObjectStart() and ArrayStart() requires a previous call to a ...Start() method", e.Message);
            } {
                var e = Throws<InvalidOperationException>(()=> {
                    ser.InitSerializer();
                    ser.ObjectStart();
                    ser.ArrayStart(true); // array can only start in an object via MemberArrayStart();
                });
                AreEqual("ObjectStart() and ArrayStart() requires a previous call to a ...Start() method", e.Message);
            } {
                var e = Throws<InvalidOperationException>(()=> {
                    ser.InitSerializer();
                    ser.MemberBln ("Test", true); // member not in root
                });
                AreEqual("Member...() methods and ObjectEnd() must not be called on root level", e.Message);
            } {
                var e = Throws<InvalidOperationException>(()=> {
                    ser.InitSerializer();
                    ser.ArrayStart(true);
                    ser.MemberBln ("Test", true); // member not in array
                });
                AreEqual("Member...() methods and ObjectEnd() must be called only within an object", e.Message);
            } {
                var e = Throws<InvalidOperationException>(()=> {
                    ser.InitSerializer();
                    ser.ObjectStart();
                    ser.ElementBln(true); // element not in object
                });
                AreEqual("Element...() methods and ArrayEnd() must be called only within an array or on root level", e.Message);
            } {
                var e = Throws<InvalidOperationException>(()=> {
                    ser.InitSerializer();
                    ser.ObjectStart();
                    ser.ArrayEnd();
                });
                AreEqual("Element...() methods and ArrayEnd() must be called only within an array or on root level", e.Message);
            } {
                var e = Throws<InvalidOperationException>(() => {
                    ser.InitSerializer();
                    ser.ArrayStart(true);
                    ser.ObjectEnd();
                });
                AreEqual("Member...() methods and ObjectEnd() must be called only within an object", e.Message);
            } {
                var e = Throws<InvalidOperationException>(() => {
                    ser.InitSerializer();
                    ser.ObjectEnd();
                });
                AreEqual("Member...() methods and ObjectEnd() must not be called on root level", e.Message);
            } {
                var e = Throws<InvalidOperationException>(() => {
                    ser.InitSerializer();
                    ser.ArrayEnd();
                });
                AreEqual("ArrayEnd...() must not be called below root level", e.Message);
            }
#endif
        }
        
        [Test]
        public void TestMaxDepth() {
            using (Utf8JsonWriter ser = new Utf8JsonWriter())
            {
                // --- Utf8JsonWriter
                ser.InitSerializer();
                ser.SetMaxDepth(1);
                
                // case OK
                ser.ArrayStart(true);
                ser.ArrayEnd();
                AreEqual(0, ser.Level);
                AreEqual("[]", ser.json.AsString());
                
                // case exception
                ser.ArrayStart(true);
                var e = Throws<InvalidOperationException>(() => ser.ArrayStart(true)); // add second array
                AreEqual("JsonWriter exceed maxDepth: 1", e.Message);
            }
        }
        
        [Test]
        public void Pretty () {
            using (var parser = new Local<Utf8JsonParser>())
            using (var ser = new Utf8JsonWriter())
            using (Bytes bytes = CommonUtils.FromFile("assets~/Burst/codec/complex.json")) {
                ser.SetPretty(true);
                parser.value.InitParser(bytes);
                ser.InitSerializer();
                parser.value.NextEvent();
                ser.WriteTree(ref parser.value);
                CommonUtils.ToFile("assets~/Burst/output/complexPrettySerializer.json", ser.json);
            }
        }
    }
}