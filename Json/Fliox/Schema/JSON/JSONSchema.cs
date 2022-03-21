﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Mapper;

using Req       = Friflo.Json.Fliox.Mapper.Fri.RequiredAttribute;
using Ignore    = Friflo.Json.Fliox.Mapper.Fri.IgnoreAttribute;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnassignedField.Global
// ReSharper disable CollectionNeverUpdated.Global
namespace Friflo.Json.Fliox.Schema.JSON
{
    /// <summary>
    /// Compatible subset of JSON Schema with some extensions required for code generation.<br/>
    /// JSON Schema specification: https://json-schema.org/specification.html<br/>
    /// <br/>
    /// Following extensions are added to JSON Schema:
    /// <list type="bullet">
    ///     <item><see cref="JsonType.extends"/> - used to declare that a typ definition extends the given one</item>
    ///     <item><see cref="JsonType.discriminator"/> - declare the property name used as discriminator</item>
    ///     <item><see cref="JsonType.isStruct"/> - type should be generated as struct</item>
    ///     <item><see cref="JsonType.isAbstract"/> - type definition is an abstract type</item>
    ///     <item><see cref="JsonType.messages"/> - list of all schema messages</item>
    ///     <item><see cref="JsonType.commands"/> - list of all schema commands</item>
    ///     <item><see cref="JsonType.key"/> - the property used as primary key</item>
    ///     <item><see cref="JsonType.descriptions"/> - a map storing the descriptions for enum values</item>
    ///     <item><see cref="FieldType.relation"/> - mark the property as a relation (aka reference or aka secondary key) to entities in the given container</item>
    /// </list>
    /// The restriction of <see cref="JSONSchema"/> are:
    /// <list type="bullet">
    ///   <item>
    ///     A schema property cannot nest anonymous types by "type": "object" with "properties": { ... }. <br/>
    ///     The property type needs to be a known type like "string", ... or a referenced ("$ref") type.  <br/>
    ///     This restriction enables generation of code and types for languages without support of anonymous types. <br/>
    ///     It also enables concise error messages for validation errors when using <see cref="Validation.TypeValidator"/>.
    ///   </item>
    ///   <item>
    ///     Note: Arrays and dictionaries are also valid schema properties. E.g. <br></br>
    ///     A valid array property like: <code>{ "type": ["array", "null"], "items": { "type": "string" } }</code><br></br>
    ///     A valid dictionary property like:  <code>{ "type": "object", "additionalProperties": { "type": "string" } }</code><br></br>
    ///     These element / value types needs to be a known type like "string", ... or a referenced ("$ref") type.
    ///   </item>
    ///   <item>
    ///     On root level are only "$ref": "..." and "definitions": [...] allowed.
    ///   </item>
    /// </list>
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public sealed class JSONSchema
    {
        /// <summary>reference to 'main' type definition in <see cref="definitions"/> to<br/>
        /// enable schema urls without fragment suffix like: <c>#/definitions/SomeType</c> </summary>
        [Fri.Property(Name =                               "$ref")]
                public      string                          rootRef;

        /// <summary>map of type <see cref="definitions"/> contained by the JSON Schema.</summary>
                public      Dictionary<string, JsonType>    definitions;

        /// <summary>file name is <see cref="name"/> + ".json".
        /// E.g. <see cref="name"/>: Standard.json, <see cref="name"/>: "Standard</summary>
        [Ignore]public      string                          fileName;
        [Ignore]public      string                          name;
        [Ignore]internal    Dictionary<string, JsonTypeDef> typeDefs;

        public override string                          ToString() => fileName;
    }

    /// <summary>
    /// Use by <see cref="JSONSchema.definitions"/> in <see cref="JSONSchema"/> to declare a type definition
    /// </summary>
    public sealed class JsonType
    {
        /// <summary>reference to type definition which <see cref="extends"/> this type - <i>JSON Schema extension</i></summary>
                public  TypeRef                         extends;
        /// <summary><see cref="discriminator"/> declares the name of the property used for polymorphic types - <i>JSON Schema extension</i></summary>
                public  string                          discriminator;
        /// <summary>list of all specific types a polymorphic type can be. Is required if <see cref="discriminator"/> is assigned</summary>
                public  List<FieldType>                 oneOf;
        /// <summary>declare type as an abstract type - <i>JSON Schema extension</i></summary>
                public  bool?                           isAbstract;
        //
        /// <summary>'null', 'object', 'string', 'boolean', 'number', 'integer' or 'array'</summary>
                public  string                          type; // null or SchemaType
             // public  SchemaType?                     type; // todo use this
        /// <summary>name of the property used as primary <see cref="key"/> for entities - <i>JSON Schema extension</i></summary>
                public  string                          key;  // if null a property named "id" must exist
        /// <summary>map of all <see cref="properties"/> declared by the type definition. The map keys are the property names</summary>
                public  Dictionary<string, FieldType>   properties;
        /// <summary>database <see cref="commands"/>. The map keys are the command names - <i>JSON Schema extension</i></summary>
                public  Dictionary<string, MessageType> commands;
        /// <summary>database <see cref="messages"/>. The map keys are the message names - <i>JSON Schema extension</i></summary>
                public  Dictionary<string, MessageType> messages;
        /// <summary>true if type should be generated as a value type (struct) - <i>JSON Schema extension</i></summary>
                public  bool?                           isStruct;
        /// <summary>list of <see cref="required"/> properties</summary>
                public  List<string>                    required;
        /// <summary>true if <see cref="additionalProperties"/> are allowed</summary>
                public  bool                            additionalProperties;
        /// <summary>all values that can be used for an enumeration type</summary>
        [Fri.Property(Name =                           "enum")]
                public  List<string>                    enums;
        /// <summary>map of optional <see cref="descriptions"/> for <b>enum</b> values - <i>JSON Schema extension</i></summary>
                public  Dictionary<string,string>       descriptions;

