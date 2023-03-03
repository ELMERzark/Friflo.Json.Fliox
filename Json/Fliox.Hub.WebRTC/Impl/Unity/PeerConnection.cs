// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if UNITY_5_3_OR_NEWER

using System;
using System.Threading.Tasks;
using Unity.WebRTC;

// ReSharper disable InconsistentNaming
// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.WebRTC.Impl
{
    internal sealed class PeerConnection
    {
        private     readonly    RTCPeerConnection           impl;
        internal                SignalingState              SignalingState => GetSignalingState();          
        internal    event       Action<PeerConnectionState> OnConnectionStateChange;
        internal    event       Action<DataChannel>         OnDataChannel;
        internal    event       Action<IceCandidate>        OnIceCandidate;

        internal PeerConnection (WebRtcConfig config) {
            impl = new RTCPeerConnection(ref config.Get().impl);
            impl.OnConnectionStateChange += state => {
                PeerConnectionState connState;
                switch (state) {
                    case RTCPeerConnectionState.Closed:         connState = PeerConnectionState.closed;         break;
                    case RTCPeerConnectionState.Failed:         connState = PeerConnectionState.failed;         break;
                    case RTCPeerConnectionState.Disconnected:   connState = PeerConnectionState.disconnected;   break;
                    case RTCPeerConnectionState.New:            connState = PeerConnectionState.@new;           break;
                    case RTCPeerConnectionState.Connecting:     connState = PeerConnectionState.connecting;     break;
                    case RTCPeerConnectionState.Connected:      connState = PeerConnectionState.connected;      break;
                    default:
                        throw new InvalidOperationException($"unexpected connection state: {state}");
                    
                }
                OnConnectionStateChange?.Invoke(connState);
            };
            impl.OnDataChannel += channel => {
                var dc = new DataChannel(channel);
                OnDataChannel?.Invoke(dc);
            };
            impl.OnIceCandidate += candidate => {
                OnIceCandidate?.Invoke(new IceCandidate(candidate));
            };
        }
        
        internal Task<DataChannel> CreateDataChannel(string label) {
            var init = new RTCDataChannelInit { ordered = true, maxPacketLifeTime = 10000 };
            var dcImpl = impl.CreateDataChannel(label, init);
            var dc = new DataChannel(dcImpl);
            return Task.FromResult(dc);
        }
        
        internal async Task<SessionDescription> CreateOffer() {
            var asyncOp = impl.CreateOffer();
            await UnityWebRtc.Singleton.Await(asyncOp).ConfigureAwait(false);
            return new SessionDescription(asyncOp.Desc);  
        }
        
        internal async Task<SessionDescription> CreateAnswer() {
            var asyncOp = impl.CreateAnswer();
            await UnityWebRtc.Singleton.Await(asyncOp).ConfigureAwait(false);
            return new SessionDescription(asyncOp.Desc);  
        }
        
        internal async Task<string> SetRemoteDescription(SessionDescription desc) {
            var asyncOp = impl.SetRemoteDescription(ref desc.impl);
            await UnityWebRtc.Singleton.Await(asyncOp).ConfigureAwait(false);
            if (!asyncOp.IsError) {
                return null;
            }
            return asyncOp.Error.errorType.ToString();
        }
        
        internal async Task SetLocalDescription(SessionDescription desc) {
            var asyncOp = impl.SetLocalDescription(ref desc.impl); 
            await UnityWebRtc.Singleton.Await(asyncOp).ConfigureAwait(false);
        }
        
        internal void AddIceCandidate(IceCandidate candidate) {
            impl.AddIceCandidate(candidate.impl);
        }
        
        private SignalingState GetSignalingState() {
            switch (impl.SignalingState) {
                case RTCSignalingState.Stable:              return SignalingState.Stable;
                case RTCSignalingState.HaveLocalOffer:      return SignalingState.HaveLocalOffer;
                case RTCSignalingState.HaveLocalPrAnswer:   return SignalingState.HaveLocalPrAnswer;
                case RTCSignalingState.HaveRemoteOffer:     return SignalingState.HaveRemoteOffer;
                case RTCSignalingState.HaveRemotePrAnswer:  return SignalingState.HaveRemotePrAnswer;
                case RTCSignalingState.Closed:              return SignalingState.Closed;
                    default: throw new InvalidOperationException($"unexpected signaling state: {impl.SignalingState}");
            }
        }
    }
    
    internal sealed class SessionDescription
    {
        internal    RTCSessionDescription   impl;
        internal    string                  sdp { get => impl.sdp; init => impl.sdp = value; }
        internal    SdpType                 type {
            get {
                switch (impl.type) {
                    case RTCSdpType.Answer: return SdpType.answer;
                    case RTCSdpType.Offer:  return SdpType.offer;
                    default: throw new InvalidOperationException($"unexpected type: {impl.type}");
                }
            }
            init {
                switch (value) {
                    case SdpType.answer:    impl.type = RTCSdpType.Answer;  return;
                    case SdpType.offer:     impl.type = RTCSdpType.Offer;   return;
                    default: throw new InvalidOperationException($"unexpected type: {value}");
                }
            }
        }
        
        internal SessionDescription() {
            impl = new RTCSessionDescription();
        }

        internal SessionDescription(in RTCSessionDescription impl) {
            this.impl = impl;
        }
    }
    
    internal sealed class IceCandidate
    {
        
        internal readonly RTCIceCandidate impl;
        
        internal IceCandidate(RTCIceCandidate impl) {
            this.impl = impl;
        }
        
        internal string ToJson() {
            var model = new IceCandidateModel {
                candidate           = impl.Candidate,
                sdpMid              = impl.SdpMid,
                sdpMLineIndex       = impl.SdpMLineIndex,
                usernameFragment    = impl.UserNameFragment
            };
            return JsonSerializer.Serialize(model);
        }

        public static bool TryParse(string value, out IceCandidate candidate) {
            var model   = JsonSerializer.Deserialize<IceCandidateModel>(value);
            var init    = new RTCIceCandidateInit {
                candidate           = model.candidate,
                sdpMid              = model.sdpMid,
                sdpMLineIndex       = model.sdpMLineIndex,
            };
            var iceCandidate    = new RTCIceCandidate (init);
            candidate           = new IceCandidate(iceCandidate);
            return true;
        }
    }
    
    internal sealed class IceCandidateModel {
        public  string  candidate;
        public  string  sdpMid;
        public  int?    sdpMLineIndex;
        public  string  usernameFragment;
    }
}

#endif
