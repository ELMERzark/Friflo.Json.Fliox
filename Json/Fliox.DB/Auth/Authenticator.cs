﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.Auth.Rights;
using Friflo.Json.Fliox.DB.Host;
using Friflo.Json.Fliox.DB.Protocol;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.DB.Auth
{
    /// <summary>
    /// Performs authentication and authorization by checking <see cref="SyncRequest.userId"/> and <see cref="SyncRequest.token"/>
    /// in every <see cref="EntityDatabase.ExecuteSync"/> call.
    /// </summary>
    public abstract class Authenticator
    {
        protected readonly  Dictionary<string, AuthorizePredicate>  registeredPredicates;
        protected readonly  JsonKey                                 anonymous = new JsonKey("anonymous");  
        internal  readonly  ConcurrentDictionary<JsonKey, AuthUser> authUsers;
            
        public abstract Task    Authenticate    (SyncRequest syncRequest, MessageContext messageContext);
        
        protected Authenticator () {
            registeredPredicates    = new Dictionary<string, AuthorizePredicate>();
            authUsers               = new ConcurrentDictionary <JsonKey, AuthUser>(JsonKey.Equality);
        }
        
        /// <summary>
        /// Validate <see cref="MessageContext.clientId"/>. Return true if it was valid or null.
        /// </summary>
        public virtual ClientIdValidation ValidateClientId(ClientController clientController, MessageContext messageContext) {
            if (messageContext.clientId.IsNull()) {
                return ClientIdValidation.IsNull;
            }
            var userId = messageContext.userId.IsNull() ? anonymous : messageContext.userId;  
            clientController.AddClientIdFor(userId, messageContext.clientId);
            if (!authUsers.TryGetValue(userId, out var credentials)) {
                throw new InvalidOperationException("Expect user already added");
            }
            credentials.clients.Add(messageContext.clientId);
            return ClientIdValidation.Valid;
        }
        
        public virtual bool EnsureValidClientId(ClientController clientController, MessageContext messageContext, out string error) {
            error = null;
            switch (messageContext.clientIdValidation) {
                case ClientIdValidation.Valid:
                    return true;
                case ClientIdValidation.IsNull:
                    if (messageContext.userId.IsNull()) {
                        messageContext.clientId = clientController.NewClientIdFor(anonymous);
                    } else { 
                        messageContext.clientId = clientController.NewClientIdFor(messageContext.userId);
                    }
                    messageContext.clientIdValidation = ClientIdValidation.Valid;
                    return true;
            }
            throw new InvalidOperationException ("unexpected clientIdValidation state");
        }

        /// <summary>
        /// Register a predicate function by the given <see cref="name"/> which enables custom authorization via code,
        /// which cannot be expressed by one of the provided <see cref="Right"/> implementations.
        /// If called its parameters are intended to filter the aspired condition and return true if task execution is granted.
        /// To reject task execution it returns false.
        /// </summary>
        public void RegisterPredicate(string name, AuthPredicate predicate) {
            var authorizer = new AuthorizePredicate (name, predicate);
            registeredPredicates.Add(name, authorizer);
        }
        
        /// <summary>
        /// Register a predicate function which enables custom authorization via code, which cannot be expressed by one of the
        /// provided <see cref="Right"/> implementations.
        /// The <see cref="predicate"/> is registered by its delegate name.
        /// If called its parameters are intended to filter the aspired condition and return true if task execution is granted.
        /// To reject task execution it returns false.
        /// </summary>
        public void RegisterPredicate(AuthPredicate predicate) {
            var name = predicate.Method.Name;
            var authorizer = new AuthorizePredicate (name, predicate);
            registeredPredicates.Add(name, authorizer);
        }
    }
    
    public enum ClientIdValidation {
        IsNull,
        Invalid,
        Valid
    }
    
    public sealed class AuthenticateNone : Authenticator
    {
        private readonly Authorizer unknown;

        public AuthenticateNone(Authorizer unknown) {
            this.unknown = unknown ?? throw new NullReferenceException(nameof(unknown));
        }
        
        public override Task Authenticate(SyncRequest syncRequest, MessageContext messageContext) {
            var userId = messageContext.userId.IsNull() ? anonymous : messageContext.userId;
            if (!authUsers.TryGetValue(userId, out var authUser)) {
                authUser = new AuthUser(null, unknown);
                authUsers.TryAdd(userId, authUser);
            }
            messageContext.authState.SetFailed(authUser, "not authenticated", unknown);
            return Task.CompletedTask;
        }
    }
}