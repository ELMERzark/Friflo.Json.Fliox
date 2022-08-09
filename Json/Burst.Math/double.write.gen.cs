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
    public static partial class JsonMath
    {
        // --------------------------------------- vectors ----------------------------------------------
        public static void MemberDouble2(this ref Utf8JsonWriter s, in Str32 key, in double2 value) {
            s.MemberArrayStart(key, false);
            WriteDouble2(ref s, in value);
            s.ArrayEnd();
        }

        public static void ArrayDouble2(this ref Utf8JsonWriter s, in double2 value) {
            s.ArrayStart(false);
            WriteDouble2(ref s, in value);
            s.ArrayEnd();
        }

        private static void WriteDouble2(ref Utf8JsonWriter s, in double2 value) {
            s.ElementDbl(value.x);
            s.ElementDbl(value.y);
        }

        public static void MemberDouble3(this ref Utf8JsonWriter s, in Str32 key, in double3 value) {
            s.MemberArrayStart(key, false);
            WriteDouble3(ref s, in value);
            s.ArrayEnd();
        }

        public static void ArrayDouble3(this ref Utf8JsonWriter s, in double3 value) {
            s.ArrayStart(false);
            WriteDouble3(ref s, in value);
            s.ArrayEnd();
        }

        private static void WriteDouble3(ref Utf8JsonWriter s, in double3 value) {
            s.ElementDbl(value.x);
            s.ElementDbl(value.y);
            s.ElementDbl(value.z);
        }

        public static void MemberDouble4(this ref Utf8JsonWriter s, in Str32 key, in double4 value) {
            s.MemberArrayStart(key, false);
            WriteDouble4(ref s, in value);
            s.ArrayEnd();
        }

        public static void ArrayDouble4(this ref Utf8JsonWriter s, in double4 value) {
            s.ArrayStart(false);
            WriteDouble4(ref s, in value);
            s.ArrayEnd();
        }

        private static void WriteDouble4(ref Utf8JsonWriter s, in double4 value) {
            s.ElementDbl(value.x);
            s.ElementDbl(value.y);
            s.ElementDbl(value.z);
            s.ElementDbl(value.w);
        }

        // ------------------------------ matrices (rows x columns) -------------------------------------
        public static void MemberDouble2x2(this ref Utf8JsonWriter s, in Str32 key, in double2x2 value) {
            s.MemberArrayStart(key, true);
            ArrayDouble2(ref s, in value.c0);
            ArrayDouble2(ref s, in value.c1);
            s.ArrayEnd();
        }

        public static void MemberDouble2x3(this ref Utf8JsonWriter s, in Str32 key, in double2x3 value) {
            s.MemberArrayStart(key, true);
            ArrayDouble2(ref s, in value.c0);
            ArrayDouble2(ref s, in value.c1);
            ArrayDouble2(ref s, in value.c2);
            s.ArrayEnd();
        }

        public static void MemberDouble2x4(this ref Utf8JsonWriter s, in Str32 key, in double2x4 value) {
            s.MemberArrayStart(key, true);
            ArrayDouble2(ref s, in value.c0);
            ArrayDouble2(ref s, in value.c1);
            ArrayDouble2(ref s, in value.c2);
            ArrayDouble2(ref s, in value.c3);
            s.ArrayEnd();
        }

        public static void MemberDouble3x2(this ref Utf8JsonWriter s, in Str32 key, in double3x2 value) {
            s.MemberArrayStart(key, true);
            ArrayDouble3(ref s, in value.c0);
            ArrayDouble3(ref s, in value.c1);
            s.ArrayEnd();
        }

        public static void MemberDouble3x3(this ref Utf8JsonWriter s, in Str32 key, in double3x3 value) {
            s.MemberArrayStart(key, true);
            ArrayDouble3(ref s, in value.c0);
            ArrayDouble3(ref s, in value.c1);
            ArrayDouble3(ref s, in value.c2);
            s.ArrayEnd();
        }

        public static void MemberDouble3x4(this ref Utf8JsonWriter s, in Str32 key, in double3x4 value) {
            s.MemberArrayStart(key, true);
            ArrayDouble3(ref s, in value.c0);
            ArrayDouble3(ref s, in value.c1);
            ArrayDouble3(ref s, in value.c2);
            ArrayDouble3(ref s, in value.c3);
            s.ArrayEnd();
        }

        public static void MemberDouble4x2(this ref Utf8JsonWriter s, in Str32 key, in double4x2 value) {
            s.MemberArrayStart(key, true);
            ArrayDouble4(ref s, in value.c0);
            ArrayDouble4(ref s, in value.c1);
            s.ArrayEnd();
        }

        public static void MemberDouble4x3(this ref Utf8JsonWriter s, in Str32 key, in double4x3 value) {
            s.MemberArrayStart(key, true);
            ArrayDouble4(ref s, in value.c0);
            ArrayDouble4(ref s, in value.c1);
            ArrayDouble4(ref s, in value.c2);
            s.ArrayEnd();
        }

        public static void MemberDouble4x4(this ref Utf8JsonWriter s, in Str32 key, in double4x4 value) {
            s.MemberArrayStart(key, true);
            ArrayDouble4(ref s, in value.c0);
            ArrayDouble4(ref s, in value.c1);
            ArrayDouble4(ref s, in value.c2);
            ArrayDouble4(ref s, in value.c3);
            s.ArrayEnd();
        }
    }
}
