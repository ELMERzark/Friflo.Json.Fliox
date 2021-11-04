﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Schema.Native;
using Friflo.Json.Fliox.Transform;
using Friflo.Json.Tests.Common.UnitTest.Fliox.Hubs;
using Friflo.Json.Tests.Common.Utils;
using UnityEngine.TestTools;
using static NUnit.Framework.Assert;

#if UNITY_5_3_OR_NEWER
    using UnitTest.Dummy;
#else
    using NUnit.Framework;
#endif

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Client.Errors
{
    public partial class TestErrors
    {
        [UnityTest] public IEnumerator FileValidationCoroutine() { yield return RunAsync.Await(FileValidation(), i => Logger.Info("--- " + i)); }
        [Test]      public async Task  FileValidationAsync() { await FileValidation(); }

        private static async Task FileValidation() {
            using (var _                = DefaultSharedEnv.Instance) // for LeakTestsFixture
            using (var database         = new FileDatabase(TestGlobals.PocStoreFolder))
            using (var hub          	= new FlioxHub(database, TestGlobals.Shared))
            using (var modifierHub      = new WriteModifierHub(hub))
            using (var createStore      = new PocStore(modifierHub) { UserId = "createStore" })
            using (var nativeSchema     = new NativeTypeSchema(typeof(PocStore)))
            using (database.Schema  = new DatabaseSchema(nativeSchema)) {
                await AssertValidation(createStore, modifierHub);
            }
        }
        
        private static async Task AssertValidation(PocStore store, WriteModifierHub modifierHub) {
            modifierHub.ClearErrors();
            var articles = store.articles;
            
            var articleModifier = modifierHub.GetWriteModifiers(nameof(PocStore.articles));
            articleModifier.writes.Add("article-missing-id",     val => new JsonValue("{\"id\": \"article-missing-id\" }"));
            articleModifier.writes.Add("article-incorrect-type", val => new JsonValue(val.AsString().Replace("\"xxx\"", "123")));

            var articleMissingName      = new Article { id = "article-missing-name" };
            var articleMissingId        = new Article { id = "article-missing-id" };
            var articleIncorrectType    = new Article { id = "article-incorrect-type", name = "xxx"};
            
            // --- test validation errors for creates
            var createTask = articles.CreateRange(new [] { articleMissingName, articleMissingId, articleIncorrectType});
            
            var sync = await store.TrySyncTasks(); // ----------------
            
            IsFalse(sync.Success);
            IsFalse(createTask.Success);
            var errors = createTask.Error.entityErrors;
            AreEqual("Missing required fields: [name] at Article > (root), pos: 45", errors[new JsonKey("article-missing-name")].message);
            const string expectError = @"EntityErrors ~ count: 3
| WriteError: articles [article-incorrect-type], Incorrect type. was: 123, expect: string at Article > name, pos: 41
| WriteError: articles [article-missing-id], Missing required fields: [name] at Article > (root), pos: 29
| WriteError: articles [article-missing-name], Missing required fields: [name] at Article > (root), pos: 45";
            AreEqual(expectError, createTask.Error.Message);
            
            // --- test validation errors for upserts
            var upsertTask = articles.UpsertRange(new [] { articleMissingName, articleMissingId, articleIncorrectType});
            
            sync = await store.TrySyncTasks(); // ----------------
            
            IsFalse(sync.Success);
            IsFalse(upsertTask.Success);
            errors = upsertTask.Error.entityErrors;
            AreEqual("Missing required fields: [name] at Article > (root), pos: 45", errors[new JsonKey("article-missing-name")].message);
            AreEqual(expectError, upsertTask.Error.Message);
            
            
            // test validation errors for patches
            var patchModifier = modifierHub.GetPatchModifiers(nameof(PocStore.articles));
            patchModifier.patches.Add("article-2", patch => {
                var replace = (PatchReplace)patch.patches[0];
                replace.value = new JsonValue("123");
                return patch;
            });
            var articlePatch    = new Article { id = "article-2", name = "changed name"};
            var patchArticle    = articles.Patch(articlePatch);
            patchArticle.Member(a => a.name);
            
            sync = await store.TrySyncTasks(); // ----------------
            
            IsFalse(patchArticle.Success);
            AreEqual(@"EntityErrors ~ count: 1
| PatchError: articles [article-2], Incorrect type. was: 123, expect: string at Article > name, pos: 40", patchArticle.Error.Message);


            // --- test validation errors for invalid JSON
            articleModifier.writes.Add("invalid-json",       val => new JsonValue("X"));
            // articleModifier.writes.Add("empty-json",         val => new EntityValue(""));
            
            var invalidJson     = new Article { id = "invalid-json" };
            // var emptyJson       = new Article { id = "empty-json" };

            createTask = articles.CreateRange(new [] { invalidJson });
            
            await store.TrySyncTasks(); // ----------------
            
            IsFalse(sync.Success);
            IsFalse(createTask.Success);
            AreEqual("InvalidTask ~ error at entities[0]: entity value must be an object.", createTask.Error.Message);

        }
    }
}