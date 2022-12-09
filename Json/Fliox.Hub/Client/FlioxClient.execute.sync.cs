﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using Friflo.Json.Fliox.Hub.Client.Internal;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol;

// Note!  Keep file in sync with:  FlioxClient.execute.async.cs

namespace Friflo.Json.Fliox.Hub.Client
{
    public partial class FlioxClient
    {
        /// <summary> Execute all tasks created by methods of <see cref="EntitySet{TKey,T}"/> and <see cref="FlioxClient"/> </summary>
        /// <remarks>
        /// In case any task failed a <see cref="SyncTasksException"/> is thrown. <br/>
        /// As an alternative use <see cref="TrySyncTasks"/> to execute tasks which does not throw an exception. <br/>
        /// The method can be called without awaiting the result of a previous call. </remarks>
        public SyncResult SyncTasksSynchronous() {
            var syncRequest = CreateSyncRequest(out SyncStore syncStore);
            var buffer      = CreateMemoryBuffer();
            var syncContext = CreateSyncContext(buffer);
            var response    = ExecuteRequest(syncRequest, syncContext);
            
            ReuseSyncContext(syncContext);
            var result      = HandleSyncResponse(syncRequest, response, syncStore, buffer);
            if (!result.Success) {
                throw new SyncTasksException(response.error, result.failed);
            }
            return result;
        }
        
        /// <summary> Execute all tasks created by methods of <see cref="EntitySet{TKey,T}"/> and <see cref="FlioxClient"/> </summary>
        /// <remarks>
        /// Failed tasks are available via the returned <see cref="SyncResult"/> in the field <see cref="SyncResult.failed"/> <br/>
        /// In performance critical application this method should be used instead of <see cref="SyncTasks"/> as throwing exceptions is expensive. <br/> 
        /// The method can be called without awaiting the result of a previous call. </remarks>
        public SyncResult TrySyncTasksSynchronous() {
            var syncRequest = CreateSyncRequest(out SyncStore syncStore);
            var buffer      = CreateMemoryBuffer();
            var syncContext = CreateSyncContext(buffer);
            var response    = ExecuteRequest(syncRequest, syncContext);

            ReuseSyncContext(syncContext);
            return HandleSyncResponse(syncRequest, response, syncStore, buffer);
        }
        
        private ExecuteSyncResult ExecuteRequest(SyncRequest syncRequest, SyncContext syncContext) {
            _intern.syncCount++;
            if (_intern.ackTimerPending) {
                _intern.ackTimer?.Change(Timeout.Infinite, Timeout.Infinite);
                _intern.ackTimerPending = false;
            }

            try {
                _intern.hub.InitSyncRequest(syncRequest);
                var response = _intern.hub.ExecuteRequest(syncRequest, syncContext);
                
                // The Hub returns a client id if the client didn't provide one and one of its task require one. 
                var success = response.success;
                if (_intern.clientId.IsNull() && success != null && !success.clientId.IsNull()) {
                    SetClientId(success.clientId);
                }
                return response;
            }
            catch (Exception e) {
                var errorMsg = ErrorResponse.ErrorFromException(e).ToString();
                return new ExecuteSyncResult(errorMsg, ErrorResponseType.Exception);
            }
        }
    }
}