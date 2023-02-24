// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER

using System.Collections.Generic;
using SIPSorcery.Net;

namespace Friflo.Json.Fliox.Hub.WebRTC
{
    public partial class WebRtcConfig
    {
        internal RTCConfiguration GetRtcConfiguration() {
            var config = new RTCConfiguration {
                iceServers = new List<RTCIceServer> { new RTCIceServer { urls = StunUrl } }
            };
            return config;
        }
    }
}

namespace System.Runtime.CompilerServices
{
    // This is needed to enable following features in .NET framework and .NET core <= 3.1 projects:
    // - init only setter properties. See [Init only setters - C# 9.0 draft specifications | Microsoft Learn] https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-9.0/init
    // - record types
    internal static class IsExternalInit { }
}
#endif