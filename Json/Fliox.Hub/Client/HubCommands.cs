// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Fliox.Hub.DB.Cluster;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.Hub.Client
{
    /// <summary>
    /// Used to group commands by a single class. <br/>
    /// The intention is to use a sub class of <see cref="HubCommands"/> as a field in a class extending <see cref="FlioxClient"/>.
    /// <br/> 
    /// This establish differentiation between <see cref="FlioxClient"/> own methods and commands added
    /// to a <see cref="FlioxClient"/> sub class.
    /// <code>
    ///     public class MyStore : FlioxClient
    ///     {
    ///         public MyCommands test;
    ///    
    ///         public MyStore(FlioxHub hub) : base(hub) {
    ///             test = new MyCommands(this);
    ///         }
    ///     }
    ///
    ///     public class MyCommands : HubCommands
    ///     {
    ///         public MyCommands(FlioxClient client) : base(client) { }
    ///    
    ///         public CommandTask &lt;string&gt; Cmd (string param) => SendCommand &lt;string, string&gt;("test.Cmd", param);
    ///     } 
    /// </code>
    /// </summary>
    public class HubCommands
    {
        private readonly    FlioxClient client;
        
        protected HubCommands (FlioxClient client) {
            this.client = client;
        }
        
        protected CommandTask<TResult> SendCommand<TParam, TResult>(string name, TParam param) {
            return client.SendCommand<TParam, TResult>(name, param);
        }
    }
    
    // ---------------------------------- standard database & host commands ----------------------------------
    
    /// <summary>
    /// Contains commands addressed to the database. Its commands are prefixed with
    /// <b>db.*</b>
    /// </summary>
    public class DatabaseCommands : HubCommands
    {
        protected internal DatabaseCommands(FlioxClient client) : base(client) { }
        
        // Declared only to generate command in Schema 
        internal CommandTask<JsonValue>     Echo(JsonValue _) => throw new InvalidOperationException("unexpected call of DbEcho command");

        // --- commands
        public CommandTask<TParam>          Echo<TParam> (TParam param) => SendCommand<TParam,TParam>  (Std.Echo, param);
        public CommandTask<DbContainers>    Containers()=>  SendCommand<JsonValue, DbContainers>(Std.Containers,new JsonValue());
        public CommandTask<DbCommands>      Commands()  =>  SendCommand<JsonValue, DbCommands>  (Std.Commands,  new JsonValue());
        public CommandTask<DbSchema>        Schema()    =>  SendCommand<JsonValue, DbSchema>    (Std.Schema,    new JsonValue());
        public CommandTask<DbStats>         Stats()     =>  SendCommand<JsonValue, DbStats>     (Std.Stats,     new JsonValue());
    }
    
    /// <summary>
    /// Contains commands addressed to the host. Its commands are prefixed with
    /// <b>host.*</b>
    /// </summary>
    public class HostCommands : HubCommands
    {
        protected internal HostCommands(FlioxClient client) : base(client) { }
        
        // --- commands
        public CommandTask<HostDetails>     Details()   =>  SendCommand<JsonValue, HostDetails> (Std.HostDetails,  new JsonValue());
        public CommandTask<HostCluster>     Cluster()   =>  SendCommand<JsonValue, HostCluster> (Std.HostCluster,  new JsonValue());
    }
    
    /// Should not be public 
    internal static class Std  {
        // --- db.*
        public const string Echo        = "db.Echo";
        public const string Containers  = "db.Containers";
        public const string Commands    = "db.Commands";
        public const string Schema      = "db.Schema";
        public const string Stats       = "db.Stats";

        // --- host.*
        public const string HostDetails = "host.Details";
        public const string HostCluster = "host.Cluster";
    }
}