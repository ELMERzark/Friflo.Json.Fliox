﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.DB.Host;
using Friflo.Json.Fliox.DB.Host.Monitor;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Tests.Common.UnitTest.Fliox.Client.Happy;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Client
{
    public static class TestGlobals {
        public static TypeStore typeStore;
        
        public static void Init() {
            HostTypeStore.Init();
            // LeakTestsFixture requires to register all types used by TypeStore before leak tracking starts
            typeStore = new TypeStore();
            RegisterTypeMatcher(typeStore);
            RegisterTypeMatcher(JsonDebug.DebugTypeStore);
        }
        
        private static void RegisterTypeMatcher(TypeStore typeStore) {
            typeStore.GetTypeMapper(typeof(TestMessage));
            
            // create all TypeMappers required by PocStore model classes before leak tracking of LeakTestsFixture starts.
            typeStore.GetTypeMapper(typeof(PocStore));
            typeStore.GetTypeMapper(typeof(PocEntity)); // todo necessary?
            typeStore.GetTypeMapper(typeof(SimpleStore));
            typeStore.GetTypeMapper(typeof(MonitorStore));
        }
        
        public static void Dispose() {
            typeStore.Dispose();
            typeStore = null;
            HostTypeStore.Dispose();
        }
    }
}