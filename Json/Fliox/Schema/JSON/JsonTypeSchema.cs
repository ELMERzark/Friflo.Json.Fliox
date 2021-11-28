﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Schema.Definition;

// ReSharper disable JoinNullCheckWithUsage
namespace Friflo.Json.Fliox.Schema.JSON
{
    /// <summary>
    /// A <see cref="TypeSchema"/> constructed by a set of given <see cref="JsonSchema"/>'s.
    /// The utility method <see cref="JsonTypeSchema.ReadSchemas"/> can be used to read a set of
    /// <see cref="JsonSchema"/>'s as files in a folder.
    /// </summary>
    public sealed class JsonTypeSchema : TypeSchema, IDisposable
    {
        public  override    ICollection<TypeDef>            Types           { get; }
        public  override    StandardTypes                   StandardTypes   { get; }
        public  override    TypeDef                         RootType       { get; }
        
        private readonly    Dictionary<string, JsonTypeDef> typeMap;
        
        public JsonTypeSchema(List<JsonSchema> schemaList, string rootType = null) {
            typeMap = new Dictionary<string, JsonTypeDef>(schemaList.Count);
            foreach (JsonSchema schema in schemaList) {
                schema.typeDefs = new Dictionary<string, JsonTypeDef>(schema.definitions.Count);
                foreach (var pair in schema.definitions) {
                    var typeName    = pair.Key;
                    var type        = pair.Value;
                    var @namespace  = GetNamespace(schema, typeName);
                    var typeDef     = new JsonTypeDef (type, typeName, @namespace);
                    var schemaId = $"./{schema.fileName}#/definitions/{typeName}";
                    typeMap.Add(schemaId, typeDef);
                    var localId = $"#/definitions/{typeName}";
                    schema.typeDefs.Add(localId, typeDef);
                }
            }
            
            Types               = new List<TypeDef>(typeMap.Values);
            var standardTypes   = new JsonStandardTypes(typeMap);
            StandardTypes       = standardTypes;
            
            using (var typeStore    = new TypeStore())
            using (var reader       = new ObjectReader(typeStore))
            {
                foreach (JsonSchema schema in schemaList) {
                    var context = new JsonTypeContext(schema, typeMap, standardTypes, reader);
                    var rootRef = schema.rootRef;
                    if (rootRef != null) {
                        FindRef(schema.rootRef, context);
                    }
                    foreach (var pair in schema.typeDefs) {
                        JsonTypeDef typeDef = pair.Value;
                        JsonType    type    = typeDef.type;
                        var         extends = type.extends;
                        type.name           = pair.Key;
                        if (extends != null) {
                            typeDef.baseType = FindRef(extends.reference, context);
                        }
                        var typeType    = type.type;
                        var oneOf       = type.oneOf;
                        if (oneOf != null || typeType == "object") {
                            typeDef.isAbstract  = type.isAbstract.HasValue && type.isAbstract.Value;
                            typeDef.isStruct    = type.isStruct.HasValue && type.isStruct.Value;
                            var properties      = type.properties;
                            if (properties != null) {
                                typeDef.fields = new List<FieldDef>(properties.Count);
                                foreach (var propPair in properties) {
                                    string      fieldName   = propPair.Key;
                                    FieldType   field       = propPair.Value;
                                    // discriminator field is not a real member -> skip it
                                    if (type.discriminator == fieldName)
                                        continue;
                                    SetField(typeDef, fieldName, field, context);
                                }
                            }
                            var commands      = type.commands;
                            if (commands != null) {
                                typeDef.messages = new List<MessageDef>(commands.Count);
                                foreach (var msgPair in commands) {
                                    string      messageName = msgPair.Key;
                                    MessageType message     = msgPair.Value;
                                    SetMessage(typeDef, messageName, message, context);
                                }
                            }
                        }
                        if (oneOf != null) {
                            var unionTypes = new List<UnionItem>(oneOf.Count);
                            foreach (var item in oneOf) {
                                var itemRef             = FindRef(item.reference, context);
                                var discriminantMember  = itemRef.type.properties[type.discriminator];
                                var discriminant        = discriminantMember.discriminant[0];
                                var unionItem           = new UnionItem(itemRef, discriminant);
                                unionTypes.Add(unionItem);
                            }
                            typeDef.isAbstract = true;
                            typeDef.unionType  = new UnionType (type.discriminator, unionTypes);
                        }
                    }
                }
                foreach (JsonSchema schema in schemaList) {
                    foreach (var pair in schema.typeDefs) {
                        JsonTypeDef typeDef = pair.Value;
                        if (typeDef.discriminant == null)
                            continue;
                        var baseType = typeDef.baseType;
                        while (baseType != null) {
                            var unionType = baseType.unionType;
                            if (unionType != null) {
                                typeDef.discriminator = unionType.discriminator;
                                break;
                            }
                            baseType = baseType.baseType;
                        }
                        if (typeDef.discriminator == null)
                            throw new InvalidOperationException($"found no discriminator in base classes. type: {typeDef}");
                    }
                }
            }
            MarkDerivedFields();
            if (rootType != null) {
                var rootTypeDef = TypeAsTypeDef(rootType);
                if (rootTypeDef == null)
                    throw new InvalidOperationException($"rootType not found: {rootType}");
                if (!rootTypeDef.IsClass)
                    throw new InvalidOperationException($"rootType must be a class: {rootType}");
                RootType = rootTypeDef;
            }
        }
        
