﻿using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using Friflo.Fliox.Engine.Client;
using Friflo.Fliox.Engine.ECS;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Explorer;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Event;
using Friflo.Json.Fliox.Hub.Remote;

namespace Friflo.Fliox.Editor;

public class Editor
{
    private readonly    ManualResetEvent    signalEvent = new ManualResetEvent(false);
    private             EventProcessorQueue processor;
    private             HttpServer          server;

    public async Task Init()
    {
        var schema      = DatabaseSchema.Create<GameClient>();
        var database    = CreateDatabase(schema, "file-system");
        var hub         = new FlioxHub(database);
        hub.UsePubSub();    // need currently called before SetupSubscriptions()
        hub.EventDispatcher = new EventDispatcher(EventDispatching.Send);
        //
        var client      = new GameClient(hub);
        var store       = new GameEntityStore(PidType.UsePidAsId);
        var sync        = new GameDataSync(store, client);
        processor       = new EventProcessorQueue(ReceivedEvent);
        client.SetEventProcessor(processor);
        await sync.SubscribeDatabaseChangesAsync();
        
        await AddSampleEntities(sync);
        server = RunServer(hub);
    }
    
    internal void Shutdown() {
        server?.Stop();
    }
    
    internal void Run()
    {
        // simple event/game loop 
        while (signalEvent.WaitOne())
        {
            signalEvent.Reset();
            processor.ProcessEvents();
        }
    }
    
    private void ProcessEvents() {
        processor.ProcessEvents();
    }
    
    private void ReceivedEvent () {
        Dispatcher.UIThread.Post(ProcessEvents);
    }
    
    private static HttpServer RunServer(FlioxHub hub)
    {
        hub.Info.Set ("Editor", "dev", "https://github.com/friflo/Friflo.Json.Fliox/tree/main/Engine", "rgb(91,21,196)"); // optional
        hub.UseClusterDB(); // required by HubExplorer

        // --- create HttpHost
        var httpHost    = new HttpHost(hub, "/fliox/");
        httpHost.UseStaticFiles(HubExplorer.Path); // nuget: https://www.nuget.org/packages/Friflo.Json.Fliox.Hub.Explorer
    
        var server = new HttpServer ("http://localhost:5000/", httpHost);  // http://localhost:5000/fliox/
        var thread = new Thread(_ => {
            // HttpServer.RunHost("http://localhost:5000/", httpHost); // http://localhost:5000/fliox/
            server.Start();
            server.Run();
        });
        thread.Start();
        return server;
    }
    
    private static EntityDatabase CreateDatabase(DatabaseSchema schema, string provider)
    {
        if (provider == "file-system") {
            var directory = Directory.GetCurrentDirectory() + "/DB";
            return new FileDatabase("game", directory, schema) { Pretty = false };
        }
        return new MemoryDatabase("game", schema) { Pretty = false };
    }
        
    private static async Task AddSampleEntities(GameDataSync sync)
    {
        var store   = sync.Store;
        var root    = store.CreateEntity(1);
        root.AddComponent(new Position(1, 1, 1));
        root.AddComponent(new EntityName("root"));
        var child   = store.CreateEntity(2);
        child.AddComponent(new Position(2, 2, 2));
        root.AddChild(child);
        await sync.StoreGameEntitiesAsync();
    }
}