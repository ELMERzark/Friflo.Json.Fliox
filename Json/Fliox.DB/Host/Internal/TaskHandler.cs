﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.Protocol;
using Friflo.Json.Fliox.DB.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.DB.Host.Internal
{
    public class TaskHandler
    {
        private readonly Dictionary<string, CommandCallback> commands = new Dictionary<string, CommandCallback>();
        
        public void AddCommandHandler<TValue, TResult>(string name, CommandHandler<TValue, TResult> handler) {
            var command = new CommandCallback<TValue, TResult>(name, handler);
            commands.Add(name, command);
        }
        
        public void AddCommandHandler<TValue, TResult>(CommandHandler<TValue, TResult> handler) {
            var name = typeof(TValue).Name;
            var command = new CommandCallback<TValue, TResult>(name, handler);
            commands.Add(name, command);
        }
        
        public void AddCommandHandlerAsync<TValue, TResult>(string name, CommandHandler<TValue, Task<TResult>> handler) {
            var command = new CommandAsyncCallback<TValue, TResult>(name, handler);
            commands.Add(name, command);
        }
        
        public void AddCommandHandlerAsync<TValue, TResult>(CommandHandler<TValue, Task<TResult>> handler) {
            var name = typeof(TValue).Name;
            var command = new CommandAsyncCallback<TValue, TResult>(name, handler);
            commands.Add(name, command);
        }

        private static bool AuthorizeTask(SyncRequestTask task, MessageContext messageContext, out SyncTaskResult error) {
            var authorizer = messageContext.authState.authorizer;
            if (authorizer.Authorize(task, messageContext)) {
                error = null;
                return true;
            }
            var message = "not authorized";
            var authError = messageContext.authState.error; 
            if (authError != null) {
                message = $"{message}. {authError}";
            }
            error = SyncRequestTask.PermissionDenied(message);
            return false;
        }
        
        public virtual async Task<SyncTaskResult> ExecuteTask (SyncRequestTask task, EntityDatabase database, SyncResponse response, MessageContext messageContext) {
            if (!AuthorizeTask(task, messageContext, out var error)) {
                return error;
            }
            if (task is SendMessage cmd) {
                if (commands.TryGetValue(cmd.name, out CommandCallback callback)) {
                    using (var pooledMapper = messageContext.pools.ObjectMapper.Get()) {
                        var jsonResult  = await callback.InvokeCallback(pooledMapper.instance, cmd.name, cmd.value).ConfigureAwait(false);
                        return new SendMessageResult { result = new JsonValue { json = jsonResult } };
                    }
                }
            }
            var result = await task.Execute(database, response, messageContext).ConfigureAwait(false);
            return result;
        }
    }
}