        public void Dispose() { }

        private static void SetField (JsonTypeDef typeDef, string fieldName, FieldType field, in JsonTypeContext context) {
            field.name              = fieldName;
            TypeDef fieldType; // not initialized by intention
            bool    isArray         = false;
            bool    isDictionary    = false;
            bool    required        = typeDef.type.required?.Contains(fieldName) ?? false;

            FieldType   items       = GetItemsFieldType(field.items, out bool isNullableElement);
            JsonValue   jsonType    = field.type;
            FieldType   addProps    = field.additionalProperties;

            if (field.reference != null) {
                fieldType = FindRef(field.reference, context);
            }
            else if (items?.reference != null) {
                isArray = true;
                fieldType = FindFieldType(field, items, context);
            }
            else if (field.oneOf != null) {
                TypeDef oneOfType = null; 
                foreach (var item in field.oneOf) {
                    var itemType = FindFieldType(field, item, context);
                    if (itemType == null)
                        continue;
                    oneOfType = itemType;
                }
                if (oneOfType == null)
                    throw new InvalidOperationException($"'oneOf' array without a type: {field.oneOf}");
                fieldType = oneOfType;
            }
            else if (addProps != null) {
                isDictionary = true;
                if (addProps.reference != null) {
                    fieldType = FindRef(addProps.reference, context);
                } else {
                    fieldType = FindTypeFromJson(field, jsonType, items, context, ref isArray);
                }
            }
            else if (!jsonType.IsNull()) {
                fieldType = FindTypeFromJson (field, jsonType, items, context, ref isArray);
            }
            else if (field.discriminant != null) {
                typeDef.discriminant = field.discriminant[0]; // a discriminant has no FieldDef
                return;
            }
            else {
                fieldType = context.standardTypes.JsonValue;
                // throw new InvalidOperationException($"cannot determine field type. type: {type}, field: {field}");
            }
            var isKey           = field.isKey.HasValue && field.isKey.Value;
            var isAutoIncrement = field.isAutoIncrement.HasValue && field.isAutoIncrement.Value;
            var fieldDef = new FieldDef (fieldName, required, isKey, isAutoIncrement, fieldType, isArray, isDictionary, isNullableElement, typeDef);
            typeDef.fields.Add(fieldDef);
        }
        
        private static void SetMessage (JsonTypeDef typeDef, string messageName, MessageType field, in JsonTypeContext context) {
            field.name      = messageName;
            var valueType   = FindFieldType(null, field.command[0], context);
            var resultType  = FindFieldType(null, field.command[1], context);
            var messageDef  = new MessageDef(messageName, valueType, resultType);
            typeDef.messages.Add(messageDef);
        }
        
        private static TypeDef FindTypeFromJson (FieldType field, JsonValue jsonArray, FieldType items, in JsonTypeContext context, ref bool isArray) {
            var json = jsonArray.AsString();
            if     (json.StartsWith("\"")) {
                var jsonValue = json.Substring(1, json.Length - 2);
                if (jsonValue == "array") {
                    isArray = true;
                    return FindFieldType (field, items, context);
                }
                if (jsonValue == "null")
                    return null;
                return FindType(jsonValue, context);
            }
            if (json.StartsWith("[")) {
                // handle nullable field types
                TypeDef elementType = null;
                var fieldTypes = context.reader.Read<List<string>>(json);
                foreach (var itemType in fieldTypes) {
                    if (itemType == "null")
                        continue;
                    if (itemType == "array") {
                        isArray = true;
                        return FindFieldType (field, items, context);
                    }
                    var elementTypeDef = FindType(itemType, context);
                    if (elementTypeDef != null) {
                        elementType = elementTypeDef;
                    }
                }
                if (elementType == null)
                    throw new InvalidOperationException("additionalProperties requires '$ref'");
                return elementType;
            }
            throw new InvalidOperationException($"Unexpected type: {json}");
        }
        
        private static TypeDef FindType (string type, in JsonTypeContext context) {
            var standardType = StandardType(type, context.standardTypes);
            return standardType;
        }
        
        private static TypeDef StandardType (string type, JsonStandardTypes types) {
            switch (type) {
                case "boolean": return types.Boolean;
                case "string":  return types.String;
                case "integer": return types.Int32;
                case "number":  return types.Double;
                case "object":  return types.JsonValue;
                // case "null":    return null;
                // case "array":   return null;
            }
            throw new InvalidOperationException($"unexpected standard type: {type}");
        }
        
