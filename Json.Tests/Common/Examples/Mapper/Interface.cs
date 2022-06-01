﻿using System.Linq;
using Friflo.Json.Fliox;
using Friflo.Json.Fliox.Mapper;
using NUnit.Framework;
#pragma warning disable 649 // Field 'field' is never assigned

namespace Friflo.Json.Tests.Common.Examples.Mapper
{
    public class TestInterface
    {
        [Instance(typeof(Employee))]
        interface IPerson {
        }
        
        class Employee : IPerson {
            public int employeeId;
        }

        [Test]
        public void Run() {
            string json = @"
            {
                ""employeeId"":   123
            }";
            using (var m = new ObjectMapper()) {
                var person = m.Read<IPerson>(json);

                var result = m.Write(person);
                string expect = string.Concat(json.Where(c => !char.IsWhiteSpace(c)));

                Assert.AreEqual(expect, result);
            }
        }
    }
}