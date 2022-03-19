﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Hub.Remote;
using Friflo.Json.Fliox.Mapper;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable InconsistentNaming
namespace Friflo.Json.Fliox.Hub.Protocol
{
    // ----------------------------------- message -----------------------------------
    /// <summary>
    /// <see cref="ProtocolMessage"/> is the base type for all messages which are classified into request, response and event.
    /// It can also be used in communication going beyond the request / response schema.
    /// <br/>
    /// A <see cref="ProtocolMessage"/> is either one of the following types:
    /// <list type="bullet">
    ///   <item> <see cref="ProtocolRequest"/>  send by clients / received by hosts</item>
    ///   <item> <see cref="ProtocolResponse"/> send by hosts / received by clients</item>
    ///   <item> <see cref="ProtocolEvent"/>    send by hosts / received by clients</item>
    /// </list>
    /// <i>Note</i>: By applying this classification the protocol can also be used in peer-to-peer networking.
    /// <para>
    ///     General principle of <see cref="Fliox"/> message protocol:<br/>
    ///     All messages like requests (their tasks), responses (their results) and events are stateless.<br/>
    ///     In other words: All messages are self-contained and doesnt (and must not) rely and previous sent messages.
    ///     The technical aspect of having a connection e.g. HTTP or WebSocket is not relevant.
    ///     This enables two fundamental features:<br/>
    ///     1. embedding all messages in various communication protocols like HTTP, WebSockets, TCP, WebRTC or datagram based protocols.<br/>
    ///     2. multiplexing of messages from different clients, servers or peers in a shared connection.<br/>
    ///     This also means all <see cref="Fliox"/> messages doesnt (and must not) require a session.<br/>
    ///     This principle also enables using a single <see cref="FlioxHub"/> by multiple clients like
    ///     <see cref="Client.FlioxClient"/> even for remote clients like <see cref="RemoteClientHub"/>.
    /// </para>
    /// </summary>
    [Fri.Discriminator("msg", Description = "message type")] 
    [Fri.Polymorph(typeof(SyncRequest),    Discriminant = "sync")]
    [Fri.Polymorph(typeof(SyncResponse),   Discriminant = "resp")]
    [Fri.Polymorph(typeof(ErrorResponse),  Discriminant = "error")]
    [Fri.Polymorph(typeof(EventMessage),   Discriminant = "ev")]
    public abstract class ProtocolMessage
    {
        internal abstract   MessageType     MessageType { get; }
        
        public static Type[] Types => new [] { typeof(ProtocolMessage), typeof(ProtocolRequest), typeof(ProtocolResponse), typeof(ProtocolEvent) }; 
    }
    
    // ----------------------------------- request -----------------------------------
    [Fri.Discriminator("msg", Description = "request type")] 
    [Fri.Polymorph(typeof(SyncRequest),         Discriminant = "sync")]
    public abstract class ProtocolRequest   : ProtocolMessage {
        /// <summary>Used only for <see cref="RemoteClientHub"/> to enable:
        /// <para>
        ///   1. Out of order response handling for their corresponding requests.
        /// </para>
        /// <para>
        ///   2. Multiplexing of requests and their responses for multiple clients e.g. <see cref="Client.FlioxClient"/>
        ///      using the same connection.
        ///      This is not a common scenario but it enables using a single <see cref="WebSocketClientHub"/>
        ///      used by multiple clients.
        /// </para>
        /// The host itself only echos the <see cref="reqId"/> to <see cref="ProtocolResponse.reqId"/> and
        /// does <b>not</b> utilize it internally.
        /// </summary>
        [Fri.Property(Name =               "req")]
                        public  int?        reqId       { get; set; }
        /// <summary>As a user can access a <see cref="FlioxHub"/> by multiple clients the <see cref="clientId"/>
        /// enables identifying each client individually. <br/>
        /// The <see cref="clientId"/> is used for <see cref="SubscribeMessage"/> and <see cref="SubscribeChanges"/>
        /// to enable sending <see cref="EventMessage"/>'s to the desired subscriber.
        /// </summary>
        [Fri.Property(Name =               "clt")]
                        public  JsonKey     clientId    { get; set; }
    }
    
    // ----------------------------------- response -----------------------------------
    /// <summary>
    /// Base type for response messages send from a host to a client in reply of <see cref="SyncRequest"/><br/>
    /// A response is either a <see cref="SyncResponse"/> or a <see cref="ErrorResponse"/> in case of a general error. 
    /// </summary>
    [Fri.Discriminator("msg", Description = "response type")] 
    [Fri.Polymorph(typeof(SyncResponse),        Discriminant = "resp")]
    [Fri.Polymorph(typeof(ErrorResponse),       Discriminant = "error")]
    public abstract class ProtocolResponse : ProtocolMessage {
        /// <summary>Set to the value of the corresponding <see cref="ProtocolRequest.reqId"/> of a <see cref="ProtocolRequest"/></summary>
        [Fri.Property(Name =               "req")]
                        public  int?        reqId       { get; set; }
        /// <summary>
        /// Set to <see cref="ProtocolRequest.clientId"/> of a <see cref="SyncRequest"/> in case the given
        /// <see cref="ProtocolRequest.clientId"/> was valid. Otherwise it is set to null. <br/>
        /// Calling <see cref="Host.Auth.Authenticator.EnsureValidClientId"/> when <see cref="clientId"/> == null a
        /// new unique client id will be assigned. <br/>
        /// For tasks which require a <see cref="clientId"/> a client need to set <see cref="ProtocolRequest.clientId"/>
        /// to <see cref="clientId"/>. <br/>
        /// This enables tasks like <see cref="SubscribeMessage"/> or <see cref="SubscribeChanges"/> identifying the
        /// <see cref="EventMessage"/> target. 
        /// </summary>
        [Fri.Property(Name =               "clt")]
                        public  JsonKey     clientId    { get; set; }
    }
    
    // ----------------------------------- event -----------------------------------
    [Fri.Discriminator("msg", Description = "event type")] 
    [Fri.Polymorph(typeof(EventMessage),   Discriminant = "ev")]
    public abstract class ProtocolEvent     : ProtocolMessage {
        // note for all fields
        // used { get; set; } to force properties on the top of JSON
        
        /// Increasing event sequence number starting with 1 for a specific target client <see cref="dstClientId"/>.
        /// Each target client (subscriber) has its own sequence.
                        public  int         seq         { get; set; }
        /// The user which caused the event. Specifically the user which made a database change or sent a message / command.
        /// The user client is not preserved by en extra property as a use case for this is not obvious.
        [Fri.Property(Name =               "src")]
        [Fri.Required]  public  JsonKey     srcUserId   { get; set; }
        
        /// The target client the event is sent to. This enabled sharing a single (WebSocket) connection by multiple clients.
        /// In many scenarios this property is redundant as every client uses a WebSocket exclusively.
        [Fri.Property(Name =               "clt")]
        [Fri.Required]  public  JsonKey     dstClientId { get; set; }
    }
    
    /// <summary>
    /// The general message types used in the Protocol
    /// </summary>
    public enum MessageType
    {
        /// <summary>event message - send from host to clients with subscriptions</summary>
        ev,
        /// <summary>request - send from a client to a host</summary>
        sync,
        /// <summary>response - send from a host to a client in reply of a request</summary>
        resp,
        /// <summary>response error - send from a host to a client in reply of a request</summary>
        error
    }
    
    /// <summary>
    /// Annotated fields are only available for debugging ergonomics.
    /// They are not not used by the library in any way as they represent redundant information.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class DebugInfoAttribute : Attribute { }
}