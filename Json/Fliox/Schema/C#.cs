﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text;
using Friflo.Json.Fliox.Schema.Definition;
using Friflo.Json.Fliox.Schema.Utils;
using static Friflo.Json.Fliox.Schema.Generator;
// Must not have other dependencies to Friflo.Json.Fliox.* except .Schema.Definition & .Schema.Utils

namespace Friflo.Json.Fliox.Schema
{
    public sealed partial class CSharpGenerator
    {
        private  readonly   Generator                   generator;
        private  readonly   Dictionary<TypeDef, string> standardTypes;
        private  readonly   Dictionary<TypeDef, string> customTypes;

        private CSharpGenerator (Generator generator) {
            this.generator  = generator;
            standardTypes   = GetStandardTypes(generator.standardTypes);
            customTypes     = GetCustomTypes  (generator.standardTypes);
        }
        
        public static void Generate(Generator generator) {
            var emitter = new CSharpGenerator(generator);
            var sb      = new StringBuilder();
            foreach (var type in generator.types) {
                sb.Clear();
                var result = emitter.EmitType(type, sb);
                if (result == null)
                    continue;
                generator.AddEmitType(result);
            }
            generator.GroupTypesByPath(true); // sort dependencies - otherwise possible error TS2449: Class '...' used before its declaration.
            emitter.EmitFileHeaders(sb);
            emitter.EmitFileFooters(sb);
            generator.EmitFiles(sb, ns => $"{ns}{generator.fileExt}");
        }
        
        private static Dictionary<TypeDef, string> GetStandardTypes(StandardTypes standard) {
            var map = new Dictionary<TypeDef, string>();
            AddType (map, standard.Boolean,     "bool" );
            AddType (map, standard.String,      "string" );

            AddType (map, standard.Uint8,       "byte" );
            AddType (map, standard.Int16,       "short" );
            AddType (map, standard.Int32,       "int" );
            AddType (map, standard.Int64,       "long" );
               
            AddType (map, standard.Double,      "double" );
            AddType (map, standard.Float,       "float" );
            AddType (map, standard.JsonKey,     "string" );
            return map;
        }
        
        private static Dictionary<TypeDef, string> GetCustomTypes(StandardTypes standard) {
            var map = new Dictionary<TypeDef, string>();
            AddType (map, standard.BigInteger,  "System.Numerics" );
            AddType (map, standard.DateTime,    "System" );
            AddType (map, standard.Guid,        "System" );
            AddType (map, standard.JsonValue,   "Friflo.Json.Fliox.Mapper" );
            return map;
        }
        
        private EmitType EmitType(TypeDef type, StringBuilder sb) {
            if (type.IsClass) {
                return EmitClassType(type, sb);
            }
            if (type.IsEnum) {
                var enumValues = type.EnumValues;
                sb.AppendLine($"public enum {type.Name} {{");
                foreach (var enumValue in enumValues) {
                    sb.AppendLine($"    {enumValue.name},");
                }
                sb.AppendLine("}");
                sb.AppendLine();
                return new EmitType(type, sb);
            }
            return null;
        }
        
