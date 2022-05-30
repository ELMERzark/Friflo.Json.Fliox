﻿using Friflo.Json.Fliox.Hub.DB.Cluster;
using Friflo.Json.Fliox.Hub.DB.Monitor;
using Friflo.Json.Fliox.Hub.DB.UserAuth;
using Friflo.Json.Fliox.Hub.GraphQL;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Event;
using Friflo.Json.Fliox.Hub.Remote;
using Friflo.Json.Fliox.Schema.Definition;
using Friflo.Json.Fliox.Schema.JSON;
using Friflo.Json.Fliox.Schema.Native;
using Friflo.Json.Tests.Common.UnitTest.Fliox.Client;
using Friflo.Json.Tests.Common.UnitTest.Fliox.Schema.Misc;

// ReSharper disable UseObjectOrCollectionInitializer
namespace Friflo.Json.Tests.Main
{
    internal  static partial class  Program
    {
        // Example requests for server at: /Json.Tests/www~/example-requests/
        //
        //   Note:
        // Http server may require a permission to listen to the given host/port.
        // Otherwise exception is thrown on startup: System.Net.HttpListenerException: permission denied.
        // To give access see: [add urlacl - Win32 apps | Microsoft Docs] https://docs.microsoft.com/en-us/windows/win32/http/add-urlacl
        //     netsh http add urlacl url=http://+:8010/ user=<DOMAIN>\<USER> listen=yes
        //     netsh http delete urlacl http://+:8010/
        // 
        // Get DOMAIN\USER via  PowerShell
        //     $env:UserName
        //     $env:UserDomain 
        private static void FlioxServer(string endpoint) {
            var hostHub = CreateHttpHost(new Config());
        //  var hostHub = CreateMiniHost();
            var server = new HttpListenerHost(endpoint, hostHub);
            server.Start();
            server.Run();
        }
        
        /// <summary>
        /// Blueprint method showing how to setup a <see cref="HttpHost"/> utilizing all features available
        /// via HTTP and WebSockets.
        /// </summary>
        public static HttpHost CreateHttpHost(Config c) {
            var typeSchema          = NativeTypeSchema.Create(typeof(PocStore)); // optional - create TypeSchema from Type 
        //  var typeSchema          = CreateTypeSchema();               // alternatively create TypeSchema from JSON Schema
            var databaseSchema      = new DatabaseSchema(typeSchema);
            var database            = CreateDatabase(c, databaseSchema, new PocHandler());
            
            var hub                 = new FlioxHub(database, c.env);
            hub.Info.projectName    = "Test Hub";                                                               // optional
            hub.Info.projectWebsite = "https://github.com/friflo/Friflo.Json.Fliox/tree/main/Json.Tests/Main";  // optional
            hub.Info.envName        = "dev"; hub.Info.envColor = "rgb(34 140 0)";                               // optional
            hub.AddExtensionDB (new ClusterDB("cluster", hub));     // optional - expose info of hosted databases. Required by Hub Explorer
            hub.AddExtensionDB (new MonitorDB("monitor", hub));     // optional - expose monitor stats as extension database
            hub.EventDispatcher     = new EventDispatcher(true, c.env); // optional - enables sending events for subscriptions
            
            var userDB              = new FileDatabase("user_db", c.UserDbPath, new UserDBHandler(), null, false);
            hub.Authenticator       = new UserAuthenticator(userDB, c.env).SubscribeUserDbChanges(hub.EventDispatcher);    // optional - otherwise all request tasks are authorized
            hub.AddExtensionDB(userDB);                             // optional - expose userStore as extension database
            
            var httpHost            = new HttpHost(hub, "/fliox/", c.env).CacheControl(c.cache);
            httpHost.AddHandler      (new GraphQLHandler());
            httpHost.AddHandler      (new StaticFileHandler(c.Www).CacheControl(c.cache)); // optional - serve static web files of Hub Explorer
            httpHost.AddSchemaGenerator("jtd", "JSON Type Definition", JsonTypeDefinition.GenerateJTD);  // optional - add code generator
            return httpHost;
        }
        
        public class Config {
            internal readonly   SharedEnv   env; 
            private  readonly   string      rootPath;
            internal            string      DbPath      => rootPath + "./Json.Tests/assets~/DB/PocStore";
            internal            string      UserDbPath  => rootPath + "./Json.Tests/assets~/DB/UserStore";
            internal            string      Www         => rootPath + "./Json/Fliox.Hub.Explorer/www~"; // HubExplorer.Path;
            internal readonly   string      cache       = null; // "max-age=600"; // HTTP Cache-Control
            internal readonly   bool        useMemoryDb;
            internal readonly   MemoryType  memoryType  = MemoryType.Concurrent;
            
            internal Config() { }
            internal Config(SharedEnv env, string rootPath, bool useMemoryDb, MemoryType memoryType, string cache) {
                this.env = env; this.rootPath = rootPath; this.useMemoryDb = useMemoryDb; this.memoryType = memoryType; this.cache = cache;
            }
        }
        
        private static HttpHost CreateMiniHost() {
            var c                   = new Config();
            // Run a minimal Fliox server without monitoring, messaging, Pub-Sub, user authentication / authorization & entity validation
            var database            = CreateDatabase(c, null, new PocHandler());
            var hub          	    = new FlioxHub(database);
            var httpHost            = new HttpHost(hub, "/fliox/");
            httpHost.AddHandler      (new StaticFileHandler(c.Www, c.cache));   // optional - serve static web files of Hub Explorer
            return httpHost;
        }
        
        private static EntityDatabase CreateDatabase(Config c, DatabaseSchema schema, TaskHandler handler) {
            var fileDb = new FileDatabase("main_db", c.DbPath, handler, null, false);
            fileDb.Schema = schema;
            if (!c.useMemoryDb)
                return fileDb;
            var memoryDB = new MemoryDatabase("main_db", handler, c.memoryType);
            memoryDB.Schema = schema;
            memoryDB.SeedDatabase(fileDb).Wait();
            return memoryDB;
        }
        
        private static TypeSchema CreateTypeSchema() {
            var schemas = JsonTypeSchema.ReadSchemas("./Json.Tests/assets~/Schema/JSON/PocStore");
            return new JsonTypeSchema(schemas, "./UnitTest.Fliox.Client.json#/definitions/PocStore");
        }
    }
}
