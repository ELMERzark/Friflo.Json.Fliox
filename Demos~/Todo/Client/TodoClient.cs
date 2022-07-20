﻿using System;
using System.ComponentModel.DataAnnotations;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host;

// ReSharper disable All
namespace Todo
{
    /// <summary>
    /// The <see cref="TodoClient"/> offer two functionalities: <br/>
    /// 1. Defines a database <b>schema</b> by declaring its containers, commands and messages<br/>
    /// 2. Is a database <b>client</b> providing type-safe access to its containers, commands and messages <br/>
    /// </summary>
    public class TodoClient : FlioxClient
    {
        // --- containers
        public  readonly    EntitySet <long, Job>   jobs;

        public TodoClient(FlioxHub hub, string dbName = null) : base (hub, dbName) { }
    }
    
    // ---------------------------------- entity models ----------------------------------
    public class Job
    {
        [Key]       public  long        id { get; set; }
        ///<summary> short job title / name </summary>
        [Required]  public  string      title;
                    public  bool?       completed;
                    public  DateTime?   created;
                    public  string      description;
    }
}
