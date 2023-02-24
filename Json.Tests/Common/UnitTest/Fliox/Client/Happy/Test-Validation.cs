﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Schema.JSON;
using Friflo.Json.Fliox.Schema.Native;
using Friflo.Json.Tests.Common.Utils;
using UnityEngine.TestTools;

#if UNITY_5_3_OR_NEWER
    using UnitTest.Dummy;
#else
    using NUnit.Framework;
#endif

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Client.Happy
{
    public partial class TestHappy
    {
        [UnityTest] public IEnumerator ValidationByTypesCoroutine() { yield return RunAsync.Await(ValidationByTypes(), i => Logger.Info("--- " + i)); }
        [Test]      public async Task  ValidationByTypesAsync() { await ValidationByTypes(); }

        private static async Task ValidationByTypes() {
            using (var _                = SharedEnv.Default) // for LeakTestsFixture
            using (var database         = new FileDatabase(TestGlobals.DB, TestGlobals.PocStoreFolder, new PocService()))
            using (var hub          	= new FlioxHub(database, TestGlobals.Shared))
            using (var createStore      = new PocStore(hub) { UserId = "createStore"}) {
                database.Schema     = new DatabaseSchema(typeof(PocStore)); 
                // All write operation performed in following call produce JSON payload which meet the types defined
                // in the assigned schema => the call succeed without any validation error. 
                await TestRelationPoC.CreateStore(createStore);
            }
        }
        
        [UnityTest] public IEnumerator ValidationByJsonSchemaCoroutine() { yield return RunAsync.Await(ValidationByJsonSchema(), i => Logger.Info("--- " + i)); }
        [Test]      public async Task  ValidationByJsonSchemaAsync() { await ValidationByJsonSchema(); }

        private static async Task ValidationByJsonSchema() {
            var jsonSchemaFolder    = CommonUtils.GetBasePath() + "assets~/Schema/JSON/PocStore";
            var schemas             = JsonTypeSchema.ReadSchemas(jsonSchemaFolder);
            using (var _            = SharedEnv.Default) // for LeakTestsFixture
            using (var database     = new FileDatabase(TestGlobals.DB, TestGlobals.PocStoreFolder, new PocService()))
            using (var hub          = new FlioxHub(database, TestGlobals.Shared))
            using (var createStore  = new PocStore(hub) { UserId = "createStore"})
            using (var jsonSchema   = new JsonTypeSchema(schemas, "./UnitTest.Fliox.Client.json#/definitions/PocStore")) {
                database.Schema  = new DatabaseSchema(jsonSchema); 
                // All write operation performed in following call produce JSON payload which meet the types defined
                // in the assigned schema => the call succeed without any validation error. 
                await TestRelationPoC.CreateStore(createStore);
            }
        }
    }
}