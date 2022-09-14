﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.


using System;

namespace Friflo.Json.Fliox.Hub.Remote.WebSockets
{
    /// <summary>
    /// RFC 6455: The WebSocket Protocol https://www.rfc-editor.org/rfc/rfc6455#section-5.2 <br/>
    /// RFC 6455 in pretty: https://greenbytes.de/tech/webdav/rfc6455.html#baseframing
    /// </summary>
    [Flags]
    internal enum FrameFlags {
        Fin                 = 0x80,
        Rsv1                = 0x40,
        Rsv2                = 0x20,
        Rsv3                = 0x10,
        //
        Opcode              = 0x0f,
    }

    /// <summary> RFC 6455: The WebSocket Protocol https://www.rfc-editor.org/rfc/rfc6455#section-5.2 - Opcode</summary>
    internal enum Opcode {
        ContinuationFrame   = 0x00,
        TextFrame           = 0x01,
        BinaryFrame         = 0x02,
        ConnectionClose     = 0x08,
        Ping                = 0x09,
        Pong                = 0x0a
    }

    /// <summary> RFC 6455: The WebSocket Protocol https://www.rfc-editor.org/rfc/rfc6455#section-5.2 - Mask</summary>
    [Flags]
    internal enum LenFlags {
        Mask                = 0x80
    }
}