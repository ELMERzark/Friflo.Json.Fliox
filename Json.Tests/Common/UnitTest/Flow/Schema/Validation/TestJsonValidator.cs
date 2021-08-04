﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Flow.Graph;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Schema.JSON;
using Friflo.Json.Flow.Schema.Native;
using Friflo.Json.Flow.Schema.Validation;
using Friflo.Json.Flow.UserAuth;
using Friflo.Json.Tests.Common.Utils;
using Friflo.Json.Tests.Unity.Utils;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Flow.Schema.Validation
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class TestJsonValidator : LeakTestsFixture
    {
        static readonly         string  JsonSchemaFolder = CommonUtils.GetBasePath() + "assets/Schema/JSON/UserStore";
        private static readonly Type[]  UserStoreTypes   = { typeof(Role), typeof(UserCredential), typeof(UserPermission) };
        
        [Test]
        public static void ValidateByJsonSchema() {
            var schemas                 = JsonTypeSchema.ReadSchemas(JsonSchemaFolder);
            var jsonSchema              = new JsonTypeSchema(schemas);
            using (var validationSchema = new ValidationSchema(jsonSchema))
            using (var validator        = new JsonValidator()) {
                var types = new TestTypes {
                    roleType    = jsonSchema.TypeAsValidationType<Role>(validationSchema, "Friflo.Json.Flow.UserAuth.")
                };
                Validate(validator, types);
            }
        }
        
        [Test]
        public static void ValidateByTypes() {
            using (var typeStore        = CreateTypeStore(UserStoreTypes))
            using (var nativeSchema     = new NativeTypeSchema(typeStore))
            using (var validationSchema = new ValidationSchema(nativeSchema))
            using (var validator        = new JsonValidator()) {
                var types = new TestTypes {
                    roleType    = nativeSchema.TypeAsValidationType<Role>(validationSchema)
                };
                Validate(validator, types);
            }
        }
        
        private static void Validate(JsonValidator validator, TestTypes test) {
            IsTrue(validator.ValidateObject     ("{}",              test.roleType, out _));
            IsTrue(validator.ValidateArray      ("[]",              test.roleType, out _));
            IsTrue(validator.ValidateObjectMap  ("{\"key\": {}}",   test.roleType, out _));

            IsTrue(validator.ValidateObject     (test.roleValid,    test.roleType, out _));
        }
        
        private class TestTypes {
            internal    ValidationType  roleType;
            internal    readonly string roleValid = AsJson(
                @"{'id': 'role-database','description': 'test',
                    'rights': [ { 'type': 'database', 'containers': {'Article': { 'operations': ['read', 'update'], 'subscribeChanges': ['update'] }}} ]
                }");
        }

        // --- helper
        private static TypeStore CreateTypeStore (ICollection<Type> types) {
            var typeStore = EntityStore.AddTypeMatchers(new TypeStore());
            typeStore.AddMappers(types);
            return typeStore;
        } 
        
        private static string AsJson (string str) {
            return str.Replace('\'', '"');
        }
    }
}