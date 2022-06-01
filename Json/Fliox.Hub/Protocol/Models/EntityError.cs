﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Req = Friflo.Json.Fliox.RequiredFieldAttribute;
using Ignore = Friflo.Json.Fliox.IgnoreFieldAttribute;
using Serialize = Friflo.Json.Fliox.SerializeFieldAttribute;

namespace Friflo.Json.Fliox.Hub.Protocol.Models
{
    /// <summary>
    /// Used by <see cref="SyncResponse"/> to return errors when mutating an entity by: create, upsert, patch and delete
    /// </summary>
    /// <remarks> 
    /// An <see cref="EntityError"/> needs to be set only, if the access to <see cref="EntityValue"/>'s
    /// returned by a previous call to <see cref="EntityContainer.ReadEntitiesSet"/> or
    /// <see cref="EntityContainer.QueryEntities"/> fails.
    /// This implies that the previous read or query call was successful.
    /// </remarks> 
    public sealed class EntityError
    {
        /// <summary>entity id</summary>
        [Req]       public  JsonKey             id;
        /// <summary>error type when accessing an entity in a database</summary>
        [Req]       public  EntityErrorType     type;
        /// <summary>error details when accessing an entity</summary>
        [Serialize] public  string              message;
            
        [Ignore]    public  string              container;
        /// <summary>Is != <see cref="TaskErrorResultType.None"/> if the error is caused indirectly by a <see cref="SyncRequestTask"/> error.</summary>
        [Ignore]    public  TaskErrorResultType taskErrorType;
        /// <summary>Show the stacktrace if <see cref="taskErrorType"/> == <see cref="TaskErrorResultType.UnhandledException"/>
        /// and the accessed <see cref="EntityContainer"/> implementation expose this data.</summary>
        [Ignore]    public  string              stacktrace;

        public override     string              ToString() => AsText(true);

        public EntityError() { } // required for TypeMapper

        public EntityError(EntityErrorType type, string container, in JsonKey id, string message) {
            this.type       = type;
            this.container  = container;
            this.id         = id;
            this.message    = message;
        }
        
        public string AsText(bool showStack) {
            var sb = new StringBuilder();
            AppendAsText("", sb, showStack);
            return sb.ToString();
        }

        public void AppendAsText(string prefix, StringBuilder sb, bool showStack) {
            sb.Append(prefix);
            sb.Append(type);
            sb.Append(": ");
            sb.Append(container);
            sb.Append(" [");
            id.AppendTo(sb);
            sb.Append("], ");
            if (taskErrorType != TaskErrorResultType.None) {
                sb.Append(taskErrorType);
                sb.Append(" - ");
            }
            sb.Append(message);
            if (showStack && stacktrace != null) {
                sb.Append('\n');
                sb.Append(stacktrace);
            }
        }
    }

    public sealed class EntityException : Exception
    {
        public EntityException(EntityError error) : base(error.AsText(false)) { }
    }

    /// <summary>
    /// Error type when accessing an entity from a database container  
    /// </summary>
    public enum EntityErrorType
    {
        Undefined,   // Prevent implicit initialization of underlying value 0 to a valid value (ParseError)
        /// <summary>Invalid JSON when reading an entity from database<br/>
        /// can happen with key-value databases - e.g. file-system - as their values are not restricted to JSON</summary>
        ParseError,
        /// <summary>Reading an entity from database failed<br/>
        /// e.g. a corrupt file when using the file-system as database</summary>
        ReadError,
        /// <summary>Writing an entity to database failed<br/>
        /// e.g. the file is already in use by another process when using the file-system as database</summary>
        WriteError,
        /// <summary>Deleting an entity in database failed<br/>
        /// e.g. the file is already in use by another process when using the file-system as database</summary>
        DeleteError,
        /// <summary>Patching an entity failed</summary>
        PatchError
    }
}
