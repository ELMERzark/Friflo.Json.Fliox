// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Host.Auth
{
    public sealed class AuthorizeAll : Authorizer {
        private readonly    Authorizer[]     list;
        
        public AuthorizeAll(ICollection<Authorizer> list) {
            this.list = list.ToArray();    
        }
        
        public override void AddAuthorizedDatabases(HashSet<DatabaseFilter> databaseFilters) {
            foreach (var item in list) {
                item.AddAuthorizedDatabases(databaseFilters);
            }
        }
        
        public override bool AuthorizeTask(SyncRequestTask task, SyncContext syncContext) {
            foreach (var item in list) {
                if (!item.AuthorizeTask(task, syncContext))
                    return false;
            }
            return true;
        }
    }
}