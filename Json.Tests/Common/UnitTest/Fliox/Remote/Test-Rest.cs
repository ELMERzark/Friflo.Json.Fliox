// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using NUnit.Framework;

// ReSharper disable MethodHasAsyncOverload
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Remote
{
    public partial class TestRemote
    {
        /// <summary>
        /// use name <see cref="Rest__main_db_init"/> to run as first test.
        /// This forces loading all required RestHandler code and subsequent tests show the real execution time
        /// </summary>
        [Test, Order(0)]
        public static void Rest__main_db_init() {
            var request = HttpRequest("POST", "/rest/main_db/", "?command=std.Echo");
            AssertRequest(request, 200, "application/json", "null");
        }
        
        [Test, Order(1)]
        public static void Rest_main_db_happy_read() {
            ExecuteHttpFile("Rest/main_db/happy-read.http", "Rest/main_db/happy-read.result.http");
        }
        
        [Test, Order(1)]
        public static void Rest_main_db_handler_errors() {
            ExecuteHttpFile("Rest/main_db/handler-errors.http", "Rest/main_db/handler-errors.result.http");
        }
        
        [Test, Order(1)]
        public static void Rest_main_db_task_coverage() {
            ExecuteHttpFile("Rest/main_db/task-coverage.http", "Rest/main_db/task-coverage.result.http");
        }
        
        [Test, Order(2)]
        public static void Rest_main_db_happy_mutate() {
            ExecuteHttpFile("Rest/main_db/happy-mutate.http", "Rest/main_db/happy-mutate.result.http");
        }
    }
}
