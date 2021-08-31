﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Graph;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Sync;
using Friflo.Json.Tests.Common.UnitTest.Fliox.Graph.Happy;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Graph
{
    public static class TestGlobals {
        public static TypeStore typeStore;
        
        public static void Init() {
            SyncTypeStore.Init();
            // LeakTestsFixture requires to register all types used by TypeStore before leak tracking starts
            typeStore = new TypeStore();
            RegisterTypeMatcher(typeStore);
            RegisterTypeMatcher(JsonDebug.DebugTypeStore);
            // force instantiation of ObjectWriter before leak tracking starts. Otherwise debugger does instantiation
            // when using JsonDebug.ToJson() in a ToString() override resulting in a false positive leak.
            JsonDebug.Init();
        }
        
        private static void RegisterTypeMatcher(TypeStore typeStore) {
            typeStore.GetTypeMapper(typeof(TestMessage));
            
            // create all TypeMappers required by PocStore model classes before leak tracking of LeakTestsFixture starts.
            EntityStore.AddTypeMatchers(typeStore);
            typeStore.GetTypeMapper(typeof(PocStore));
            typeStore.GetTypeMapper(typeof(PocEntity)); // todo necessary?
            typeStore.GetTypeMapper(typeof(SimpleStore));
        }
        
        public static void Dispose() {
            typeStore.Dispose();
            typeStore = null;
            SyncTypeStore.Dispose();
        }
    }
}