        private EmitType EmitClassType(TypeDef type, StringBuilder sb) {
            var imports     = new HashSet<TypeDef>();
            var context     = new TypeContext (generator, imports, type);
            var fields      = type.Fields;
            var extendsStr  = "";
            var baseType    = type.BaseType;
            if (baseType != null) {
                extendsStr = $": {baseType.Name} ";
                imports.Add(baseType);
            }
            var unionType = type.UnionType;
            if (unionType == null) {
                var classType = type.IsStruct ? "struct" : "class";
                var abstractStr = type.IsAbstract ? "abstract " : "";
                sb.AppendLine($"public {abstractStr}{classType} {type.Name} {extendsStr}{{");
            } else {
                sb.AppendLine($"[Fri.Discriminator(\"{unionType.discriminator}\")]");
                int max    = unionType.types.MaxLength(polyType => polyType.typeDef.Name.Length);
                foreach (var polyType in unionType.types) {
                    var polyTypeDef = polyType.typeDef;
                    var indent   = Indent(max, polyTypeDef.Name);
                    sb.AppendLine($"[Fri.Polymorph(typeof({polyTypeDef.Name}),{indent} Discriminant = \"{polyType.discriminant}\")]");
                    imports.Add(polyTypeDef);
                }
                sb.AppendLine($"public abstract class {type.Name} {extendsStr}{{");
            }
            var emitFields = new List<EmitField>(fields.Count);
            foreach (var field in fields) {
                if (field.IsDerivedField)
                    continue;
                var fieldType = GetFieldType(field, context);
                var emitField = new EmitField(fieldType, field);
                emitFields.Add(emitField);
            }
            int maxFieldName    = emitFields.MaxLength(field => field.type.Length);
            foreach (var field in emitFields) {
                var indent   = Indent(maxFieldName, field.type);
                var def      = field.def;
                var isReferenceType = def.isArray || def.isDictionary || !def.type.IsStruct;
                bool notNull        = def.required || isReferenceType;
                var nullStr         = notNull ? " " : "?";
                if (def.isKey)
                    sb.AppendLine("    [Fri.Key]");
                if (def.required && isReferenceType)
                    sb.AppendLine("    [Fri.Required]");
                sb.AppendLine($"    {field.type}{nullStr}{indent} {def.name};");
            }
            sb.AppendLine("}");
            sb.AppendLine();
            return new EmitType(type, sb, imports);
        }
        
        private string GetFieldType(FieldDef field, TypeContext context) {
            if (field.isArray) {
                var elementTypeName = GetElementType(field, context);
                return $"List<{elementTypeName}>";
            }
            if (field.isDictionary) {
                var valueTypeName = GetElementType(field, context);
                return $"Dictionary<string, {valueTypeName}>";
            }
            return GetTypeName(field.type, context);
        }
        
        private string GetElementType(FieldDef field, TypeContext context) {
            var elementTypeName = GetTypeName(field.type, context);
            if (field.isNullableElement)
                return $"{elementTypeName}?";
            return elementTypeName;
        }
        
        private string GetTypeName(TypeDef type, TypeContext context) {
            if (standardTypes.TryGetValue(type, out string name))
                return name;
            context.imports.Add(type);
            return type.Name;
        }
        
        private void EmitFileHeaders(StringBuilder sb) {
            foreach (var pair in generator.fileEmits) {
                EmitFile    emitFile    = pair.Value;
                string      filePath    = pair.Key;
                sb.Clear();
                sb.AppendLine($"// {Note}");
                sb.AppendLine("using System.Collections.Generic;");
                var namespaces = new HashSet<string> {"Friflo.Json.Fliox.Mapper"};
                foreach (var importPair in emitFile.imports) {
                    var import = importPair.Value;
                    if (import.type.Path == filePath)
                        continue;
                    if (customTypes.TryGetValue(import.type, out var @namespace)) {
                        namespaces.Add(@namespace);
                        continue;
                    }
                    namespaces.Add(import.@namespace);
                }
                foreach (var ns in namespaces) {
                    sb.AppendLine($"using {ns};");
                }
                sb.AppendLine();
                sb.AppendLine("#pragma warning disable 0169 // [CS0169] The field '...' is never used");
                sb.AppendLine();
                sb.AppendLine($"namespace {emitFile.@namespace} {{");

                emitFile.header = sb.ToString();
            }
        }
        
        private void EmitFileFooters(StringBuilder sb) {
            foreach (var pair in generator.fileEmits) {
                EmitFile    emitFile    = pair.Value;
                sb.Clear();
                sb.AppendLine("}");
                emitFile.footer = sb.ToString();
            }
        }
    }
    
    internal readonly struct EmitField {
        internal readonly   string      type;
        internal readonly   FieldDef    def;
        
        public override string ToString() => def.name;
        
        internal EmitField (string type, FieldDef def) {
            this.type   = type;
            this.def    = def;
        }
    }
}