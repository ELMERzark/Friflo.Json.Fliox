﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Utils;

namespace Friflo.Json.Fliox.Hub.Client
{
    /// <summary>
    /// Set of utility methods to guide a graceful shutdown by disposing all resources.
    /// The intended order for shutdown is:
    /// <list type="bullet">
    ///     <item><see cref="DisposeStore"/></item>
    ///     <item><see cref="DisposeDatabase"/></item>
    ///     <item><see cref="DisposeCaches"/></item>
    /// </list>  
    /// </summary>
    public static class DisposeUtils
    {
        public static async Task DisposeStore(FlioxClient store) {
            if (store == null)
                return;
            await store.CancelPendingSyncs().ConfigureAwait(false);
            store.Dispose();
        }
        
        public static void DisposeDatabase(FlioxHub hub) {
            if (hub == null)
                return;
            if (hub.EventDispatcher != null) {
                var eb = hub.EventDispatcher;
                hub.EventDispatcher = null;
                eb.Dispose();
            }
            hub.Dispose();
        }
        
        public static void DisposeCaches() {
            SharedEnv.Default.Pool.Dispose();
            SharedTypeStore.Dispose();
        }
    }
}