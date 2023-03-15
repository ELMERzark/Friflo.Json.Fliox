﻿using System;
using Friflo.Json.Burst;
using NUnit.Framework;

namespace Friflo.Json.Tests.Common.Examples.Burst
{
    public class TestHelloWorld
    {
        [Test]
        public void HelloWorldParser() {
            string say = "", to = "";
            var p = new Utf8JsonParser();
            p.InitParser(new Bytes (@"{""say"": ""Hello"", ""to"": ""World 🌎""}"));
            p.ExpectRootObject(out JObj i);
            while (i.NextObjectMember(ref p)) {
                if (i.UseMemberStr (ref p, "say"))  { say = p.value.AsString(); }
                if (i.UseMemberStr (ref p, "to"))   { to =  p.value.AsString(); }
            }
            Console.WriteLine($"Output: {say}, {to}");
            // Output: Hello, World 🌎
        }
        
        [Test]
        public void HelloWorldSerializer() {
            var s = new Utf8JsonWriter();
            s.InitSerializer();
            s.ObjectStart();
                s.MemberStr ("say", "Hello");
                s.MemberStr ("to",  "World 🌎");
            s.ObjectEnd();
            Console.WriteLine($"Output: {s.json.AsString()}");
            // Output: {"say":"Hello","to":"World 🌎"}
        }
    }
}