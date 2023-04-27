using System.Net;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.DB.Cluster;
using Friflo.Json.Fliox.Hub.DB.Monitor;
using Friflo.Json.Fliox.Hub.Explorer;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Event;
using Friflo.Json.Fliox.Hub.Remote;
using Friflo.Json.Fliox.Hub.SQLite;
using Friflo.Json.Tests.Common.Utils;
using Friflo.Json.Tests.Provider.Client;

namespace Friflo.Json.Tests.Provider
{
    public class Program
    {
      
        public static async Task Run()
        {
            var host            = await CreateHttpHost();
            var httpListener    = new HttpListener();
            httpListener.Prefixes.Add("http://+:8011/");
            var server          = new HttpServer(httpListener, host);
            server.Start();
            server.Run();
        }
        
        private static async Task<HttpHost> CreateHttpHost() {
            var env                 = new SharedEnv();
            string      cache       = null;
            var databaseSchema      = new DatabaseSchema(typeof(TestClient));
            var fileDb              = new FileDatabase("file_db", Env.TestDbFolder) { Schema = databaseSchema };
            var memoryDb            = new MemoryDatabase("memory_db");
            await Env.Seed(memoryDb, fileDb);
            
            var hub                 = new FlioxHub(memoryDb, env);
            hub.Info.projectName    = "Test DB";
            hub.Info.projectWebsite = "https://github.com/friflo/Friflo.Json.Fliox/tree/main/Json.Tests/DB";
            hub.Info.envName        = "test"; hub.Info.envColor = "rgb(0 140 255)";
            hub.AddExtensionDB (fileDb);
            var testDb              = await Env.CreateTestDatabase("test_db", Env.TEST_DB_PROVIDER);
            if (testDb != null) {
                await Env.Seed(testDb, fileDb);
                hub.AddExtensionDB (testDb);
            }
#if !UNITY_5_3_OR_NEWER
            var sqliteDb           = new SQLiteDatabase("sqlite_db", CommonUtils.GetBasePath() + "sqlite_db.sqlite3") { Schema = databaseSchema };
            hub.AddExtensionDB (sqliteDb);
#endif
            hub.AddExtensionDB (new ClusterDB("cluster", hub));         // optional - expose info of hosted databases. Required by Hub Explorer
            hub.EventDispatcher     = new EventDispatcher(EventDispatching.QueueSend, env); // optional - enables Pub-Sub (sending events for subscriptions)
            
            var httpHost            = new HttpHost(hub, "/fliox/", env)       { CacheControl = cache };
            httpHost.AddHandler      (new StaticFileHandler(HubExplorer.Path) { CacheControl = cache }); // optional - serve static web files of Hub Explorer
            return httpHost;
        }
    }
}