﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using Friflo.Json.Fliox.Hub.Host.Auth;
using Friflo.Json.Fliox.Hub.Host.Event;
using Friflo.Json.Fliox.Hub.Host.Utils;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Utils;

namespace Friflo.Json.Fliox.Hub.Host
{
    // ------------------------------------ SyncContext ------------------------------------
    /// <summary>
    /// One <see cref="SyncContext"/> is created per <see cref="FlioxHub.ExecuteSync"/> call to enable
    /// multi threaded / concurrent handling of a <see cref="SyncRequest"/>.
    /// </summary>
    /// <remarks>
    /// Note: In case of adding transaction support for <see cref="SyncRequest"/>'s in future transaction data / state
    /// need to be handled by this class.
    /// </remarks>
    public sealed class SyncContext
    {
        // --- public
        public              FlioxHub                    Hub             => hub;
        public              JsonKey                     clientId;
        public              ClientIdValidation          clientIdValidation;
        public              User                        User            => authState.user;
        public              bool                        Authenticated   => authState.authenticated;
        public              string                      DatabaseName    { get; internal set; }              // not null
        public              EntityDatabase              Database        => hub.GetDatabase(DatabaseName);   // not null
        public              ObjectPool<ObjectMapper>    ObjectMapper    => pool.ObjectMapper;
        public              ObjectPool<EntityProcessor> EntityProcessor => pool.EntityProcessor;

        // --- internal / private by intention
        /// <summary>
        /// Note!  Keep <see cref="pool"/> internal
        /// Its <see cref="ObjectPool{T}"/> instances can be made public as properties if required
        /// </summary>
        internal  readonly  Pool                pool;
        /// <summary>Is set for clients requests only. In other words - from the initiator of a <see cref="ProtocolRequest"/></summary>
        internal  readonly  IEventReceiver      eventReceiver;
        internal            AuthState           authState;
        internal            Action              canceler = () => {};
        internal            FlioxHub            hub;
        internal  readonly  SharedCache         sharedCache;
        
        public override     string              ToString() => $"userId: {authState.user}, auth: {authState}";

        internal SyncContext (Pool pool, IEventReceiver eventReceiver, SharedCache sharedCache) {
            this.pool           = pool;
            this.eventReceiver    = eventReceiver;
            this.sharedCache    = sharedCache;
        }
        
        internal SyncContext (Pool pool, IEventReceiver eventReceiver, SharedCache sharedCache, in JsonKey clientId) {
            this.pool           = pool;
            this.eventReceiver  = eventReceiver;
            this.clientId       = clientId;
            this.sharedCache    = sharedCache;
        }
        
        public void AuthenticationFailed(User user, string error, Authorizer authorizer) {
            AssertAuthenticationParams(user, authorizer);
            authState.user            = user;
            authState.authExecuted    = true;
            authState.authenticated   = false;
            authState.authorizer      = authorizer;
            authState.error           = error;
        }
        
        public void AuthenticationSucceed (User user, Authorizer authorizer) {
            AssertAuthenticationParams(user, authorizer);
            authState.user            = user;
            authState.authExecuted    = true;
            authState.authenticated   = true;
            authState.authorizer      = authorizer;
        }
        
        [Conditional("DEBUG")]
        private void AssertAuthenticationParams(User user, Authorizer authorizer) {
            if (authState.authExecuted) throw new InvalidOperationException("Expect AuthExecuted == false");
            if (user == null)           throw new ArgumentNullException(nameof(user));
            if (authorizer == null)     throw new ArgumentNullException(nameof(authorizer));
        }
        
        internal void Cancel() {
            canceler(); // canceler.Invoke();
        }
        
        // todo remove
        public void Release() { }
    }
    
    /// <summary>
    /// Contains the result of a <see cref="FlioxHub.ExecuteSync"/> call. <br/>
    /// After execution either <see cref="success"/> or <see cref="error"/> is set. Never both.
    /// </summary>
    public readonly struct ExecuteSyncResult {
        public   readonly   SyncResponse    success;
        public   readonly   ErrorResponse   error;

        public ExecuteSyncResult (SyncResponse successResponse) {
            success = successResponse ?? throw new ArgumentNullException(nameof(successResponse));
            error   = null;
        }
        
        public ExecuteSyncResult (string errorMessage, ErrorResponseType errorType) {
            success = null;
            error   = new ErrorResponse { message = errorMessage, type = errorType };
        }
        
        public  ProtocolResponse Result { get {
            if (success != null)
                return success;
            return error;
        } }

        public override string ToString() => success != null ? success.ToString() : error.ToString();
    }
}
