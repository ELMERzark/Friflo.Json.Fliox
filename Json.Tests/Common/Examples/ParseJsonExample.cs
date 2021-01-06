﻿using System.Collections.Generic;
using Friflo.Json.Burst;
using NUnit.Framework;

using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.Examples
{
    public class ParseJsonExample
    {
        static readonly string jsonString = @"
{
    ""firstName"":  ""John"",
    ""age"":        24,
    ""hobbies"":    [
        {""name"":  ""Gaming"" },
        {""name"":  ""STAR WARS""}
    ]
}";
        public class Buddy {
            public          string       firstName;
            public          int          age;
            public readonly List<Hobby>  hobbies = new List<Hobby>();
        }
    
        public struct Hobby {
            public string   name;
        }

        [Test]
        public void ParseJson() {
            Buddy buddy = new Buddy();
            
            JsonParser p = new JsonParser();
            Bytes json = new Bytes(jsonString); 
            p.InitParser(json);
            p.NextEvent(); // expect JsonEvent.ObjectStart
            
            ReadBuddy(ref p, ref buddy);
            
            AreEqual(JsonEvent.EOF, p.NextEvent());
            if (p.error.ErrSet)
                Fail(p.error.msg.ToString());
            AreEqual("John",        buddy.firstName);
            AreEqual(24,            buddy.age);
            AreEqual("Gaming",      buddy.hobbies[0].name);
            AreEqual("STAR WARS",   buddy.hobbies[1].name);
        }
        
        public static void ReadBuddy(ref JsonParser p, ref Buddy buddy) {
            var readRoot = new ReadObject();
            while (readRoot.NextEvent(ref p)) {                     // descend to root object & iterate key/values
                if      (readRoot.UseStr(ref p, "firstName")) {
                    buddy.firstName = p.value.ToString();
                }
                else if (readRoot.UseNum(ref p, "age")) {
                    buddy.age = p.ValueAsInt(out _);
                }
                else if (readRoot.UseArr(ref p, "hobbies")) {       
                    var readHobbies = new ReadArray();
                    while (readHobbies.NextEvent(ref p)) {          // descend to hobbies array & iterate elements
                        if (readHobbies.UseObj(ref p)) {        
                            var hobby = new Hobby();
                            ReadHobby(ref p, ref hobby);
                            buddy.hobbies.Add(hobby);
                        }
                    }
                }
            }
        }
        
        public static void ReadHobby(ref JsonParser p, ref Hobby hobby) {
            var readHobby = new ReadObject();
            while (readHobby.NextEvent(ref p)) {    // descend to hobby object & iterate key/values
                if (readHobby.UseStr(ref p, "name")) {
                    hobby.name = p.value.ToString();
                }
            }
        }

        // ------------- Demonstrating an anti pattern, which is not recommended -------------
        [Test]
        public void ParseJsonMonolith() {
            Buddy buddy = new Buddy();
            
            JsonParser p = new JsonParser();
            Bytes json = new Bytes(jsonString); 
            p.InitParser(json);
            p.NextEvent(); // expect JsonEvent.ObjectStart

            var readRoot = new ReadObject();
            while (readRoot.NextEvent(ref p)) {                     // descend to root object & iterate key/values
                if      (readRoot.UseStr(ref p, "firstName")) {
                    buddy.firstName = p.value.ToString();
                }
                else if (readRoot.UseNum(ref p, "age")) {
                    buddy.age = p.ValueAsInt(out _);
                }
                else if (readRoot.UseArr(ref p, "hobbies")) {       
                    var readHobbies = new ReadArray();
                    while (readHobbies.NextEvent(ref p)) {          // descend to hobbies array & iterate elements
                        if (readHobbies.UseObj(ref p)) {        
                            var hobby = new Hobby();
                            var readHobby = new ReadObject();
                            while (readHobby.NextEvent(ref p)) {    // descend to hobby object & iterate key/values
                                if (readHobby.UseStr(ref p, "name")) {
                                    hobby.name = p.value.ToString();
                                }
                            }
                            buddy.hobbies.Add(hobby);
                        }
                    }
                }
            }
            AreEqual(JsonEvent.EOF, p.NextEvent());
            if (p.error.ErrSet)
                Fail(p.error.msg.ToString());
            AreEqual("John",        buddy.firstName);
            AreEqual(24,            buddy.age);
            AreEqual("Gaming",      buddy.hobbies[0].name);
            AreEqual("STAR WARS",   buddy.hobbies[1].name);
        }
    }
}