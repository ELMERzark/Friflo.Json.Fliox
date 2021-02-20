﻿using System;
using System.IO;
using System.Text;

namespace Friflo.Json.Burst.Math.CodeGen
{
    class Program
    {
        private static void Main(string[] args) {
            GenerateType("bool",    "Bln");
            GenerateType("float",   "Num");
            GenerateType("double",  "Num");
            GenerateType("int",     "Lng");
            // GenerateType("uint",    "Lng");
        }

        private static void GenerateType(string type, string suffix) {
            var sb = new StringBuilder();
            
            RenderType(sb, type, suffix);
            
            string baseDir = Directory.GetCurrentDirectory() + "/../../../../Burst.Math/";
            baseDir = Path.GetFullPath(baseDir);
            string path = baseDir + type + ".gen.cs";
            using (StreamWriter fileStream = new StreamWriter(path)) {
                fileStream.Write(sb);
            }
        }
        
        private static void RenderType(StringBuilder sb, string name, string suffix)
        {
            var header = $@"// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using Unity.Mathematics;

#if JSON_BURST
    using Str32 = Unity.Collections.FixedString32;
#else
    using Str32 = System.String;
#endif

// ReSharper disable InconsistentNaming
namespace Friflo.Json.Burst.Math
{{
    public static partial class Json
    {{";
            sb.Append(header);

            RenderTypeDim1(sb, name, 2, "Num");
            RenderTypeDim1(sb, name, 3, "Num");
            RenderTypeDim1(sb, name, 4, "Num");
            
            // 2x2, 2x3, 2x4, 3x2, 3x3, 3x4, 4x2, 4x3, 4x4
            RenderTypeDim2(sb, name, 2, 2);
            RenderTypeDim2(sb, name, 2, 3);
            RenderTypeDim2(sb, name, 2, 4);
            
            RenderTypeDim2(sb, name, 3, 2);
            RenderTypeDim2(sb, name, 3, 3);
            RenderTypeDim2(sb, name, 3, 4);

            RenderTypeDim2(sb, name, 4, 2);
            RenderTypeDim2(sb, name, 4, 3);
            RenderTypeDim2(sb, name, 4, 4);

            var footer = $@"    }}
}}
";
            sb.Append(footer);
        }

        private static string GetPascalCase(string type) {
            return type[0].ToString().ToUpper() + type.Substring(1);
        }

        private static void RenderTypeDim1(StringBuilder sb, string type, int size, string suffix)
        {
            var pascal = GetPascalCase(type);
            var str = $@"
        public static bool UseMember{pascal}X{size}(this ref JObj i, ref JsonParser p, in Str32 key, ref {type}{size} value) {{
            if (i.UseMemberArr(ref p, in key, out JArr arr)) {{
                Read{pascal}{size}(ref arr, ref p, ref value);
                return true;
            }}
            return false;
        }}

        private static void Read{pascal}{size}(ref JArr i, ref JsonParser p, ref {type}{size} value) {{
            int index = 0;
            while (i.NextArrayElement(ref p)) {{
                if (i.UseElement{suffix}(ref p)) {{
                    if (index < {size})
                        value[index++] = p.ValueAs{pascal}(out bool _);
                }} else 
                    p.ErrorMsg(""Json.Burst.Math"", ""expect JSON number"");
            }}
        }}
";
            sb.Append(str);
        }
        
        private static void RenderTypeDim2(StringBuilder sb, string type, int size1, int size2)
        {
            var pascal = GetPascalCase(type);
            var dim = $"{size1}x{size2}";
            var str = $@"
        private static void Read{pascal}{dim}(ref JArr i, ref JsonParser p, ref {type}{dim} value) {{
            int index = 0;
            while (i.NextArrayElement(ref p)) {{
                if (i.UseElementArr(ref p, out JArr arr)) {{
                    if (index < {size2})
                        Read{pascal}{size1}(ref arr, ref p, ref value[index++]);
                }} else 
                    p.ErrorMsg(""Json.Burst.Math"", ""expect JSON number"");
            }}
        }}
        
        public static bool UseMember{pascal}X{dim}(this ref JObj obj, ref JsonParser p, in Str32 key, ref {type}{dim} value) {{
            if (obj.UseMemberArr(ref p, in key, out JArr arr)) {{
                Read{pascal}{dim}(ref arr, ref p, ref value);
                return true;
            }}
            return false;
        }}
";
            sb.Append(str);
        }
    }
}