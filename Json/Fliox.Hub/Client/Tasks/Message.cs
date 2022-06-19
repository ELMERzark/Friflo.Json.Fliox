﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Diagnostics;
using Friflo.Json.Fliox.Hub.Client.Internal;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Mapper;
using static System.Diagnostics.DebuggerBrowsableState;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Client
{
    /// <summary>
    /// A <see cref="MessageTask"/> contains the message (or command) <see cref="name"/> and <see cref="param"/> sent to
    /// an <see cref="EntityDatabase"/> by <see cref="FlioxClient.SendMessage"/>.<br/>
    /// It is used to send to the message (or command) as en event to all clients which successful subscribed the
    /// message by its <see cref="name"/>.
    /// If the message was sent successful <see cref="SyncFunction.Success"/> is true.
    /// <br/>
    /// <b>Note</b>: A message returns no result. To get a result send a command by <see cref="FlioxClient.SendCommand{TParam,TResult}"/> 
    /// </summary>
    public class MessageTask : SyncTask
    {
        internal readonly   string          name;
        internal readonly   JsonValue       param;
        
        [DebuggerBrowsable(Never)]
        internal            TaskState       state;
        internal override   TaskState       State       => state;
        
        public   override   string          Details     => $"MessageTask (name: {name})";

        
        internal MessageTask(string name, JsonValue param) {
            this.name   = name;
            this.param  = param;
        }
    }

    /// <summary>
    /// A <see cref="CommandTask"/> is created when a command is send to an <see cref="EntityDatabase"/> by
    /// <see cref="FlioxClient.SendCommand{TResult}"/>.<br/>
    /// Additional to a <see cref="MessageTask"/> a <see cref="CommandTask"/> also provide a command <see cref="RawResult"/>
    /// after the task is synced successful.
    /// <br/>
    /// <b>Note</b>: For type safe access to the result use <see cref="CommandTask{TResult}"/> returned by
    /// <see cref="FlioxClient.SendCommand{TParam,TResult}"/>
    /// </summary>
    public class CommandTask : MessageTask
    {
        private  readonly   Pool            pool;
        internal            JsonValue       result;

        public   override   string          Details     => $"CommandTask (name: {name})";

        /// <summary>Return the result of a command used as a command as JSON.
        /// JSON is "null" if the command doesnt return a result.
        /// For type safe access of the result use <see cref="ReadResult{T}"/></summary>
        public              JsonValue       RawResult  => IsOk("CommandTask.RawResult", out Exception e) ? result : throw e;
        
        internal CommandTask(string name, JsonValue param, Pool pool) : base (name, param) {
            this.pool = pool;
        }

        /// <summary>
        /// Return a type safe result of a command.
        /// The result is null if the command doesnt return a result.
        /// Throws <see cref="JsonReaderException"/> if read fails.
        /// </summary>
        public T ReadResult<T>() {
            var ok = IsOk("CommandTask.ReadResult", out Exception e);
            if (ok) {
                using (var pooled = pool.ObjectMapper.Get()) {
                    var reader  = pooled.instance.reader;
                    var resultValue = reader.Read<T>(result);
                    if (reader.Success)
                        return resultValue;
                    var error = reader.Error;
                    throw new JsonReaderException (error.msg.AsString(), error.Pos);
                }
            }
            throw e;
        }
        
        /// <summary>
        /// Return a type safe result of a command.
        /// The result is null if the command doesnt return a result.
        /// Return false if read fails and set <paramref name="error"/>.
        /// </summary>
        public bool TryReadResult<T>(out T resultValue, out JsonReaderException error) {
            var ok = IsOk("CommandTask.TryReadResult", out Exception e);
            if (ok) {
                using (var pooled = pool.ObjectMapper.Get()) {
                    var reader  = pooled.instance.reader;
                    resultValue = reader.Read<T>(result);
                    if (reader.Success) {
                        error = null;
                        return true;
                    }
                    var readError = reader.Error;
                    error = new JsonReaderException (readError.msg.AsString(), readError.Pos);
                    return false;
                }
            }
            throw e;
        }
    }

    /// <summary>
    /// A <see cref="CommandTask{TResult}"/> is created when a command is send to an <see cref="EntityDatabase"/> by
    /// <see cref="FlioxClient.SendCommand{TResult}"/>.<br/>
    /// Its <see cref="Result"/> is available after calling <see cref="FlioxClient.SyncTasks"/>. <br/>
    /// Additional to a <see cref="MessageTask"/> a <see cref="CommandTask{TResult}"/> also provide type safe access
    /// to the command <see cref="Result"/> after the task is synced successful.
    /// </summary>
    public sealed class CommandTask<TResult> : CommandTask
    {
        public              TResult         Result => ReadResult<TResult>();
        
        internal CommandTask(string name, JsonValue param, Pool pool) : base (name, param, pool) { }
    }
}

