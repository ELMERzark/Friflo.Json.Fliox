// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.WebRTC
{
    // ---------------------------------- entity models ----------------------------------
    public sealed class WebRtcHost {
        /// <summary>WebRTC Host id</summary>
        [Required]  public  string      id;
        [Required]  public  string      client;
                        
        public override     string      ToString() => JsonSerializer.Serialize(this).Replace("\"", "'");
    }
    
    // ---------------------------- command models - aka DTO's ---------------------------
    // --- register host
    /// request: WebRTC Host -> Signaling Server
    public sealed class RegisterHost {
        [Required]  public  string      name;
    }
    
    /// response: Signaling Server -> WebRTC Host 
    public sealed class RegisterHostResult { }
    
    // --- connect client
    /// request: WebRTC Client -> Signaling Server
    public sealed class ConnectClient {
        [Required]  public  string      name;
        [Required]  public  string      offerSDP;
    }
    
    /// response: Signaling Server -> WebRTC Client
    public sealed class ConnectClientResult {
        [Required]  public  string      answerSDP;
    }
    
    // ------------------------------------ event models ---------------------------------
    /// Signaling Server -> WebRTC Host
    public sealed class Offer {
        [Required]  public  ShortString client;
        [Required]  public  string      sdp;
    }
    
    /// WebRTC Host -> Signaling Server 
    public sealed class Answer {
        [Required]  public  ShortString client;
        [Required]  public  string      sdp;
    }
    
    /// WebRTC Client -> WebRTC Host
    public sealed class ClientIce {
        [Required]  public  JsonValue   candidate;
    }
    
    /// WebRTC Host -> WebRTC Client 
    public sealed class HostIce {
        [Required]  public  JsonValue   candidate;
    }
}