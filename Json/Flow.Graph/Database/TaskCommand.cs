﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Mapper.Map.Val;

namespace Friflo.Json.Flow.Database
{
    public readonly struct Command<TValue> {
        public              string          Name    { get; }
        public              TValue          Value   => reader.Read<TValue>(json);
        
        private readonly    string          json;
        
        private readonly    ObjectReader    reader;
        
        internal Command(string name, string json, ObjectReader reader) {
            Name        = name;
            this.json   = json;  
            this.reader = reader;
        }
    }
    
    public delegate TResult CommandHandler<TValue, out TResult>(Command<TValue> command);
    
    internal abstract class CommandCallback
    {
        internal abstract Task<string> InvokeCallback(ObjectMapper mapper, string messageName, JsonValue messageValue);
    }
    
    internal class CommandCallback<TValue, TResult> : CommandCallback
    {
        private  readonly   string                                  name;
        private  readonly   CommandHandler<TValue, TResult>         handler;

        public   override   string                                  ToString() => name;

        internal CommandCallback (string name, CommandHandler<TValue, TResult> handler) {
            this.name       = name;
            this.handler    = handler;
        }
        
        internal override Task<string> InvokeCallback(ObjectMapper mapper, string messageName, JsonValue messageValue) {
            var     cmd     = new Command<TValue>(messageName, messageValue.json, mapper.reader);
            TResult result  = handler(cmd);
            var jsonResult  = mapper.Write(result);
            return Task.FromResult(jsonResult);
        }
    }
    
    internal class CommandAsyncCallback<TValue, TResult> : CommandCallback
    {
        private  readonly   string                                  name;
        private  readonly   CommandHandler<TValue, Task<TResult>>   handler;

        public   override   string                                  ToString() => name;

        internal CommandAsyncCallback (string name, CommandHandler<TValue, Task<TResult>> handler) {
            this.name       = name;
            this.handler    = handler;
        }
        
        internal override async Task<string> InvokeCallback(ObjectMapper mapper, string messageName, JsonValue messageValue) {
            var     cmd     = new Command<TValue>(messageName, messageValue.json, mapper.reader);
            TResult result  = await handler(cmd);
            var jsonResult  = mapper.Write(result);
            return jsonResult;
        }
    }
}