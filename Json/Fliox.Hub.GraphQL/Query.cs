// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Schema.Definition;
using Friflo.Json.Fliox.Schema.GraphQL;
using Friflo.Json.Fliox.Transform.Project;

namespace Friflo.Json.Fliox.Hub.GraphQL
{
    internal readonly struct Query
    {
        internal  readonly  string          name;
        internal  readonly  QueryType       type;
        internal  readonly  string          container;
        internal  readonly  SyncRequestTask task;
        internal  readonly  SelectionNode   selection;

        public    override  string          ToString() => $"{type}: {name}";

        internal Query(string name, QueryType type, string container, SyncRequestTask task, in SelectionNode selection) {
            this.name       = name;
            this.type       = type;
            this.container  = container;
            this.task       = task;
            this.selection  = selection;
        }
    }
    
    internal readonly struct QueryResolver
    {
        internal  readonly  string          name;
        internal  readonly  QueryType       queryType;
        internal  readonly  SelectionType   type;
        
        /// <summary> only: <see cref="QueryType.Query"/> and <see cref="QueryType.ReadById"/> </summary>
        internal  readonly  string      container;
        /// <summary> only: <see cref="QueryType.Message"/> and <see cref="QueryType.Command"/> </summary>
        internal  readonly  bool        hasParam;
        /// <summary> only: <see cref="QueryType.Message"/> and <see cref="QueryType.Command"/> </summary>
        internal  readonly  bool        paramRequired;  // message / command only

        public    override  string      ToString() => $"{queryType}: {name}";

        internal QueryResolver(string name, QueryType queryType, string container, FieldDef param, TypeDef type) {
            this.name       = container != null ? Gql.MethodName(name, container) : name;
            this.queryType  = queryType;
            this.container  = container;
            hasParam        = param != null;
            paramRequired   = param != null && param.required;
            var typeName    = type?.nameUtf8 ?? default;
            this.type       = new SelectionType(typeName);
        }
    }
    
    internal enum QueryType {
        Query,
        Count,
        ReadById,
        Create,
        Upsert,
        Delete,
        Command,
        Message
    }
}
