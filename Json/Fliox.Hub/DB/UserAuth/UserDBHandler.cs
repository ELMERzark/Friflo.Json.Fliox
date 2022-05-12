// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Linq;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.Hub.DB.UserAuth
{
    public sealed class UserDBHandler : TaskHandler
    {
        public UserDBHandler() {
            AddCommandHandlerAsync<Credentials, AuthResult>              (nameof(AuthenticateUser), AuthenticateUser);
            AddCommandHandlerAsync<JsonValue, ValidateUserDbResult> (nameof(ValidateUserDb),    ValidateUserDb);
        }
        
        private async Task<AuthResult> AuthenticateUser (Param<Credentials> param, MessageContext command) {
            using(var pooled = command.Pool.Type(() => new UserStore(command.Hub)).Get()) {
                var store           = pooled.instance;
                store.UserId        = UserStore.Server;
                if (!param.GetValidate(out var authenticate, out var error)) {
                    command.Error(error);
                    return null;
                }
                var userId          = authenticate.userId;
                var readCredentials = store.credentials.Read();
                var findCred        = readCredentials.Find(userId);
                
                await store.TrySyncTasks().ConfigureAwait(false);
                
                if (!readCredentials.Success) {
                    command.Error(readCredentials.Error.Message);
                    return null;  
                }

                UserCredential  cred    = findCred.Result;
                bool            isValid = cred != null && cred.token == authenticate.token;
                return new AuthResult { isValid = isValid };
            }
        }
        
        private async Task<ValidateUserDbResult> ValidateUserDb (Param<JsonValue> param, MessageContext command) {
            var authenticator   = (UserAuthenticator)command.Hub.Authenticator;
            var databases       = command.Hub.GetDatabases().Keys.ToHashSet();
            var errors          = await authenticator.ValidateUserDb(databases);
            
            return new ValidateUserDbResult { errors = errors.ToArray() };
        }
    }
}