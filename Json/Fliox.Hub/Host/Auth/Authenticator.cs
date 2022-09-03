﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.DB.Cluster;
using Friflo.Json.Fliox.Hub.Host.Auth.Rights;
using Friflo.Json.Fliox.Hub.Protocol;
using static System.Diagnostics.DebuggerBrowsableState;

namespace Friflo.Json.Fliox.Hub.Host.Auth
{
    /// <summary>
    /// Performs authentication and authorization by checking <see cref="SyncRequest.userId"/> and <see cref="SyncRequest.token"/>
    /// in every <see cref="FlioxHub.ExecuteSync"/> call.
    /// </summary>
    /// <remarks>
    /// <see cref="Authenticator"/> is mutable. Its <see cref="users"/> and <see cref="registeredPredicates"/> are subject to change.
    /// </remarks>
    public abstract class Authenticator
    {
        protected readonly  Dictionary<string, AuthorizePredicate>  registeredPredicates;
        [DebuggerBrowsable(Never)]
        internal  readonly  ConcurrentDictionary<JsonKey, User>     users;  // todo make private
        // ReSharper disable once UnusedMember.Local - expose Dictionary as list in Debugger
        private             ICollection<User>                       Users => users.Values;
        internal  readonly  User                                    anonymousUser;
        
        public    override  string                                  ToString() => $"users: {users.Count}";

        public abstract Task    Authenticate    (SyncRequest syncRequest, SyncContext syncContext);
        
        protected Authenticator (TaskAuthorizer anonymousAuthorizer, HubPermission anonymousHubPermission) {
            registeredPredicates    = new Dictionary<string, AuthorizePredicate>();
            users                   = new ConcurrentDictionary <JsonKey, User>(JsonKey.Equality);
            anonymousUser           = new User(User.AnonymousId, null, anonymousAuthorizer, anonymousHubPermission); 
            users.TryAdd(User.AnonymousId, anonymousUser);
        }
        
        /// <summary>
        /// Validate <see cref="SyncContext.clientId"/> and returns <see cref="ClientIdValidation"/> result.
        /// </summary>
        public virtual ClientIdValidation ValidateClientId(ClientController clientController, SyncContext syncContext) {
            ref var clientId = ref syncContext.clientId; 
            if (clientId.IsNull()) {
                return ClientIdValidation.IsNull;
            }
            var user = syncContext.User;
            if (clientController.UseClientIdFor(user, clientId))
                return ClientIdValidation.Valid;
            return ClientIdValidation.Invalid;
        }
        
        public virtual bool EnsureValidClientId(ClientController clientController, SyncContext syncContext, out string error) {
            switch (syncContext.clientIdValidation) {
                case ClientIdValidation.Valid:
                    error = null;
                    return true;
                case ClientIdValidation.IsNull:
                    error           = null;
                    var user        = syncContext.User;
                    var clientId    = clientController.NewClientIdFor(user);
                    syncContext.SetClientId(clientId);
                    return true;
                case ClientIdValidation.Invalid:
                    error = "invalid clientId";
                    return false;
            }
            throw new InvalidOperationException ("unexpected clientIdValidation state");
        }
        
        public virtual Task SetUserOptions (User user, UserParam param) {
            user.SetUserOptions(param);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Register a predicate function by the given <paramref name="name"/> which enables custom authorization via code,
        /// which cannot be expressed by one of the provided <see cref="TaskRight"/> implementations.
        /// If called its parameters are intended to filter the aspired condition and return true if task execution is granted.
        /// To reject task execution it returns false.
        /// </summary>
        public void RegisterPredicate(string name, AuthPredicate predicate) {
            var authorizer = new AuthorizePredicate (name, predicate);
            registeredPredicates.Add(name, authorizer);
        }
        
        /// <summary>
        /// Register a predicate function which enables custom authorization via code, which cannot be expressed by one of the
        /// provided <see cref="TaskRight"/> implementations.
        /// The <paramref name="predicate"/> is registered by its delegate name.
        /// If called its parameters are intended to filter the aspired condition and return true if task execution is granted.
        /// To reject task execution it returns false.
        /// </summary>
        public void RegisterPredicate(AuthPredicate predicate) {
            var name = predicate.Method.Name;
            var authorizer = new AuthorizePredicate (name, predicate);
            registeredPredicates.Add(name, authorizer);
        }
        
        internal void ClearUserStats() {
            foreach (var pair in users) {
                pair.Value.requestCounts.Clear();
            }
        }
    }
    
    /// <summary>
    /// Represent the result of client id validation returned by <see cref="Authenticator.ValidateClientId"/>  
    /// </summary>
    public enum ClientIdValidation {
        IsNull,
        Invalid,
        Valid
    }
}