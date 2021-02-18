﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using Unity.Mathematics;

#if JSON_BURST
    using Str32 = Unity.Collections.FixedString32;
#else
    using Str32 = System.String;
#endif

// ReSharper disable InconsistentNaming
namespace Friflo.Json.Burst.Math
{
    public static partial class JsonMath
    {
        public static bool UseMemberFloat2(this ref JsonParser p, ref JObj obj, in Str32 key, ref float2 value) {
            if (obj.UseMemberArr(ref p, in key, out JArr arr)) {
                ArrayFloat2(ref p, ref arr, ref value);
                return true;
            }
            return false;
        }
        
        public static bool UseMemberFloat3(this ref JsonParser p, ref JObj obj, in Str32 key, ref float3 value) {
            if (obj.UseMemberArr(ref p, in key, out JArr arr)) {
                ArrayFloat3(ref p, ref arr, ref value);
                return true;
            }
            return false;
        }
        
        public static bool UseMemberFloat4(this ref JsonParser p, ref JObj obj, in Str32 key, ref float4 value) {
            if (obj.UseMemberArr(ref p, in key, out JArr arr)) {
                ArrayFloat4(ref p, ref arr, ref value);
                return true;
            }
            return false;
        }

        
        private static void ArrayFloat2(ref JsonParser p, ref JArr i, ref float2 value) {
            int index = 0;
            while (i.NextArrayElement(ref p)) {
                if (i.UseElementNum(ref p)) {
                    if (index < 2)
                        value[index++] = p.ValueAsFloat(out bool _);
                } else 
                    p.ErrorMsg("Json.Burst.Math", "expect JSON number");
            }
        }
        
        private static void ArrayFloat3(ref JsonParser p, ref JArr i, ref float3 value) {
            int index = 0;
            while (i.NextArrayElement(ref p)) {
                if (i.UseElementNum(ref p)) {
                    if (index < 3)
                        value[index++] = p.ValueAsFloat(out bool _);
                } else 
                    p.ErrorMsg("Json.Burst.Math", "expect JSON number");
            }
        }
        
        private static void ArrayFloat4(ref JsonParser p, ref JArr i, ref float4 value) {
            int index = 0;
            while (i.NextArrayElement(ref p)) {
                if (i.UseElementNum(ref p)) {
                    if (index < 4)
                        value[index++] = p.ValueAsFloat(out bool _);
                } else 
                    p.ErrorMsg("Json.Burst.Math", "expect JSON number");
            }
        }
        
        // ---
        private static void ArrayFloat4x4(ref JsonParser p, ref JArr i, ref float4x4 value) {
            int index = 0;
            while (i.NextArrayElement(ref p)) {
                if (i.UseElementArr(ref p)) {
                    var arr = p.GetArrayIterator(); // todo remove
                    if (index < 4)
                        ArrayFloat4(ref p, ref arr, ref value[index++]);
                } else 
                    p.ErrorMsg("Json.Burst.Math", "expect JSON number");
            }
        }
        
        public static bool UseMemberFloat4x4(this ref JsonParser p, ref JObj obj, in Str32 key, ref float4x4 value) {
            if (obj.UseMemberArr(ref p, in key, out JArr arr)) {
                ArrayFloat4x4(ref p, ref arr, ref value);
                return true;
            }
            return false;
        }
    }
}