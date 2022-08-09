﻿using Unity.Mathematics;

#if JSON_BURST
    using Str32 = Unity.Collections.FixedString32;
#else
    using Str32 = System.String;
#endif

namespace Friflo.Json.Burst.Math.Tests
{
    public struct MathTypes {
        public  float2      float2;
        public  float3      float3;
        public  float4      float4;
        public  float4x4    float4x4;

        public void InitSample() {
            float2      = new float2(1, 2);
            float3      = new float3(1, 2, 3);
            float4      = new float4(1, 2, 3, 4);
            float4x4      = new float4x4(
                new float4( 1,  2,  3,  4),
                new float4(11, 12, 13, 14),
                new float4(21, 22, 23, 24),
                new float4(31, 32, 33, 34));
        }
    }

    // Using a struct containing JSON key names enables using them by ref to avoid memcpy
    public struct MathKeys {
        public Str32    float2;
        public Str32    float3;
        public Str32    float4;
        public Str32    float4x4;

        public MathKeys(Default _) {
            float2      = "float2";
            float3      = "float3";
            float4      = "float4";
            float4x4    = "float4x4";
        }
    }
}