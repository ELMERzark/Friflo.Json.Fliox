﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Burst;
using Friflo.Json.Flow.Graph;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Schema.JSON;
using Friflo.Json.Flow.Schema.Native;
using Friflo.Json.Flow.Schema.Validation;
using Friflo.Json.Flow.UserAuth;
using Friflo.Json.Tests.Common.UnitTest.Flow.Schema.Misc;
using Friflo.Json.Tests.Common.Utils;
using Friflo.Json.Tests.Unity.Utils;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Flow.Schema.Validation
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class TestJsonValidator : LeakTestsFixture
    {
        static readonly string JsonSchemaFolder = CommonUtils.GetBasePath() + "assets/Schema/JSON/UserStore";
        private static readonly Type[] UserStoreTypes   = { typeof(Role), typeof(UserCredential), typeof(UserPermission) };
        
        [Test]
        public static void ValidateByJsonSchema() {
            var schemas         = JsonTypeSchema.ReadSchemas(JsonSchemaFolder);
            var jsonSchema      = new JsonTypeSchema(schemas);
            using (var parser   = new Local<JsonParser>())
            using (var schema   = new ValidationSchema(jsonSchema))
            using (var validator= new JsonValidator()) {
                var roleJsonType    = SchemaTest.JsonTypeFromType (typeof(Role), "Friflo.Json.Flow.UserAuth.");
                var roleTypeDef     = jsonSchema.TypeAsTypeDef(roleJsonType);  
                var roleValidation  = schema.TypeAsValidationType(roleTypeDef);
                
                var json = "{}";
                IsTrue(validator.Validate(ref parser.value, json, roleValidation, out _));
                
                json = AsJson(@"{'id': 'role-database','description': 'test',
                    'rights': [ { 'type': 'database', 'containers': {'Article': { 'operations': ['read', 'update'], 'subscribeChanges': ['update'] }}} ]
                }");
                IsTrue(validator.Validate(ref parser.value, json, roleValidation, out _));
            }
        }
        
        // [Test]
        public static void ValidateByTypes() {
            using (var typeStore    = CreateTypeStore(UserStoreTypes))
            using (var nativeSchema = new NativeTypeSchema(typeStore))
            using (var schema       = new ValidationSchema(nativeSchema))
            using (var parser       = new Local<JsonParser>())
            using (var validator    = new JsonValidator()) {
                var roleTypeDef     = nativeSchema.TypeAsTypeDef(typeof(Role));  
                var roleValidation  = schema.TypeAsValidationType(roleTypeDef);
                
                var json = "{}";
                IsTrue(validator.Validate(ref parser.value, json, roleValidation, out _));
                
                json = AsJson(@"{'id': 'role-database','description': 'test',
                    'rights': [ { 'type': 'database', 'containers': {'Article': { 'operations': ['read', 'update'], 'subscribeChanges': ['update'] }}} ]
                }");
                IsTrue(validator.Validate(ref parser.value, json, roleValidation, out _));
            }
        }
        
        private static TypeStore CreateTypeStore (ICollection<Type> types) {
            var typeStore = EntityStore.AddTypeMatchers(new TypeStore());
            typeStore.AddMappers(types);
            return typeStore;
        } 
        
        public static string AsJson (string str) {
            return str.Replace('\'', '"');
        }
    }
}