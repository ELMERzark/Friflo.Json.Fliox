﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System.Linq;
using Friflo.Json.Fliox;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Tests.Unity.Utils;
using NUnit.Framework;
using static NUnit.Framework.Assert;
using Serialize = Friflo.Json.Fliox.SerializeMemberAttribute;

#pragma warning disable 649 // Field 'field' is never assigned

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Mapper
{
    public class TestNaming : LeakTestsFixture
    {
        class Naming {
            public int      lower;
            public int      Upper;

            // ignored members
            [IgnoreMember]
            public int      ignoredField;
            
            [IgnoreMember]
            public int      ignoredProperty { get; set; }

            // custom member names
            [Serialize(Name = "field")]
            public int         namedField;
            
            [Serialize(Name = "property")]
            public int         namedProperty { get; set; }
        }
        
        [Test] public void CamelCaseReflect()    { CamelCase(TypeAccess.Reflection); }
        [Test] public void CamelCaseIL()         { CamelCase(TypeAccess.IL); }

        private void CamelCase(TypeAccess typeAccess) {
            string json = @"
            {
                ""property"":   10,
                ""lower"":      11,
                ""upper"":      12,
                ""field"":      13
            }";
            using (var typeStore =  new TypeStore(new StoreConfig(typeAccess, new CamelCaseNaming())))
            using (var m = new ObjectMapper(typeStore)) {
                var naming = m.Read<Naming>(json);
                var result = m.Write(naming);
                string expect = string.Concat(json.Where(c => !char.IsWhiteSpace(c)));
                
                AreEqual(expect, result);
            }
        }
        
        [Test] public void PascalCaseReflect()    { PascalCase(TypeAccess.Reflection); }
        [Test] public void PascalCaseIL()         { PascalCase(TypeAccess.IL); }
        
        private void PascalCase(TypeAccess typeAccess) {
            string json = @"
            {
                ""property"":   10,
                ""Lower"":      11,
                ""Upper"":      12,
                ""field"":      13
            }";
            using (var typeStore = new TypeStore(new StoreConfig(typeAccess, new PascalCaseNaming())))
            using (var m = new ObjectMapper(typeStore)) {
                var naming = m.Read<Naming>(json);
                var result = m.Write(naming);
                string expect = string.Concat(json.Where(c => !char.IsWhiteSpace(c)));

                AreEqual(expect, result);
            }
        }
        

    }
}