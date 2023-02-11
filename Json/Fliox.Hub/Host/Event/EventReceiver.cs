// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Hub.Protocol;

namespace Friflo.Json.Fliox.Hub.Host.Event
{
    /// <summary>
    /// An <see cref="EventReceiver"/> is used to send events to clients they have subscribed before.<br/>
    /// A single <see cref="EventReceiver"/> can be shared by multiple clients to enable using a single
    /// remote connection. To address a specific client in case of a shared remote connection the
    /// <see cref="ClientEvent.dstClientId"/> is used.
    /// </summary>
    public abstract class EventReceiver {
        protected internal abstract bool    IsOpen ();
        protected internal abstract bool    IsRemoteTarget ();
        /// <summary>Send a serialized <see cref="EventMessage"/> to the receiver</summary>
        protected internal abstract void    SendEvent(in ClientEvent clientEvent);
    }
    
    public readonly struct ClientEvent
    {
        /// <summary>the <see cref="ProtocolEvent.dstClientId"/> of the <see cref="message"/></summary>
        public  readonly    ShortString dstClientId;
        /// <summary>serialized <see cref="EventMessage"/></summary>
        public  readonly    JsonValue   message;

        public  override    string      ToString() => $"client: {dstClientId}";

        public ClientEvent(in ShortString dstClientId, in JsonValue message) {
            this.dstClientId    = dstClientId;
            this.message        = message;
        }
    }
}