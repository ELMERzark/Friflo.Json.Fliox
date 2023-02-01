// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Pools;

// Note! - Must not have any dependency to System.Net or System.Net.Http (or other HTTP stuff)

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Remote
{
    internal readonly struct RemoteRequest
    {
        internal readonly   TaskCompletionSource<ProtocolResponse>  response;
        internal readonly   ReaderPool                              responseReaderPool;   
        
        internal RemoteRequest(SyncContext syncContext, CancellationTokenSource cancellationToken) {
            response            = new TaskCompletionSource<ProtocolResponse>();
            responseReaderPool  = syncContext.responseReaderPool; 
            
            syncContext.canceler = () => {
                cancellationToken.Cancel();
            };
        }
    }
    
    internal sealed class RemoteRequestMap {
        private  readonly  Dictionary<int, RemoteRequest> requestMap = new Dictionary<int, RemoteRequest>();
        
        internal void Add(int reqId, RemoteRequest request) {
            lock (requestMap) {
                requestMap.Add(reqId, request);    
            }
        }

        internal bool Remove (int reqId, out RemoteRequest request) {
            lock (requestMap) {
                return requestMap.Remove(reqId, out request);
            }
        }

        internal void CancelRequests() {
            Dictionary<int, RemoteRequest>.ValueCollection requests;
            lock (requestMap) {
                requests = requestMap.Values;
            }
            foreach (var request in requests) {
                request.response.SetCanceled();
            }
        }
    }
}