﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;

namespace Friflo.Json.Fliox.Hub.Host.Auth
{
    /// <summary>
    /// Authorize a given task.
    /// <br></br>
    /// This <see cref="IAuthorizer"/> it stored at <see cref="AuthState.authorizer"/>.
    /// The <see cref="AuthState.authorizer"/> is set via <see cref="Authenticator.Authenticate"/> for
    /// <see cref="AuthState.authenticated"/> and for not <see cref="AuthState.authenticated"/> users.  
    /// </summary>
    public interface IAuthorizer
    {
        /// <summary>
        /// Create a set of <paramref name="databases"/> used to filter
        /// <see cref="DB.Cluster.ClusterStore"/> read and query results available to a user.
        /// </summary>
        void    AddAuthorizedDatabases  (HashSet<AuthorizeDatabase> databases);
        bool    Authorize               (SyncRequestTask task, ExecuteContext executeContext);
    }
}