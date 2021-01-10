﻿using System.Collections.Generic;
using Friflo.Json.Burst;
using NUnit.Framework;

using static NUnit.Framework.Assert;
// ReSharper disable InconsistentNaming
#pragma warning disable 618

namespace Friflo.Json.Tests.Common.Examples
{
    public class Parser
    {
        // Note: new properties can be added to the JSON anywhere without changing compatibility
        static readonly string jsonString = @"
{
    ""firstName"":  ""John"",
    ""age"":        24,
    ""hobbies"":    [
        {""name"":  ""Gaming"" },
        {""name"":  ""STAR WARS""}
    ],
    ""unknownMember"": { ""anotherUnknown"": 42}
}";
        public class Buddy {
            public  string       firstName;
            public  int          age;
            public  List<Hobby>  hobbies = new List<Hobby>();
        }
    
        public struct Hobby {
            public string   name;
        }

        /// <summary>
        /// The following JSON reader is split into multiple Read...() methods each having only one while loop to support:
        /// - Read...() methods can be reused enabling the DRY principle
        /// - Read...() methods can be unit tested
        /// - enhance readability
        /// - enhance maintainability
        /// - enables the possibility to create readable code via a code generator
        ///
        /// A weak example is shown at <see cref="ParserMonolith"/> doing exactly the same processing. 
        /// </summary>
        [Test]
        public void ReadJson() {
            Buddy buddy = new Buddy();
            
            JsonParser p = new JsonParser();
            Bytes json = new Bytes(jsonString);
            try {
                p.InitParser(json);
                p.NextEvent(); // ObjectStart
                ReadBuddy(ref p, ref buddy);

                AreEqual(JsonEvent.EOF, p.NextEvent());
                if (p.error.ErrSet)
                    Fail(p.error.msg.ToString());
                AreEqual("John",        buddy.firstName);
                AreEqual(24,            buddy.age);
                AreEqual(2,             buddy.hobbies.Count);
                AreEqual("Gaming",      buddy.hobbies[0].name);
                AreEqual("STAR WARS",   buddy.hobbies[1].name);
            }
            finally {
                // only required for Unity/JSON_BURST
                json.Dispose();
                p.Dispose();
            }
        }
        
        private static void ReadBuddy(ref JsonParser p, ref Buddy buddy) {
            var i = new ObjectIterator();
            while (p.NextObjectMember(ref i)) {
                if      (p.UseMemberStr (ref i, "firstName"))    { buddy.firstName = p.value.ToString(); }
                else if (p.UseMemberNum (ref i, "age"))          { buddy.age = p.ValueAsInt(out _); }
                else if (p.UseMemberArr (ref i, "hobbies"))      { ReadHobbyList(ref p, ref buddy.hobbies); }
            }
        }
        
        private static void ReadHobbyList(ref JsonParser p, ref List<Hobby> hobbyList) {
            var i = new ArrayIterator();
            while (p.NextArrayElement(ref i)) {
                if (p.UseElementObj(ref i)) {
                    var hobby = new Hobby();
                    ReadHobby(ref p, ref hobby);
                    hobbyList.Add(hobby);
                }
            }
        }
        
        private static void ReadHobby(ref JsonParser p, ref Hobby hobby) {
            var i = new ObjectIterator();
            while (p.NextObjectMember(ref i)) {
                if (p.UseMemberStr(ref i, "name"))  { hobby.name = p.value.ToString(); }
            }
        }
    }
}