﻿#if !UNITY_2020_1_OR_NEWER

using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.Host;
using Friflo.Json.Fliox.DB.Remote;
using Friflo.Json.Tests.Common.UnitTest.Fliox.Client;
using Friflo.Json.Tests.Common.UnitTest.Fliox.Client.Happy;

namespace Friflo.Json.Tests.Main
{
    public static class Throughput
    {
        public static async Task MemoryDbThroughput() {
            var db = new MemoryDatabase();
            await TestStore.ConcurrentAccess(db, 4, 0, 1_000_000, false);
        }
        
        public static async Task FileDbThroughput() {
            var db = new FileDatabase("./Json.Tests/assets~/DB/testConcurrencyDb");
            await TestStore.ConcurrentAccess(db, 4, 0, 1_000_000, false);
        }
        
        public static async Task WebsocketDbThroughput() {
            var db = new MemoryDatabase();
            using (var hostDatabase     = new HttpListenerHostDatabase(db, "http://+:8080/"))
            using (var remoteDatabase   = new WebSocketClientDatabase("ws://localhost:8080/")) {
                await TestStore.RunRemoteHost(hostDatabase, async () => {
                    await remoteDatabase.Connect();
                    await TestStore.ConcurrentAccess(remoteDatabase, 4, 0, 1_000_000, false);
                });
            }
        }
        
        public static async Task HttpDbThroughput() {
            var db = new MemoryDatabase();
            using (var hostDatabase     = new HttpListenerHostDatabase(db, "http://+:8080/"))
            using (var remoteDatabase   = new HttpClientDatabase("ws://localhost:8080/")) {
                await TestStore.RunRemoteHost(hostDatabase, async () => {
                    await TestStore.ConcurrentAccess(remoteDatabase, 4, 0, 1_000_000, false);
                });
            }
        }
        
        public static async Task LoopbackDbThroughput() {
            var db = new MemoryDatabase();
            using (var loopbackDatabase     = new LoopbackDatabase(db)) {
                await TestStore.ConcurrentAccess(loopbackDatabase, 4, 0, 1_000_000, false);
            }
        }
    }
}

#endif