﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Flow.Graph;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Schema;
using Friflo.Json.Flow.Schema.Native;
using Friflo.Json.Flow.Sync;
using Friflo.Json.Flow.UserAuth;
using Friflo.Json.Tests.Common.UnitTest.Flow.Graph;
using Friflo.Json.Tests.Common.UnitTest.Flow.Schema.lab;
using Friflo.Json.Tests.Common.Utils;
using NUnit.Framework;

namespace Friflo.Json.Tests.Common.UnitTest.Flow.Schema
{
    public static class GenerateSchema
    {
        private static readonly Type[] UserStoreTypes   = { typeof(Role), typeof(UserCredential), typeof(UserPermission) };
        private static readonly Type[] SyncTypes        = { typeof(DatabaseMessage) };
        private static readonly Type[] PocStoreTypes    = { typeof(Order), typeof(Customer), typeof(Article), typeof(Producer), typeof(Employee), typeof(TestType) };

        [Test]
        public static void Typescript_UserStore () {
            var typeStore = EntityStore.AddTypeMatchers(new TypeStore());
            var generator = Typescript.Generate(typeStore, UserStoreTypes);
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets/Schema/Typescript/UserStore");
        }
        
        [Test]
        public static void JsonSchema_UserStore () {
            var typeStore = EntityStore.AddTypeMatchers(new TypeStore());
            var generator = JsonSchema.Generate(typeStore, UserStoreTypes, null, UserStoreTypes);
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets/Schema/JSON/UserStore");
        }
        
        [Test]
        public static void JTD_UserStore () {
            var typeStore = EntityStore.AddTypeMatchers(new TypeStore());
            var generator = JsonTypeDefinition.Generate(typeStore, UserStoreTypes, "UserStore", null, UserStoreTypes);
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets/Schema/JTD/", false);
        }
        
        [Test]
        public static void Typescript_Sync () {
            var typeStore = EntityStore.AddTypeMatchers(new TypeStore());
            var generator = Typescript.Generate(typeStore, SyncTypes);
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets/Schema/Typescript/Sync");
        }
        
        [Test]
        public static void JTD_Sync () {
            var typeStore = EntityStore.AddTypeMatchers(new TypeStore());
            var generator = JsonTypeDefinition.Generate(typeStore, SyncTypes, "Sync");
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets/Schema/JTD/", false);
        }
        
        [Test]
        public static void JTD_PocStore () {
            var typeStore = EntityStore.AddTypeMatchers(new TypeStore());
            var generator = JsonTypeDefinition.Generate(typeStore, PocStoreTypes, "PocStore");
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets/Schema/JTD/", false);
        }
        
        [Test]
        public static void Typescript_PocStore () {
            // Use code generator directly
            var typeStore = EntityStore.AddTypeMatchers(new TypeStore());
            typeStore.AddMappers(PocStoreTypes);
            var schema      = new NativeTypeSchema(typeStore);
            var generator   = new Generator(schema, new[]{"Friflo.Json.Tests.Common."}, ".ts");
            var _           = new Typescript(generator);
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets/Schema/Typescript/PocStore");
        }
        
        [Test]
        public static void JsonSchema_PocStore () {
            // Use code generator directly
            var typeStore = EntityStore.AddTypeMatchers(new TypeStore());
            typeStore.AddMappers(PocStoreTypes);
            var schema      = new NativeTypeSchema(typeStore, PocStoreTypes);
            var generator   = new Generator(schema, new[]{"Friflo.Json.Tests.Common."}, ".json");
            var _           = new JsonSchema(generator);
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets/Schema/JSON/PocStore");
        }
    }
}