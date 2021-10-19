﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.Host;
using Friflo.Json.Fliox.Utils;

namespace Friflo.Json.Fliox.DB.UserAuth
{
    /// <summary>
    /// Used to authenticate users stored in the given user <see cref="EntityDatabase"/>.
    /// If user authentication succeed it returns also the roles attached to a user. 
    /// The schema of the user database is defined in <see cref="UserStore"/>.
    /// </summary>
    public class UserDatabaseHandler : IDisposable
    {
        private readonly SharedPool<UserStore>   storePool;
        
        public UserDatabaseHandler(EntityDatabase authDatabase) {
            storePool = new SharedPool<UserStore> (() => new UserStore(authDatabase, UserStore.Server, null));
            authDatabase.Authenticator = new UserDatabaseAuthenticator();
            authDatabase.taskHandler.AddCommandHandlerAsync<AuthenticateUser, AuthenticateUserResult>(AuthenticateUser); 
        }
        
        public void Dispose() {
            storePool.Dispose();
        }
        
        private async Task<AuthenticateUserResult> AuthenticateUser (Command<AuthenticateUser> command) {
            using (var pooledStore = storePool.Get()) {
                var store           = pooledStore.instance;
                var validateToken   = command.Value;
                var userId          = validateToken.userId;
                var readCredentials = store.credentials.Read();
                var findCred        = readCredentials.Find(userId);
                
                await store.ExecuteTasksAsync().ConfigureAwait(false);

                UserCredential  cred    = findCred.Result;
                bool            isValid = cred != null && cred.token == validateToken.token;
                return new AuthenticateUserResult { isValid = isValid };
            }
        }
    }
}