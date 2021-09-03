﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Fliox.Db.Graph;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Schema.JSON;
using Friflo.Json.Fliox.Schema.Native;
using Friflo.Json.Fliox.Schema.Validation;
using Friflo.Json.Tests.Common.UnitTest.Fliox.Graph;
using Friflo.Json.Tests.Common.Utils;
using Friflo.Json.Tests.Unity.Utils;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Schema.Validation
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class EntityIdStoreValidation : LeakTestsFixture
    {
        private static readonly string  JsonSchemaFolder    = CommonUtils.GetBasePath() + "assets~/Schema/JSON/EntityIdStore";
        private static readonly Type[]  EntityIdStoreTypes  = EntityStore.GetEntityTypes<EntityIdStore>();
        
        [Test]
        public static void ValidateByJsonSchema() {
            var schemas                 = JsonTypeSchema.ReadSchemas(JsonSchemaFolder);
            var jsonSchema              = new JsonTypeSchema(schemas);
            using (var validationSet    = new ValidationSet(jsonSchema))
            using (var validator        = new TypeValidator()) {
                var test = new TestTypes {
                    guidEntityType    = jsonSchema.TypeAsValidationType<GuidEntity>(validationSet, "UnitTest.Fliox.Graph")
                };
                ValidateSuccess(validator, test);
                ValidateFailure(validator, test);
            }
        }
        
        [Test]
        public static void ValidateByTypes() {
            using (var typeStore        = CreateTypeStore(EntityIdStoreTypes))
            using (var nativeSchema     = new NativeTypeSchema(typeStore))
            using (var validationSet    = new ValidationSet(nativeSchema))
            using (var validator        = new TypeValidator()) {
                var test = new TestTypes {
                    guidEntityType    = nativeSchema.TypeAsValidationType<GuidEntity>(validationSet)
                };
                ValidateSuccess(validator, test);
                ValidateFailure(validator, test);
            }
        }
        
        private static void ValidateSuccess(TypeValidator validator, TestTypes test)
        {
            var json = "{ \"id\": \"12345678-1234-1234-1234-1234567890ab\"}";
            IsTrue(validator.ValidateObject(json,           test.guidEntityType, out _));
        }
        
        private static void ValidateFailure(TypeValidator validator, TestTypes test)
        {
            var json = "{ \"id\": \"X2345678-1234-1234-1234-1234567890ab\"}";
            IsFalse(validator.ValidateObject(json,          test.guidEntityType, out string error));
            AreEqual("Invalid Guid: 'X2345678-1234-1234-1234-1234567890ab' at GuidEntity > id, pos: 46", error);
            
            json = "{ \"id\": \"1234567-1234-1234-1234-1234567890ab\"}";
            IsFalse(validator.ValidateObject(json,          test.guidEntityType, out error));
            AreEqual("Invalid Guid: '1234567-1234-1234-1234-1234567890ab' at GuidEntity > id, pos: 45", error);
        }
        
        private class TestTypes {
            internal    ValidationType  guidEntityType;
        }

        // --- helper
        private static TypeStore CreateTypeStore (ICollection<Type> types) {
            var typeStore = new TypeStore();
            typeStore.AddMappers(types);
            return typeStore;
        }
    }
}