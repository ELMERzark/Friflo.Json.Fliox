﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper;

// ReSharper disable CollectionNeverUpdated.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnassignedField.Global
namespace Friflo.Json.Fliox.Hub.Host.Auth.Rights
{
    /// <summary>
    /// <see cref="TaskRight"/> grant <see cref="database"/> access by a set of task <see cref="types"/>. <br/> 
    /// </summary>
    public sealed class TaskRight : Right
    {
        /// <summary>a specific database: 'test_db', multiple databases by prefix: 'test_*', all databases: '*'</summary>
                        public  string          database;
                        /// <summary>set fo task types like: create, read, upsert, delete, query, ...</summary>
        [Fri.Required]  public  List<TaskType>  types;
        
        public  override        RightType       RightType => RightType.task;
        
        public override IAuthorizer ToAuthorizer() {
            if (types.Count == 1) {
                return GetAuthorizer(types[0]);
            }
            var list = new List<IAuthorizer>(types.Count);
            foreach (var task in types) {
                list.Add(GetAuthorizer(task));
            }
            return new AuthorizeAny(list);
        }
        
        private IAuthorizer GetAuthorizer(TaskType taskType) {
            switch (taskType) {
                case TaskType.read:                return new AuthorizeTaskType(TaskType.read,              database);
                case TaskType.query:               return new AuthorizeTaskType(TaskType.query,             database);
                case TaskType.aggregate:           return new AuthorizeTaskType(TaskType.aggregate,         database);
                case TaskType.create:              return new AuthorizeTaskType(TaskType.create,            database);
                case TaskType.upsert:              return new AuthorizeTaskType(TaskType.upsert,            database);
                case TaskType.patch:               return new AuthorizeTaskType(TaskType.patch,             database);
                case TaskType.delete:              return new AuthorizeTaskType(TaskType.delete,            database);
                case TaskType.closeCursors:        return new AuthorizeTaskType(TaskType.closeCursors,      database);
                //
                case TaskType.message:             return new AuthorizeTaskType(TaskType.message,           database);
                case TaskType.command:             return new AuthorizeTaskType(TaskType.message,           database);
                case TaskType.subscribeChanges:    return new AuthorizeTaskType(TaskType.subscribeChanges,  database);
                case TaskType.subscribeMessage:    return new AuthorizeTaskType(TaskType.subscribeMessage,  database);
            }
            throw new InvalidOperationException($"unknown authorization taskType: {taskType}");
        }

    }
}