#if !UNITY_2020_1_OR_NEWER

using System.Net.WebSockets;
using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.Remote;
using Friflo.Json.Fliox.Mapper;
using Microsoft.AspNetCore.Http;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.DB.Utils
{
    public static class AspNetCoreUtils
    {
        public static async Task HandleFlioxHostRequest(this HttpContext context, HttpHostDatabase hostDatabase) {
            if (context.WebSockets.IsWebSocketRequest) {
                WebSocket ws = await context.WebSockets.AcceptWebSocketAsync();
                await WebSocketHost.SendReceiveMessages(ws, hostDatabase);
                return;
            }
            var httpRequest = context.Request;
            var reqCtx = new RequestContext(httpRequest.Method, httpRequest.Path.Value, httpRequest.Body);
            await hostDatabase.ExecuteHttpRequest(reqCtx).ConfigureAwait(false);
                    
            var httpResponse            = context.Response;
            JsonUtf8 response           = reqCtx.Response;
            httpResponse.StatusCode     = reqCtx.StatusCode;
            httpResponse.ContentType    = reqCtx.ResponseContentType;
            httpResponse.ContentLength  = response.Length;
            await httpResponse.Body.WriteAsync(response, 0, response.Length).ConfigureAwait(false);
        }
    }
}

#endif