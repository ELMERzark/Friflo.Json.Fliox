// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using NUnit.Framework;

// ReSharper disable MethodHasAsyncOverload
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Remote
{
    public partial class TestRemote
    {
        [Test]
        public static void Schema_errors() {
            ExecuteRestFile("Schema/errors.http", "Schema/errors.result.http");
        }

        [Test]
        public static void Schema_main_db_index() {
            var request = RestRequest("GET", "/schema/main_db/index.html");
            AssertRequest(request, 200, "text/html");
        }

        [Test]
        public static void Schema_main_db_json_schema_index() {
            var request = RestRequest("GET", "/schema/main_db/json-schema/index.html");
            AssertRequest(request, 200, "text/html");
        }
        
        [Test]
        public static void Schema_main_db_json_schema() {
            var request = RestRequest("GET", "/schema/main_db/json-schema.json");
            AssertRequest(request, 200, "application/json");
        }
        
        [Test]
        public static void Schema_main_db_json_schema_directory() {
            var request = RestRequest("GET", "/schema/main_db/json-schema/directory");
            AssertRequest(request, 200, "application/json");
        }
        
        [Test]
        public static void Schema_main_db_json_schema_zip() {
            var request = RestRequest("GET", "/schema/main_db/json-schema/PocStore.json-schema.zip");
            AssertRequest(request, 200, "application/zip");
        }
        
        [Test]
        public static void Schema_main_db_open_api() {
            var request = RestRequest("GET", "/schema/main_db/open-api.html");
            AssertRequest(request, 200, "text/html");
        }
    }
}
