﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Mapper.Map.Val;
using Friflo.Json.Flow.Sync;

namespace Friflo.Json.Flow.Graph.Internal
{
    internal class MessageSubscriber
    {
        internal readonly   bool                    isPrefix;
        internal readonly   string                  name;
        internal readonly   List<MessageCallback>   callbackHandlers = new List<MessageCallback>();

        public   override   string                  ToString() => name;

        internal MessageSubscriber (string name) {
            var prefix = SubscribeMessage.GetPrefix(name);
            isPrefix = prefix != null;
            if (isPrefix) {
                this.name = prefix;
            } else {
                this.name = name;
            }
        }
        
        internal void InvokeCallbacks(ObjectReader reader, JsonValue messageValue) {
            foreach (var callbackHandler in callbackHandlers) {
                try {
                    callbackHandler.InvokeCallback(reader, messageValue);
                }
                catch (Exception e) {
                    var type = callbackHandler.GetType().Name;
                    var msg = $"{type} failed. name: {callbackHandler.name}, exception: {e}";
                    Console.WriteLine(msg);
                    Debug.Fail(msg);
                }
            }
        }
    }
    
    internal abstract class MessageCallback {
        internal readonly   string  name;
        private  readonly   object  handlerObject;
        
        internal            bool    HasHandler (object handler) => handler == handlerObject;
        public   override   string  ToString()                  => name;

        internal abstract void InvokeCallback(ObjectReader reader, JsonValue messageValue);
        
        internal MessageCallback (string name, object handler) {
            this.name       = name;
            handlerObject   = handler;
        } 
    }
    
    internal class NonGenericMessageCallback : MessageCallback
    {
        private  readonly   Handler   handler;
        
        internal NonGenericMessageCallback (string name, Handler handler) : base(name, handler) {
            this.handler = handler;
        }
        
        internal override void InvokeCallback(ObjectReader reader, JsonValue messageValue) {
            var msg = new Message(name, messageValue.json, reader);
            handler(msg);
        }
    }
    
    internal class GenericMessageCallback<TValue> : MessageCallback
    {
        private  readonly   Handler<TValue>   handler;
        
        internal GenericMessageCallback (string name, Handler<TValue> handler) : base(name, handler) {
            this.handler = handler;
        }
        
        internal override void InvokeCallback(ObjectReader reader, JsonValue messageValue) {
            var msg = new Message<TValue>(name, messageValue.json, reader);
            handler(msg);
        }
    }
}