        // return null if optional
        private static TypeDef FindFieldType (FieldType field, FieldType itemType, in JsonTypeContext context) {
            var reference = itemType.reference;
            if (reference != null) {
                if (reference.StartsWith("#/definitions/")) {
                    return context.schema.typeDefs[reference];
                }
                return context.schemas[reference];
            }
            var jsonType =  itemType.type;
            if (!jsonType.IsNull()) {
                bool isArray = true;
                var itemTypeItems = GetItemsFieldType(itemType.items, out _);
                return FindTypeFromJson(field, jsonType, itemTypeItems, context, ref isArray);
            }
            return context.standardTypes.JsonValue;
        }
        
        private static readonly JsonValue Null = new JsonValue("\"null\"");
        
        /// Supporting nullable (value type) array elements seems uh - however it is supported. Reasons against:
        /// <list type="bullet">
        ///   <item>Application now have to check for null when accessing these types of arrays -> uh</item>
        ///   <item>Generated languages have typically no support for custom nullable values types.
        ///         Common element types like int, byte, ... are typically supported - custom types not.</item>
        /// </list>
        // ReSharper disable once UnusedMember.Local
        private static FieldType GetItemsFieldType (FieldType itemType, out bool isNullableElement) {
            if (itemType == null) {
                isNullableElement = false;
                return null;
            }
            if (!itemType.type.IsNull()) {
                isNullableElement = false;
                return itemType;
            }
            if (itemType.reference != null) {
                isNullableElement = false;
                return itemType;
            }
            var oneOf = itemType.oneOf;
            if (oneOf != null) {
                isNullableElement = false;
                FieldType elementType = null;
                foreach (var fieldType in oneOf) {
                    if (fieldType.type.IsEqual(Null)) {
                        isNullableElement = true;
                    }
                    if (fieldType.reference != null) {
                        if (elementType != null)
                            throw new InvalidOperationException($"Found multiple '$ref' in 'oneOf': {fieldType.reference}");        
                        elementType = fieldType;
                    }
                }
                if (elementType == null)
                    throw new InvalidOperationException("Missing '$ref' in 'oneOf'");
                return elementType;
            }
            throw new InvalidOperationException("Expected 'type', '$ref' or 'oneOf'");
        }

        private static JsonTypeDef FindRef (string reference, in JsonTypeContext context) {
            if (reference.StartsWith("#/definitions/")) {
                return context.schema.typeDefs[reference];
            }
            return context.schemas[reference];
        }
        
        private static string GetNamespace (JsonSchema schema, string typeName) {
            var name = schema.name;
            var rootRef = schema.rootRef;
            if (rootRef != null) {
                if (!rootRef.StartsWith("#/definitions/"))
                    throw new InvalidOperationException($"Expect root '$ref' starts with: #/definitions/. was: {rootRef}");
                var rootTypeName = rootRef.Substring("#/definitions/".Length);
                if (rootTypeName == typeName) {
                    name = name.Substring(0, name.Length - typeName.Length - 1); // -1 => '.'
                }
            }
            return name;
        }

        /// <summary>Read a set of <see cref="JsonSchema"/>'s stored as files in the given <see cref="folder"/>.</summary>
        public static List<JsonSchema> ReadSchemas(string folder) {
            string[] fileNames = Directory.GetFiles(folder, "*.json", SearchOption.TopDirectoryOnly);
            var schemas = new List<JsonSchema>();
            using (var typeStore    = new TypeStore())
            using (var reader       = new ObjectReader(typeStore)) {
                foreach (var path in fileNames) {
                    var fileName = path.Substring(folder.Length + 1);
                    var name = fileName.Substring(0, fileName.Length - ".json".Length);
                    var jsonSchema = File.ReadAllText(path, Encoding.UTF8);
                    var schema = reader.Read<JsonSchema>(jsonSchema);
                    schema.fileName = fileName;
                    schema.name = name;
                    schemas.Add(schema);
                }
                return schemas;
            }
        }
        
        public ICollection<TypeDef> TypesAsTypeDefs(ICollection<string> types) {
            if (types == null)
                return null;
            var list = new List<TypeDef> (types.Count);
            foreach (var type in types) {
                var typeDef = typeMap[type];
                list.Add(typeDef);
            }
            return list;
        }
        
        public TypeDef TypeAsTypeDef(string type) {
            return typeMap[type];
        }
    }
    
    internal readonly struct JsonTypeContext
    {
        internal readonly   JsonSchema                      schema;
        internal readonly   Dictionary<string, JsonTypeDef> schemas;
        internal readonly   JsonStandardTypes               standardTypes;
        internal readonly   ObjectReader                    reader;

        internal JsonTypeContext (
            JsonSchema                      schema,
            Dictionary<string, JsonTypeDef> schemas,
            JsonStandardTypes               standardTypes,
            ObjectReader                    reader)
        {
            this.schema         = schema;
            this.schemas        = schemas;
            this.standardTypes  = standardTypes;
            this.reader         = reader;
        }
    }
}