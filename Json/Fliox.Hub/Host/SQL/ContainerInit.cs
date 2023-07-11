// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Protocol.Models;

namespace Friflo.Json.Fliox.Hub.Host.SQL
{
    public interface ISQLDatabase
    {
    }
    
    public interface ISQLTable
    {
        Task<TaskExecuteError>  InitTable           (ISyncConnection connection);
        Task<TaskExecuteError>  AddVirtualColumns   (ISyncConnection connection);
    }
}