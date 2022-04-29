// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using NUnit.Framework;
using static NUnit.Framework.Assert;

// ReSharper disable MethodHasAsyncOverload
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Remote
{
    public partial class TestRemote
    {
        [Test]
        public static void Static_index() {
            var request         = RestRequest("GET", "/");
            AssertRequest(request, 200, "text/html; charset=UTF-8");
            
            var requestIndex    = RestRequest("GET", "/index.html");
            AssertRequest(requestIndex, 200, "text/html; charset=UTF-8");
            
            var requestCached   = RestRequest("GET", "/index.html");
            AssertRequest(requestCached, 200, "text/html; charset=UTF-8");
            
            var root        = request.Response.AsString();
            var indexHtml   = requestIndex.Response.AsString();
            var cachedHtml  = requestIndex.Response.AsString();
            
            AreEqual(root, indexHtml);
            AreEqual(root, cachedHtml);
        }
        
        [Test, Order(1)]
        public static void Static_happy_read() {
            ExecuteRestFile("Static/happy.http", "Static/happy.result.http");
        }
        
        [Test, Order(1)]
        public static void Static_errors() {
            ExecuteRestFile("Static/errors.http", "Static/errors.result.http");
        }
        
        [Test]
        public static void Static_swagger() {
            var request = RestRequest("GET", "/swagger");
            AssertRequest(request, 200, "application/json");
        }
    }
}
