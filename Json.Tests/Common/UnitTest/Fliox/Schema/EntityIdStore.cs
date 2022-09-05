﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Schema.JSON;
using Friflo.Json.Fliox.Schema.Language;
using Friflo.Json.Fliox.Schema.Native;
using Friflo.Json.Tests.Common.UnitTest.Fliox.Client;
using Friflo.Json.Tests.Common.UnitTest.Fliox.Schema.Misc;
using Friflo.Json.Tests.Common.Utils;
using NUnit.Framework;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Schema
{
    public static class EntityIdStoreGen
    {
        private static readonly Type[] EntityIdStoreTypes      = FlioxClient.GetEntityTypes<EntityIdStore>();

        // -------------------------------------- input: C# --------------------------------------
        /// C# -> Typescript
        [Test]
        public static void CS_Typescript () {
            // Use code generator directly
            var schema      = NativeTypeSchema.Create(typeof(EntityIdStore));
            var generator   = new Generator(schema, ".d.ts", new[]{new Replace("Friflo.Json.Tests.Common.")});
            TypescriptGenerator.Generate(generator);
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets~/Schema/Typescript/EntityIdStore");
        }
        
        /// C# -> JSON Schema
        [Test, Order(1)]
        public static void CS_JSON () {
            var options     = new NativeTypeOptions(typeof(EntityIdStore)) {
                separateTypes = EntityIdStoreTypes,
                replacements = new [] {new Replace("Friflo.Json.Tests.Common.")}
            };
            var generator   = JsonSchemaGenerator.Generate(options);
            generator.WriteFiles(JsonSchemaFolder);
        }
        
        /// C# -> C#
        [Test]
        public static void CS_CS () {
            var options     = new NativeTypeOptions(typeof(EntityIdStore)) { // call constructor with two params
                replacements = new [] {
                    new Replace("Friflo.Json.Tests.Common.UnitTest.Fliox",  "EntityIdStore2"),
                    new Replace("Friflo.Json.Fliox",                        "EntityIdStore2")
                }
            };
            var generator = CSharpGenerator.Generate(options);
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets~/Schema/C#/EntityIdStore2");
        }
        
        /// C# -> Kotlin
        [Test]
        public static void CS_Kotlin () {
            var options     = new NativeTypeOptions(typeof(EntityIdStore)) {
                replacements = new [] {
                    new Replace("Friflo.Json.Tests.Common.UnitTest.Fliox",   "EntityIdStore") }
            };
            var generator = KotlinGenerator.Generate(options);
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets~/Schema/Kotlin/src/main/kotlin/EntityIdStore");
        }

        
        // ---------------------------------- input: JSON Schema ----------------------------------
        
        static readonly string JsonSchemaFolder = CommonUtils.GetBasePath() + "assets~/Schema/JSON/EntityIdStore";
        
        /// JSON Schema -> JSON Schema
        [Test, Order(2)]
        public static void JSON_JSON () {
            var schemas     = JsonTypeSchema.ReadSchemas(JsonSchemaFolder);
            var schema      = new JsonTypeSchema(schemas, "./UnitTest.Fliox.Client.json#/definitions/EntityIdStore");
            var entityTypes = schema.GetEntityTypes().Values;
            var options     = new JsonTypeOptions(schema) { separateTypes = entityTypes };
            var generator   = JsonSchemaGenerator.Generate(options);
            
            var loopFolder  = CommonUtils.GetBasePath() + "assets~/Schema-Loop/JSON/EntityIdStore";
            generator.WriteFiles(loopFolder, false);
            SchemaTest.AssertFoldersAreEqual(JsonSchemaFolder, loopFolder);
        }
    }
}