        [Ignore]public  string                          name;
        /// <summary>optional type description</summary>
                public  string                          description;

        public override string                          ToString() => name;
    }

    /// <summary>
    /// A reference to a type definition in a JSON Schema
    /// </summary>
    public sealed class TypeRef {
        /// <summary>reference to a type definition</summary>
        [Fri.Property (Name =          "$ref")]
        [Req]   public  string          reference;
        //      public  string          type;   // not used - was used for nullable array elements

        public override string          ToString() => reference;
    }

    /// <summary>
    /// Defines the type of property
    /// </summary>
    public sealed class FieldType
    {
        /// <summary>'null', 'object', 'string', 'boolean', 'number', 'integer' or 'array'<br/>
        /// or array of these values used to declare <b>nullable</b> properties when using a basic JSON Schema type</summary>
                public  JsonValue       type;           // SchemaType or SchemaType[]
        /// <summary>discriminant of a specific polymorphic type. Always an array with one string element</summary>
        [Fri.Property(Name =           "enum")]
                public  List<string>    discriminant;   // contains exactly one element for a specific type or a list is inside an abstract type
        /// <summary>if set the property is an array - it declares the type of its <see cref="items"/></summary>
                public  FieldType       items;
        /// <summary>list of valid property types - used to declare <b>nullable</b> properties when using a <b>$ref</b> type</summary>
                public  List<FieldType> oneOf;
        /// <summary><see cref="minimum"/> valid number</summary>
                public  long?           minimum;
        /// <summary><see cref="maximum"/> valid number</summary>
                public  long?           maximum;
        /// <summary>regular expression <see cref="pattern"/> to constrain string values</summary>
                public  string          pattern;
        /// <summary>set to <b>'date-time'</b> if the property is a timestamp formatted as RFC 3339 + milliseconds</summary>
                public  string          format;  // "date-time"
        /// <summary>reference to type definition used as property type</summary>
        [Fri.Property(Name =           "$ref")]
                public  string          reference;
        /// <summary>if set the property is a map (Dictionary) using the key type <b>string</b> and the value type
        /// specified by <see cref="additionalProperties"/></summary>
                public  FieldType       additionalProperties;

        [Ignore]public  string          name;
        [Ignore]public  bool?           isKey;
        /// <summary>WIP</summary>
                public  bool?           isAutoIncrement;
        /// <summary>if set the property is used as reference to entities in a database <b>container</b> named <see cref="relation"/> - <i>JSON Schema extension</i></summary>
                public  string          relation;
        /// <summary>optional property description</summary>
                public  string          description;

        public override string          ToString() => name;
    }

    /// <summary>
    /// Defines the input <see cref="param"/> and <see cref="result"/> of a command or message
    /// </summary>
    public sealed class MessageType
    {
        [Ignore]public  string          name;
        /// <summary>type of the command / message <see cref="param"/> - <i>JSON Schema extension</i></summary>
                public  FieldType       param;
        /// <summary>type of the command <see cref="result"/> - <i>JSON Schema extension</i><br/>
        /// messages return no <see cref="result"/></summary>
                public  FieldType       result;
        /// <summary>optional command / message description</summary>
                public  string          description;

        public override string          ToString() => name;
    }
    
    internal enum SchemaType {
        [Fri.EnumValue(Name = "null")]      Null,
        [Fri.EnumValue(Name = "object")]    Object,
        [Fri.EnumValue(Name = "string")]    String,
        [Fri.EnumValue(Name = "boolean")]   Boolean,
        [Fri.EnumValue(Name = "number")]    Number,
        [Fri.EnumValue(Name = "integer")]   Integer,
        [Fri.EnumValue(Name = "array")]     Array
    }
}