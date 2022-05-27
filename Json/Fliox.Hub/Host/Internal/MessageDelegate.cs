﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.Hub.Host.Internal
{
    internal readonly struct InvokeResult
    {
        internal readonly   JsonValue   value;
        internal readonly   string      error;

        public override     string      ToString() => error ?? value.AsString();

        internal InvokeResult(byte[] value) {
            this.value  = new JsonValue(value);
            this.error  = null;
        }
        
        internal InvokeResult(string error) {
            this.value  = default;
            this.error  = error;
        }
    }
    
    internal enum MsgType {
        Command,
        Message
    }
    
    // ----------------------------------- MessageDelegate -----------------------------------
    internal abstract class MessageDelegate
    {
        // Note! Must not contain any mutable state
        private   readonly  string              name;
        internal  abstract  MsgType             MsgType { get; }  
        public    override  string              ToString()  => name;
        
        // return type could be a ValueTask but Unity doesnt support this. 2021-10-25
        internal  abstract  Task<InvokeResult>  InvokeDelegate(string messageName, JsonValue messageValue, SyncContext syncContext);
        
        protected MessageDelegate (string name) {
            this.name   = name;
        }
    }
    
    // ----------------------------------- MessageDelegate<> -----------------------------------
    internal sealed class MessageDelegate<TValue> : MessageDelegate
    {
        private  readonly   HostMessageHandler<TValue>  handler;
        internal override   MsgType                     MsgType     => MsgType.Message;

        internal MessageDelegate (string name, HostMessageHandler<TValue> handler) : base(name){
            this.handler    = handler;
        }
        
        internal override Task<InvokeResult> InvokeDelegate(string messageName, JsonValue messageValue, SyncContext syncContext) {
            var cmd     = new MessageContext(messageName,  syncContext);
            var param   = new Param<TValue> (messageValue, syncContext); 
            handler(param, cmd);
            
            var error = cmd.error;
            if (error != null) {
                return Task.FromResult(new InvokeResult(error));
            }
            return Task.FromResult(new InvokeResult((byte[])null));
        }
    }
    
    // ----------------------------------- MessageAsyncDelegate<> -----------------------------------
    internal sealed class MessageAsyncDelegate<TParam> : MessageDelegate
    {
        private  readonly   HostMessageHandlerAsync<TParam>     handler;
        internal override   MsgType                             MsgType     => MsgType.Message;

        internal MessageAsyncDelegate (string name, HostMessageHandlerAsync<TParam> handler) : base(name) {
            this.handler    = handler;
        }
        
        internal override async Task<InvokeResult> InvokeDelegate(string messageName, JsonValue messageValue, SyncContext syncContext) {
            var cmd     = new MessageContext(messageName,  syncContext);
            var param   = new Param<TParam> (messageValue, syncContext); 
            await handler(param, cmd).ConfigureAwait(false);
            
            var error   = cmd.error;
            if (error != null) {
                return new InvokeResult(error);
            }
            return new InvokeResult((byte[])null);
        }
    }
    
    // ----------------------------------- CommandDelegate<,> -----------------------------------
    internal sealed class CommandDelegate<TValue, TResult> : MessageDelegate
    {
        private  readonly   HostCommandHandler<TValue, TResult> handler;
        internal override   MsgType                             MsgType     => MsgType.Command;

        internal CommandDelegate (string name, HostCommandHandler<TValue, TResult> handler) : base(name){
            this.handler    = handler;
        }
        
        internal override Task<InvokeResult> InvokeDelegate(string messageName, JsonValue messageValue, SyncContext syncContext) {
            var cmd     = new MessageContext(messageName,  syncContext);
            var param   = new Param<TValue> (messageValue, syncContext); 
            TResult result  = handler(param, cmd);
            
            var error = cmd.error;
            if (error != null) {
                return Task.FromResult(new InvokeResult(error));
            }
            using (var pooled = syncContext.ObjectMapper.Get()) {
                var writer = pooled.instance.writer;
                writer.WriteNullMembers = cmd.WriteNull;
                writer.Pretty           = cmd.WritePretty;
                var jsonResult          = writer.WriteAsArray(result);
                return Task.FromResult(new InvokeResult(jsonResult));
            }
        }
    }
    
    // ----------------------------------- CommandAsyncDelegate<,> -----------------------------------
    internal sealed class CommandAsyncDelegate<TParam, TResult> : MessageDelegate
    {
        private  readonly   HostCommandHandler<TParam, Task<TResult>>   handler;

        internal override   MsgType                                     MsgType     => MsgType.Command;

        internal CommandAsyncDelegate (string name, HostCommandHandler<TParam, Task<TResult>> handler) : base(name) {
            this.handler    = handler;
        }
        
        internal override async Task<InvokeResult> InvokeDelegate(string messageName, JsonValue messageValue, SyncContext syncContext) {
            var cmd     = new MessageContext(messageName,  syncContext);
            var param   = new Param<TParam> (messageValue, syncContext); 
            var result  = await handler(param, cmd).ConfigureAwait(false);
            
            var error   = cmd.error;
            if (error != null) {
                return new InvokeResult(error);
            }
            using (var pooled = syncContext.ObjectMapper.Get()) {
                var writer = pooled.instance;
                writer.WriteNullMembers = cmd.WriteNull;
                writer.Pretty           = cmd.WritePretty;
                var jsonResult          = writer.WriteAsArray(result);
                return new InvokeResult(jsonResult);
            }
        }
    }
}