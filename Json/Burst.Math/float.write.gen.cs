// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using Unity.Mathematics;

#if JSON_BURST
    using Str32 = Unity.Collections.FixedString32;
#else
    using Str32 = System.String;
#endif

// ========================== Code generated by Friflo.Json.Burst.Math.CodeGen ==========================

// ReSharper disable InconsistentNaming
namespace Friflo.Json.Burst.Math
{
    public static partial class Json
    {
        // --------------------------------------- vectors ----------------------------------------------
        public static void MemberFloat2(this ref JsonSerializer s, in Str32 key, in float2 value) {
            s.MemberArrayStart(key, false);
            WriteFloat2(ref s, in value);
            s.ArrayEnd();
        }

        public static void ArrayFloat2(this ref JsonSerializer s, in float2 value) {
            s.ArrayStart(false);
            WriteFloat2(ref s, in value);
            s.ArrayEnd();
        }

        private static void WriteFloat2(ref JsonSerializer s, in float2 value) {
            s.ElementDbl(value.x);
            s.ElementDbl(value.y);
        }

        public static void MemberFloat3(this ref JsonSerializer s, in Str32 key, in float3 value) {
            s.MemberArrayStart(key, false);
            WriteFloat3(ref s, in value);
            s.ArrayEnd();
        }

        public static void ArrayFloat3(this ref JsonSerializer s, in float3 value) {
            s.ArrayStart(false);
            WriteFloat3(ref s, in value);
            s.ArrayEnd();
        }

        private static void WriteFloat3(ref JsonSerializer s, in float3 value) {
            s.ElementDbl(value.x);
            s.ElementDbl(value.y);
            s.ElementDbl(value.z);
        }

        public static void MemberFloat4(this ref JsonSerializer s, in Str32 key, in float4 value) {
            s.MemberArrayStart(key, false);
            WriteFloat4(ref s, in value);
            s.ArrayEnd();
        }

        public static void ArrayFloat4(this ref JsonSerializer s, in float4 value) {
            s.ArrayStart(false);
            WriteFloat4(ref s, in value);
            s.ArrayEnd();
        }

        private static void WriteFloat4(ref JsonSerializer s, in float4 value) {
            s.ElementDbl(value.x);
            s.ElementDbl(value.y);
            s.ElementDbl(value.z);
            s.ElementDbl(value.w);
        }

        // ------------------------------ matrices (rows x columns) -------------------------------------
        public static void MemberFloat2x2(this ref JsonSerializer s, in Str32 key, in float2x2 value) {
            s.MemberArrayStart(key, true);
            ArrayFloat2(ref s, in value.c0);
            ArrayFloat2(ref s, in value.c1);
            s.ArrayEnd();
        }

        public static void MemberFloat2x3(this ref JsonSerializer s, in Str32 key, in float2x3 value) {
            s.MemberArrayStart(key, true);
            ArrayFloat2(ref s, in value.c0);
            ArrayFloat2(ref s, in value.c1);
            ArrayFloat2(ref s, in value.c2);
            s.ArrayEnd();
        }

        public static void MemberFloat2x4(this ref JsonSerializer s, in Str32 key, in float2x4 value) {
            s.MemberArrayStart(key, true);
            ArrayFloat2(ref s, in value.c0);
            ArrayFloat2(ref s, in value.c1);
            ArrayFloat2(ref s, in value.c2);
            ArrayFloat2(ref s, in value.c3);
            s.ArrayEnd();
        }

        public static void MemberFloat3x2(this ref JsonSerializer s, in Str32 key, in float3x2 value) {
            s.MemberArrayStart(key, true);
            ArrayFloat3(ref s, in value.c0);
            ArrayFloat3(ref s, in value.c1);
            s.ArrayEnd();
        }

        public static void MemberFloat3x3(this ref JsonSerializer s, in Str32 key, in float3x3 value) {
            s.MemberArrayStart(key, true);
            ArrayFloat3(ref s, in value.c0);
            ArrayFloat3(ref s, in value.c1);
            ArrayFloat3(ref s, in value.c2);
            s.ArrayEnd();
        }

        public static void MemberFloat3x4(this ref JsonSerializer s, in Str32 key, in float3x4 value) {
            s.MemberArrayStart(key, true);
            ArrayFloat3(ref s, in value.c0);
            ArrayFloat3(ref s, in value.c1);
            ArrayFloat3(ref s, in value.c2);
            ArrayFloat3(ref s, in value.c3);
            s.ArrayEnd();
        }

        public static void MemberFloat4x2(this ref JsonSerializer s, in Str32 key, in float4x2 value) {
            s.MemberArrayStart(key, true);
            ArrayFloat4(ref s, in value.c0);
            ArrayFloat4(ref s, in value.c1);
            s.ArrayEnd();
        }

        public static void MemberFloat4x3(this ref JsonSerializer s, in Str32 key, in float4x3 value) {
            s.MemberArrayStart(key, true);
            ArrayFloat4(ref s, in value.c0);
            ArrayFloat4(ref s, in value.c1);
            ArrayFloat4(ref s, in value.c2);
            s.ArrayEnd();
        }

        public static void MemberFloat4x4(this ref JsonSerializer s, in Str32 key, in float4x4 value) {
            s.MemberArrayStart(key, true);
            ArrayFloat4(ref s, in value.c0);
            ArrayFloat4(ref s, in value.c1);
            ArrayFloat4(ref s, in value.c2);
            ArrayFloat4(ref s, in value.c3);
            s.ArrayEnd();
        }
    }
}
