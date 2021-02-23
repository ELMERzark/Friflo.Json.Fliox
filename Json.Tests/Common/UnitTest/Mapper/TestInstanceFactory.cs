﻿using System;
using System.Collections.Generic;
using System.Linq;
using Friflo.Json.Mapper;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Mapper
{
    public class TestInstanceFactory
    {
        // --------------- interface
        
        // missing [Instance] or [Polymorph] attribute
        interface IVehicle { }
        
        // missing [Instance] or [Polymorph] attribute
        abstract class Abstract { }

        // --- IBook
        [Instance(typeof(Book))]
        interface IBook { }

        class Book : IBook {
            public int int32;
        }



        [Test]  public void  TestInterfaceReflect()   { TestInterface(TypeAccess.Reflection); }
        [Test]  public void  TestInterfaceIL()        { TestInterface(TypeAccess.IL); }
        
        private void TestInterface(TypeAccess typeAccess) {
            var json = "{\"int32\":123}";
            using (var typeStore = new TypeStore(null, new StoreConfig(typeAccess)))
            using (var reader = new JsonReader(typeStore, JsonReader.NoThrow))
            using (var writer = new JsonWriter(typeStore))
            {
                // typeStore.AddInstanceFactory(new TestFactory());
                var result = reader.Read<IBook>(json);
                AreEqual(123, ((Book)result).int32);
                
                var jsonResult = writer.Write(result);
                AreEqual(json, jsonResult);
                
                var e = Throws<InvalidOperationException>(() => reader.Read<IVehicle>("{}"));
                AreEqual("type requires instantiatable types by [Instance()] or [Polymorph()] on: Friflo.Json.Tests.Common.UnitTest.Mapper.TestInstanceFactory+IVehicle", e.Message);
                
                e = Throws<InvalidOperationException>(() => reader.Read<Abstract>("{}"));
                AreEqual("type requires instantiatable types by [Instance()] or [Polymorph()] on: Friflo.Json.Tests.Common.UnitTest.Mapper.TestInstanceFactory+Abstract", e.Message);
            }
        }
        
        // --------------- polymorphic interface
        [JsonType (Discriminator = "animalType")]
        [Polymorph(typeof(Lion))]
        interface IAnimal {
        }

        class Lion : IAnimal {
            public int int32;
        }
        
        [Test]  public void  TestPolymorphicReflect()   { TestPolymorphic(TypeAccess.Reflection); }
        [Test]  public void  TestPolymorphicIL()        { TestPolymorphic(TypeAccess.IL); }
        
        private void TestPolymorphic(TypeAccess typeAccess) {
            var json = "{\"animalType\":\"Lion\",\"int32\":123}";
            using (var typeStore = new TypeStore(null, new StoreConfig(typeAccess)))
            using (var reader = new JsonReader(typeStore, JsonReader.NoThrow))
            using (var writer = new JsonWriter(typeStore))
            {
                // typeStore.AddInstanceFactory(new TestFactory());
                var result = reader.Read<IAnimal>(json);
                AreEqual(123, ((Lion)result).int32);
                
                var jsonResult = writer.Write(result);
                AreEqual(json, jsonResult);
                
                reader.Read<IAnimal>("{\"animalType\":\"Tiger\"}");
                StringAssert.Contains("No [Polymorph] type declared for discriminant: 'Tiger' on type: Friflo.Json.Tests.Common.UnitTest.Mapper.TestInstanceFactory+IAnimal", reader.Error.msg.ToString());
                
                reader.Read<IAnimal>("{}");
                StringAssert.Contains("Expect discriminator \"animalType\": \"...\" as first JSON member for type: Friflo.Json.Tests.Common.UnitTest.Mapper.TestInstanceFactory+IAnimal", reader.Error.msg.ToString());
            }
        }
        
        // --------------- polymorphic class
        [JsonType (Discriminator = "personType")]
        [Polymorph(typeof(Employee))]
        abstract class Person {
        }

        class Employee : Person {
            public int int32;
        }

        [Test]  public void  TestAbstractReflect()   { TestAbstract(TypeAccess.Reflection); }
        [Test]  public void  TestAbstractIL()        { TestAbstract(TypeAccess.IL); }
        
        private void TestAbstract(TypeAccess typeAccess) {
            var json = "{\"personType\":\"Employee\",\"int32\":123}";
            using (var typeStore = new TypeStore(null, new StoreConfig(typeAccess)))
            using (var reader = new JsonReader(typeStore, JsonReader.NoThrow))
            using (var writer = new JsonWriter(typeStore))
            {
                // typeStore.AddInstanceFactory(new TestFactory());
                var result = reader.Read<Person>(json);
                AreEqual(123, ((Employee)result).int32);
                
                var jsonResult = writer.Write(result);
                AreEqual(json, jsonResult);
            }
        }
        
        // ------ factory instances within collection
        
        class FactoryCollection
        {
            public List<IBook>      iTest   = new List<IBook>();
            public List<IAnimal>    animals = new List<IAnimal>();
        }
        
        [Test]  public void  TestFactoryCollectionReflect()   { TestFactoryCollection(TypeAccess.Reflection); }
        [Test]  public void  TestFactoryCollectionIL()        { TestFactoryCollection(TypeAccess.IL); }
        
        private void TestFactoryCollection(TypeAccess typeAccess) {
            var json = @"
{
    ""iTest"": [
        {
            ""int32"":123
        }
    ],
    ""animals"": [
        {
            ""animalType"":""Lion"",
            ""int32"":123
        }
    ]
}";
            string expect = string.Concat(json.Where(c => !char.IsWhiteSpace(c)));
            
            using (var typeStore = new TypeStore(null, new StoreConfig(typeAccess)))
            using (var reader = new JsonReader(typeStore, JsonReader.NoThrow))
            using (var writer = new JsonWriter(typeStore))
            {
                // typeStore.AddInstanceFactory(new TestFactory());
                var result = reader.Read<FactoryCollection>(json);
                
                var jsonResult = writer.Write(result);
                AreEqual(expect, jsonResult);
            }
        }
    }
}