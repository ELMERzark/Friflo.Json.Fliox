﻿using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Remote;
using Friflo.Json.Tests.Common.UnitTest.Fliox.Client.Happy;
using Friflo.Json.Tests.Common.UnitTest.Fliox.Hubs;

namespace Friflo.Json.Tests.Main
{
    public static class Throughput
    {
        public static async Task MemoryDbThroughput() {
            var database    = new MemoryDatabase();
            var hub         = new FlioxHub(database, SharedHost.Instance);
            await TestStore.ConcurrentAccess(hub, 4, 0, 1_000_000, false);
        }
        
        public static async Task FileDbThroughput() {
            var database    = new FileDatabase("./Json.Tests/assets~/DB/testConcurrencyDb");
            var hub         = new FlioxHub(database, SharedHost.Instance);
            await TestStore.ConcurrentAccess(hub, 4, 0, 1_000_000, false);
        }
        
        public static async Task WebsocketDbThroughput() {
            using (var database         = new MemoryDatabase())
            using (var hub          	= new FlioxHub(database, SharedHost.Instance))
            using (var hostHub          = new HttpHostHub(hub))
            using (var server           = new HttpListenerHost("http://+:8080/", hostHub))
            using (var remoteHub        = new WebSocketClientHub("ws://localhost:8080/", SharedHost.Instance)) {
                await TestStore.RunServer(server, async () => {
                    await remoteHub.Connect();
                    await TestStore.ConcurrentAccess(remoteHub, 4, 0, 1_000_000, false);
                });
            }
        }
        
        public static async Task HttpDbThroughput() {
            using (var database         = new MemoryDatabase())
            using (var hub          	= new FlioxHub(database, SharedHost.Instance))
            using (var hostHub          = new HttpHostHub(hub))
            using (var server           = new HttpListenerHost("http://+:8080/", hostHub))
            using (var remoteDatabase   = new HttpClientHub("ws://localhost:8080/", SharedHost.Instance)) {
                await TestStore.RunServer(server, async () => {
                    await TestStore.ConcurrentAccess(remoteDatabase, 4, 0, 1_000_000, false);
                });
            }
        }
        
        public static async Task LoopbackDbThroughput() {
            var database                = new MemoryDatabase();
            using (var hub          	= new FlioxHub(database, SharedHost.Instance))
            using (var loopbackHub      = new LoopbackHub(hub)) {
                await TestStore.ConcurrentAccess(loopbackHub, 4, 0, 1_000_000, false);
            }
        }
    }
}
