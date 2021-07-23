﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Flow.Graph;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Mapper.Map;
using Friflo.Json.Flow.Mapper.Map.Obj.Reflect;
using Friflo.Json.Flow.Schema;
using Friflo.Json.Flow.Schema.Utils;
using Friflo.Json.Flow.Sync;
using Friflo.Json.Flow.UserAuth;
using Friflo.Json.Tests.Common.UnitTest.Flow.Graph;
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
        public static void TypescriptUserStore () {
            var typeStore = EntityStore.AddTypeMatchers(new TypeStore());
            var schema = new GeneratorSchema(typeStore, UserStoreTypes);
            var generator = schema.Typescript(null, null);
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets/Schema/Typescript/UserStore");
        }
        
        [Test]
        public static void JsonSchemaUserStore () {
            var typeStore = EntityStore.AddTypeMatchers(new TypeStore());
            var schema = new GeneratorSchema(typeStore, UserStoreTypes);
            var generator = schema.JsonSchema(null, UserStoreTypes);
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets/Schema/JSON/UserStore");
        }
        
        [Test]
        public static void TypescriptSync () {
            var typeStore = EntityStore.AddTypeMatchers(new TypeStore());
            var schema = new GeneratorSchema(typeStore, SyncTypes);
            var generator = schema.Typescript(null, null);
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets/Schema/Typescript/Sync");
        }
        
        [Test]
        public static void TypescriptPocStore () {
            // Use code generator directly
            var typeStore = EntityStore.AddTypeMatchers(new TypeStore());
            typeStore.AddMappers(PocStoreTypes);
            var typescript = new Typescript(typeStore, new[]{"Friflo.Json.Tests.Common."}, null);
            typescript.GenerateSchema();
            typescript.generator.WriteFiles(CommonUtils.GetBasePath() + "assets/Schema/Typescript/PocStore");
        }
        
        [Test]
        public static void JsonSchemaPocStore () {
            // Use code generator directly
            var typeStore = EntityStore.AddTypeMatchers(new TypeStore());
            typeStore.AddMappers(PocStoreTypes);
            var jsonSchema = new JsonSchema(typeStore, new[]{"Friflo.Json.Tests.Common."}, PocStoreTypes);
            jsonSchema.GenerateSchema();
            jsonSchema.generator.WriteFiles(CommonUtils.GetBasePath() + "assets/Schema/JSON/PocStore");
        }
        
        // ReSharper disable once UnusedParameter.Local
        private static void EnsureSymbol(string _) {}
        
        // ReSharper disable once UnusedMember.Local
        private static void EnsureApiAccess() {
            EnsureSymbol(nameof(Generator.files));
            EnsureSymbol(nameof(Generator.packages));
            EnsureSymbol(nameof(Generator.typeMappers));
            EnsureSymbol(nameof(Generator.fileExt));
            
            EnsureSymbol(nameof(EmitType.type));

            EnsureSymbol(nameof(Package.imports));
            EnsureSymbol(nameof(Package.header));
            EnsureSymbol(nameof(Package.footer));
            EnsureSymbol(nameof(Package.emitTypes));
            
            EnsureSymbol(nameof(TypeContext.generator));
            EnsureSymbol(nameof(TypeContext.imports));
            EnsureSymbol(nameof(TypeContext.owner));
            
            EnsureSymbol(nameof(PolyType.type));
            EnsureSymbol(nameof(PolyType.name));
            
            EnsureSymbol(nameof(InstanceFactory.discriminator));
            EnsureSymbol(nameof(InstanceFactory.polyTypes));
            
            EnsureSymbol(nameof(PropField.name));
            EnsureSymbol(nameof(PropField.jsonName));
            EnsureSymbol(nameof(PropField.fieldType));
            
            EnsureSymbol(nameof(TypeMapper.InstanceFactory));
            EnsureSymbol(nameof(TypeMapper.Discriminant));
            EnsureSymbol(nameof(TypeMapper.type));
            EnsureSymbol(nameof(TypeMapper.isNullable));
            EnsureSymbol(nameof(TypeMapper.nullableUnderlyingType));
            EnsureSymbol(nameof(TypeMapper.isNullable));
            EnsureSymbol(nameof(TypeMapper.IsComplex));
            EnsureSymbol(nameof(TypeMapper.IsArray));
            EnsureSymbol(nameof(TypeMapper.propFields));
            EnsureSymbol(nameof(TypeMapper.GetElementMapper));
            EnsureSymbol(nameof(TypeMapper.GetEnumValues));
            EnsureSymbol(nameof(TypeMapper.GetUnderlyingMapper));
            EnsureSymbol(nameof(TypeMapper.GetTypeSemantic));
        }
    }
}