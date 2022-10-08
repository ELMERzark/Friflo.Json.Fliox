﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Tests.Common.Utils;
using Friflo.Json.Tests.Unity.Utils;
using NUnit.Framework;

using static NUnit.Framework.Assert;


namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Mapper
{
    // Ensure existence of basic API methods
    public class TestApi : LeakTestsFixture
    {
        // ------------------------------------ JsonReader / JsonWriter ------------------------------------
        [Test]
        public void ReaderWriter() {
            using (var typeStore   = new TypeStore())
            using (var read        = new ObjectReader(typeStore))
            using (var write       = new ObjectWriter(typeStore)) {
                AssertReaderBytes(read);
                AssertWriterBytes(write);
                
                AssertReaderStream(read);
                AssertWriterStream(write);
                
                AssertReaderString(read);
                AssertWriterString(write);
            }
        }

        // --------------------------------------- JSON ---------------------------------------
        /*
        [Test]
        public void Json() {
            AssertReaderBytes(JSON);
            AssertWriterBytes(JSON);
            
            AssertReaderStream(JSON);
            AssertWriterStream(JSON);
            
            AssertReaderString(JSON);
            AssertWriterString(JSON);
        } */
        
        
        // --------------------------------------- Formatter ---------------------------------------
        [Test]
        public void JsonMapper() {
            using (var  formatter   = new ObjectMapper())
            {
                AssertReaderBytes(formatter);
                AssertWriterBytes(formatter);
            
                AssertReaderStream(formatter);
                AssertWriterStream(formatter);
            
                AssertReaderString(formatter);
                AssertWriterString(formatter);
            }
        }
        
        [Test]
        public void ReaderException() {
            using (var typeStore    = new TypeStore())
            using (var read         = new ObjectReader(typeStore))
            using (var invalid      = new Bytes("invalid"))
            {
                var e = Throws<JsonReaderException>(() => read.Read<string>(invalid));
                AreEqual("JsonParser/JSON error: unexpected character while reading value. Found: i path: '(root)' at position: 1", e.Message);
                AreEqual(1, e.position);
            }
        }
        
        [Test]
        public void ReaderError() {
            using (var typeStore    = new TypeStore())
            using (var read         = new ObjectReader(typeStore) { ErrorHandler =  ObjectReader.NoThrow} )
            using (var invalid      = new Bytes("invalid"))
            {
                read.Read<string>(invalid);
                IsFalse(read.Success);
                AreEqual("JsonParser/JSON error: unexpected character while reading value. Found: i path: '(root)' at position: 1", read.Error.msg.AsString());
                AreEqual(1, read.Error.Pos);
            }
        }
        
        [Test]
        public void ReadPrimitives() {
            using (var typeStore    = new TypeStore())
            using (var read         = new ObjectReader(typeStore) { ErrorHandler =  ObjectReader.NoThrow} )
            {
                read.ReadObject("1", typeof(int));  // initiate one time allocations
                {
                    var start   = GC.GetAllocatedBytesForCurrentThread();
                    var result  = read.ReadObject("1", typeof(int));
                    var dif     = GC.GetAllocatedBytesForCurrentThread() - start;
                    IsTrue(read.Success);
                    AreEqual(1, result);
                    AreEqual(24, dif); // 24 bytes for boxing an int in the returned object
                } {
                    var result = read.ReadObject("true", typeof(int));
                    AreEqual(0, result);
                    IsFalse(read.Success);
                    AreEqual("JsonReader/error: Cannot assign bool to int. got: true path: '(root)' at position: 4", read.Error.msg.AsString());
                } {
                    var result = read.ReadObject("null", typeof(int));
                    AreEqual(0, result);
                    IsFalse(read.Success);
                    AreEqual("JsonReader/error: Cannot assign null to int. got: null path: '(root)' at position: 4", read.Error.msg.AsString());
                }
            }
        }
        
       
        // ------------------------------ Assert Bytes / string / Stream ------------------------------
        private static void AssertReaderBytes(IJsonReader read) {
            using (var num1 = new Bytes("1"))
            using (var arr1 = new Bytes("[1]"))
            {
                // --- Read ---
                AreEqual(1, read.Read<int>(num1));                      // generic
                
                AreEqual(1, read.ReadObject(num1, typeof(int)));        // non generic
                
                
                // --- ReadTo ---
                int[] reuse  = new int[1];
                int[] expect = { 1 };
                int[] result = read.ReadTo(arr1, reuse);                // generic
                AreEqual(expect, result);   
                IsTrue(reuse == result); // same reference - size did not change
                
                object resultObj = read.ReadToObject(arr1, reuse);      // non generic
                AreEqual(expect, resultObj);
                IsTrue(reuse == resultObj); // same reference - size did not change
            }
        }
        
        private static void AssertWriterBytes(IJsonWriter write) {
            using (var dst  = new TestBytes())
            {
                // --- Write ---
                write.Write(1, ref dst.bytes);                          // generic
                AreEqual("1", dst.bytes.AsString());
                
                write.WriteObject(1, ref dst.bytes);                    // non generic 
                AreEqual("1", dst.bytes.AsString());
            }
        }

        private static void AssertReaderStream(IJsonReader read) {
            // --- Read ---
            AreEqual(1, read.Read<int>(StreamFromString("1")));                     // generic
            
            AreEqual(1, read.ReadObject(StreamFromString("1"), typeof(int)));       // non generic
            
          
            // --- ReadTo ---
            int[] reuse  = new int[1];
            int[] expect = { 1 };
            int[] result = read.ReadTo(StreamFromString("[1]"), reuse);             // generic
            AreEqual(expect, result);   
            IsTrue(reuse == result); // same reference - size did not change
            
            object resultObj = read.ReadToObject(StreamFromString("[1]"), reuse);   // non generic
            AreEqual(expect, resultObj);
            IsTrue(reuse == resultObj); // same reference - size did not change
        }
        
        private static void AssertWriterStream(IJsonWriter write) {
            // --- Write ---
            Stream stream = new MemoryStream();
            write.Write(1, stream);                                                 // generic
            AreEqual("1", StringFromStream(stream));

            stream.Position = 0;
            write.WriteObject(1, stream);                                           // non generic 
            AreEqual("1", StringFromStream(stream));
        }

        private static void AssertReaderString(IJsonReader read) {
            // --- Read ---
            AreEqual(1, read.Read<int>("1"));                       // generic
            
            AreEqual(1, read.ReadObject("1", typeof(int)));         // non generic
            
           
            // --- ReadTo ---
            int[] reuse  = new int[1];
            int[] expect = { 1 };
            int[] result = read.ReadTo("[1]", reuse);               // generic
            AreEqual(expect, result);   
            IsTrue(reuse == result); // same reference - size did not change
            
            object resultObj = read.ReadToObject("[1]", reuse);     // non generic
            AreEqual(expect, resultObj);
            IsTrue(reuse == resultObj); // same reference - size did not change
        }
        
        private static void AssertWriterString(IJsonWriter write) {
            // --- Write ---
            var json1 = write.Write(1);                             // generic
            AreEqual("1", json1);
            
            var json2 = write.WriteObject(1);                       // non generic 
            AreEqual("1", json2);
        }
        
        // ----------------------------------- utils -----------------------------------
        private static Stream StreamFromString(string s)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }
        
        private static string StringFromStream(Stream stream)
        {
            stream.Position = 0;
            StreamReader reader = new StreamReader(stream);
            string str = reader.ReadToEnd();
            return str;
        }
    }
}