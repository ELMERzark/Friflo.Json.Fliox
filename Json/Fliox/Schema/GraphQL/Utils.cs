// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.


using System.Collections.Generic;
using System.Text;

namespace Friflo.Json.Fliox.Schema.GraphQL
{
    public static class Gql
    {
        public static GqlType List (GqlType itemType, bool required, bool itemsRequired) {
            if (itemsRequired) {
                itemType = new GqlNonNull { ofType = itemType };
            }
            GqlType listType = new GqlList { ofType = itemType };
            if (required) {
                return new GqlNonNull { ofType = listType };
            }
            return listType;
        }
        
        public static GqlObject Object (string name, List<GqlField> fields) {
            return new GqlObject { name = name, fields = fields };
        }
        
        public static GqlField Field (string name, GqlType type) {
            return new GqlField { name = name, type = type };
        }
        
        public static GqlScalar Scalar (string name) {
            return new GqlScalar { name = name };
        }
        
        public static GqlScalar ScalarInput (string name) {
            return new GqlScalar { name = name + "Input",  };
        }
        
        public static GqlType Type (GqlType type, bool required) {
            if (required)
                return new GqlNonNull { ofType = type };
            return type;
        }
        
        public static GqlInputValue InputValue (string name, GqlType type, bool required = false) {
            if (required) {
                type = new GqlNonNull { ofType = type };
            }
            return new GqlInputValue { name = name, type = type };
        }
        
        public static GqlType String    () =>   Scalar("String");
        public static GqlType Int       () =>   Scalar("Int");
        public static GqlType Float     () =>   Scalar("Float");
        public static GqlType Boolean   () =>   Scalar("Boolean");
        public static GqlType Any       () =>   Scalar("Any");
        
        public static string MethodName (string methodType, string container) {
            var sb = new StringBuilder();
            sb.Append(methodType);
            AppendPascalCase(sb, container);
            return sb.ToString();
        }
        
        public static string MethodResult (string methodType, string container) {
            var sb = new StringBuilder();
            AppendPascalCase(sb, methodType);
            AppendPascalCase(sb, container);
            sb.Append("Result");
            return sb.ToString();
        }
        
        private static void AppendPascalCase (StringBuilder sb, string name) {
            sb.Append(char.ToUpper(name[0]));
            sb.Append(name, 1, name.Length - 1);
        }
    }
}