// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Friflo.Json.Fliox.Hub.DB.Cluster;
using Friflo.Json.Fliox.Hub.DB.Monitor;

namespace Friflo.Json.Fliox.Hub.Host.Auth
{
    public sealed class User {
        // --- public
        public   readonly   JsonKey                     userId;
        public   readonly   string                      token;
        public   readonly   Authorizer                  authorizer;

        public   override   string                      ToString() => userId.AsString();
        
        // --- internal
        internal readonly   ConcurrentDictionary<JsonKey, Empty>        clients;        // key: clientId
        internal readonly   ConcurrentDictionary<string, RequestCount>  requestCounts;  // key: database
        private             HashSet<string>                             groups;         // can be null
        
        public static readonly  JsonKey   AnonymousId = new JsonKey("anonymous");


        internal User (in JsonKey userId, string token, Authorizer authorizer) {
            clients         = new ConcurrentDictionary<JsonKey, Empty>(JsonKey.Equality);
            requestCounts   = new ConcurrentDictionary<string, RequestCount>();
            this.userId     = userId;
            this.token      = token;
            this.authorizer = authorizer;
        }
        
        public  IReadOnlyCollection<string> GetGroups() {
            if (groups != null)
                return groups;
            return Array.Empty<string>();
        }
        
        internal void SetGroups(IReadOnlyCollection<string> groups) {
            this.groups = groups?.ToHashSet();
        }
        
        public void SetUserOptions(UserOptions options) {
            groups = UpdateGroups(groups, options);
        }
        
        public static HashSet<string> UpdateGroups(ICollection<string> groups, UserOptions options) {
            var result = groups != null ? new HashSet<string>(groups) : new HashSet<string>();
            var addGroups = options.addGroups;
            if (addGroups != null) {
                result.UnionWith(addGroups);
            }
            var removeGroups = options.removeGroups;
            if (removeGroups != null) {
                foreach (var item in removeGroups) {
                    result.Remove(item);                    
                }
            }
            return result;
        }
    }
    
    internal struct Empty { }
}
