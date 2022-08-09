﻿using System.Linq;
using Friflo.Json.Fliox;
using Friflo.Json.Fliox.Mapper;
using NUnit.Framework;
#pragma warning disable 649 // Field 'field' is never assigned

namespace Friflo.Json.Tests.Common.Examples.Mapper
{
    public class TestPolymorphClass
    {
        [Discriminator("vehicleType")]
        [PolymorphType(typeof(Car),     Discriminant = "car")]
        [PolymorphType(typeof(Bike),    Discriminant = "bike")]
        class Vehicle {
        }
        
        class Car : Vehicle {
            public int  seatCount;
        }
        
        class Bike : Vehicle {
            public bool hasLuggageRack;
        }

        
        [Test]
        public void Run() {
            string json = @"
            [
                {
                    ""vehicleType"":    ""car"",
                    ""seatCount"":      4
                },
                {
                    ""vehicleType"":    ""bike"",
                    ""hasLuggageRack"": true
                }
            ]";
            using (var m = new ObjectMapper()) {
                var vehicles = m.Read<Vehicle[]>(json);

                var result = m.Write(vehicles);
                string expect = string.Concat(json.Where(c => !char.IsWhiteSpace(c)));

                Assert.AreEqual(expect, result);
            }
        }
    }
}