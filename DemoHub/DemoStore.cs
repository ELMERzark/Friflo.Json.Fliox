﻿using Friflo.Json.Fliox;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host;

// ReSharper disable UnassignedReadonlyField
namespace Fliox.DemoHub
{
    /// <summary>
    /// The <see cref="DemoStore"/> offer two functionalities: <br/>
    /// 1. Defines a database <b>schema</b> by declaring its containers, commands and messages<br/>
    /// 2. Is a database <b>client</b> providing type-safe access to its containers, commands and messages <br/>
    /// <br/>
    /// <i>Info</i>: Use command <b>demo.FakeRecords</b> to create fake records in various containers. <br/>
    /// </summary>
    /// <remarks>
    /// <see cref="DemoStore"/> containers are fields or properties of type <see cref="EntitySet{TKey,T}"/>. <br/>
    /// Its commands are methods returning a <see cref="CommandTask{TResult}"/>. <br/>
    /// Its messages are methods returning a <see cref="MessageTask"/>. <br/>
    /// <br/>
    /// <see cref="DemoStore"/> instances can be used on client and server side. <br/>
    /// The <see cref="MessageHandler"/> demonstrates how to use a <see cref="DemoStore"/> instances as client to
    /// execute common database operations like: Upsert, Count and Query. <br/>
    /// </remarks>
    [OpenAPI(Version = "1.0.0",
        ContactName = "Ullrich Praetz", ContactUrl = "https://github.com/friflo/Friflo.Json.Fliox/issues",
        LicenseName = "MIT",            LicenseUrl = "https://spdx.org/licenses/MIT.html")]
    [OpenAPIServer(Description = "public DemoHub API", Url = "http://ec2-174-129-178-18.compute-1.amazonaws.com/fliox/rest/main_db")]
    [MessagePrefix("demo.")]
    public class DemoStore : FlioxClient {
        // --- containers
        public readonly EntitySet <long, Article>     articles;
        public readonly EntitySet <long, Customer>    customers;
        public readonly EntitySet <long, Employee>    employees;
        public readonly EntitySet <long, Order>       orders;
        public readonly EntitySet <long, Producer>    producers;
        
        // --- commands
        /// <summary> generate random entities (records) in the containers listed in the <see cref="DemoHub.Fake"/> param </summary>
        public CommandTask<Records>     FakeRecords (Fake param)    => SendCommand<Fake, Records>   ("demo.FakeRecords", param);

        /// <summary> count records added to containers within the last param seconds. default 60</summary>
        public CommandTask<Counts>      CountLatest (int? param)    => SendCommand<int?, Counts>    ("demo.CountLatest", param);
        
        /// <summary> return records added to containers within the last param seconds. default 60</summary>
        public CommandTask<Records>     LatestRecords(int? param)   => SendCommand<int?, Records>   ("demo.LatestRecords", param);

        /// <summary> simple command adding two numbers - no database access. </summary>
        public CommandTask<double>      Add  (Operands  param)      => SendCommand<Operands, double>("demo.Add", param);

        public DemoStore(FlioxHub hub) : base (hub) { }
    }
}
