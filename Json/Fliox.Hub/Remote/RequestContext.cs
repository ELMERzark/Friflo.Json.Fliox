// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Event;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Utils;

// Note! - Must not have any dependency to System.Net or System.Net.Http (or other HTTP stuff)
namespace Friflo.Json.Fliox.Hub.Remote
{
    public interface IRequestHandler
    {
        string      Route           { get; }
        bool        IsMatch         (RequestContext context);
        Task        HandleRequest   (RequestContext context);
    }
    
    public sealed class RequestContext
    {
        // --- fields
        public    readonly  FlioxHub                    hub;
        public    readonly  string                      method;
        public    readonly  string                      route;
        public    readonly  string                      query;
        public    readonly  Stream                      body;
        public    readonly  IHttpHeaders                headers;
        public    readonly  IHttpCookies                cookies;
        private             Dictionary<string, string>  responseHeaders;
        internal            bool                        handled;
        // --- public properties
        public              string                      ResponseContentType { get; private set; }
        public              int                         StatusCode          { get; private set; }
        public              JsonValue                   Response            { get; private set; }
        public              int                         Offset              { get; private set; }
        public              Dictionary<string, string>  ResponseHeaders     => responseHeaders;
        public              bool                        Handled             => handled;
        public              ObjectPool<ObjectMapper>    ObjectMapper        => Pool.ObjectMapper;
        // --- internal properties
        internal            Pool                        Pool                => hub.sharedEnv.Pool;
        internal            SharedCache                 SharedCache         => hub.sharedEnv.sharedCache;

        public    override  string                      ToString() => $"{method} {route}{query}";

        public RequestContext (RemoteHostHub hostHub, string  method, string route, string query, Stream body, IHttpHeaders headers, IHttpCookies cookies) {
            this.hub        = hostHub.LocalHub;
            this.method     = method;
            this.route      = route;
            this.query      = query;
            this.body       = body;
            this.headers    = headers;
            this.cookies    = cookies;
        }
        
        public void Write (JsonValue value, int offset, string contentType, int statusCode) {
            ResponseContentType = contentType;
            StatusCode          = statusCode;
            Response            = value;
            Offset              = offset;
        }
        
        public void WriteString (string value, string contentType, int statusCode) {
            ResponseContentType = contentType;
            StatusCode          = statusCode;
            Response            = new JsonValue(value);
            Offset              = 0;
        }
        
        public void WriteError (string errorType, string message, int statusCode) {
            var error           = $"{errorType} > {message}";
            ResponseContentType = "text/plain";
            StatusCode          = statusCode;
            Response            = new JsonValue(error);
            Offset              = 0;
        }
        
        public void AddHeader(string key, string value) {
            if (responseHeaders == null) {
                responseHeaders = new Dictionary<string, string>();
            }
            responseHeaders.Add(key, value);
        }
        
        public void SetHeaders(Dictionary<string, string> headers) {
            responseHeaders = headers;
        }
        
        public static bool IsBasePath(string basePath, string route) {
            if (!route.StartsWith(basePath))
                return false;
            if (route.Length == basePath.Length)
                return true;
            return route[basePath.Length] == '/';
        }
        
        // method is added to preserve API internal for: SharedCache, Pool()
        public ExecuteContext CreateExecuteContext(IEventTarget eventTarget) {
            var sharedCache     = SharedCache;
            var localPool       = new Pool(hub.sharedEnv);
            return new ExecuteContext(localPool, eventTarget, sharedCache);
        }
    }
    
    public interface IHttpHeaders {
        string this[string key] { get; }
    }
    
    public interface IHttpCookies {
        string this[string key] { get; }
    }
}