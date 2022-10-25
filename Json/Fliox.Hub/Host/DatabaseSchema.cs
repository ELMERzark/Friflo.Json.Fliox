﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host.Internal;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Hub.Remote;
using Friflo.Json.Fliox.Schema.Definition;
using Friflo.Json.Fliox.Schema.Language;
using Friflo.Json.Fliox.Schema.Native;
using Friflo.Json.Fliox.Schema.Validation;
using static System.Diagnostics.DebuggerBrowsableState;

namespace Friflo.Json.Fliox.Hub.Host
{
    /// <summary>
    /// A <see cref="DatabaseSchema"/> can be assigned to a <see cref="EntityDatabase.Schema"/> to enable validation
    /// of entities represented as JSON used in write operations - create, upsert and patch.
    /// </summary>
    /// <remarks>
    /// It is intended to be used for <see cref="RemoteHost"/> instances to ensure that the entities
    /// (records) in an <see cref="EntityContainer"/> always meet the expected type. So only successful validated JSON
    /// payloads are written to an <see cref="EntityContainer"/>.
    /// JSON validation includes the following checks:
    /// <list type="bullet">
    ///   <item>
    ///     Check if the type of a property matches the container entity type.<br/>
    ///     E.g. A container using a type 'Article' expect the property "name" of type string.
    ///     <code>{ "id": "test", "name": 123 }</code> will result in the error:
    ///     <code>WriteError: Article 'test', Incorrect type. was: 123, expect: string at Article > name</code>
    ///   </item>
    ///   <item>
    ///     Check if required properties defined in the container type are present in the JSON payload.<br/>
    ///     E.g. A container using a type 'Article' requires the property "name" being present.
    ///     <code>{ "id": "test" }</code> will result in the error:
    ///     <code>WriteError: Article 'test', Missing required fields: [name] at Article > (root)</code>
    ///   </item>
    ///   <item>
    ///     Check that no unknown properties are present in a JSON payload<br/>
    ///     E.g. A container using a type 'Article' expect only the properties 'id' and 'name'.
    ///     <code>{ "id": "test", "name": "Phone", "foo": "Bar" }</code> will result in the error:
    ///     <code>WriteError: Article 'test', Unknown property: 'foo' at Article > foo</code>
    ///   </item>
    /// </list>   
    /// </remarks>
    public sealed class DatabaseSchema
    {
        public   readonly   TypeSchema                          typeSchema;
        [DebuggerBrowsable(Never)]
        private  readonly   Dictionary<string, ValidationType>  containerTypes  = new Dictionary<string, ValidationType>();
        // ReSharper disable once UnusedMember.Local - expose Dictionary as list in Debugger
        private             IReadOnlyCollection<ValidationType> ContainerTypes  => containerTypes.Values;
        
        private             Dictionary<string, JsonValue>       jsonSchemas; // cache schemas after creation
        
        internal            string                              Name        => typeSchema.RootType.Name;
        internal            string                              Path        => typeSchema.RootType.Path + ".json";

        public   override   string                              ToString()  => typeSchema.RootType.Name;

        public DatabaseSchema(TypeSchema typeSchema) {
            this.typeSchema = typeSchema;
            AddTypeSchema(typeSchema);
            AddStoreSchema<SequenceStore>();
        }
        
        public void AddStoreSchema<TClient>() where TClient : FlioxClient {
            var nativeSchema    = NativeTypeSchema.Create(typeof(TClient));
            AddTypeSchema(nativeSchema);
        }
        
        public void AddTypeSchema(TypeSchema typeSchema) {
            var rootType = typeSchema.RootType;
            if (rootType == null)
                throw new InvalidOperationException($"Expect {nameof(TypeSchema)}.{nameof(TypeSchema.RootType)} not null");
            var validationSet   = new ValidationSet(typeSchema);
            var validationRoot = validationSet.GetValidationType(rootType);
            foreach (var field in validationRoot.Fields) {
                containerTypes.Add(field.fieldName, field);
            }
        }

        public string ValidateEntities (
            string                  container,
            List<JsonEntity>        entities,
            SyncContext             syncContext,
            EntityErrorType         errorType,
            ref List<EntityError>   validationErrors
        ) {
            if (!containerTypes.TryGetValue(container, out ValidationType type)) {
                return $"No Schema definition for container Type: {container}";
            }
            using (var pooled = syncContext.pool.TypeValidator.Get()) {
                TypeValidator validator = pooled.instance;
                for (int n = 0; n < entities.Count; n++) {
                    var entity = entities[n];
                    // if (entity.json == null)  continue; // TAG_ENTITY_NULL
                    if (!validator.ValidateObject(entity.value, type, out string error)) {
                        if (validationErrors == null) {
                            validationErrors = new List<EntityError>();
                        }
                        entities[n] = default;
                        validationErrors.Add(new EntityError(errorType, container, entity.key, error));
                    }
                }
            }
            if (validationErrors == null)
                return null;
            
            // Remove invalid entries from entities
            int pos = 0;
            for (int n = 0; n < entities.Count; n++) {
                var entity = entities[n];
                if (entity.value.IsNull())
                    continue;
                entities  [pos] = entity;
                pos++;
            }
            int count = entities.Count - pos;
            entities.RemoveRange  (pos, count);
            return null;
        }
        
        internal Dictionary<string, JsonValue> GetJsonSchemas() {
            var schemas = jsonSchemas;
            if (schemas != null)
                return schemas;
            var entityTypes = typeSchema.GetEntityTypes().Values;
            var generator   = new Generator(typeSchema, ".json", null, entityTypes);
            JsonSchemaGenerator.Generate(generator);
            schemas = new Dictionary<string, JsonValue>();
            foreach (var pair in generator.files) {
                schemas.Add(pair.Key,new JsonValue(pair.Value));
            }
            jsonSchemas = schemas;
            return schemas;
        }
        
        public string[] GetContainers() {
            var rootType        = typeSchema.RootType;
            var fields          = rootType.Fields;
            var containerList   = new string [fields.Count];
            int n = 0;
            foreach (var field in fields) {
                containerList[n++] = field.name;
            }
            return containerList;
        }
        
        public string[] GetCommands() {
            var rootType = typeSchema.RootType;
            return GetMessages(rootType.Commands);
        }
        
        public string[] GetMessages() {
            var rootType = typeSchema.RootType;
            return GetMessages(rootType.Messages);
        }

        private static string[] GetMessages(IReadOnlyList<MessageDef> messageDefs) {
            if (messageDefs == null)
                return Array.Empty<string>();
            var result      = new string [messageDefs.Count];
            int n = 0;
            foreach (var field in messageDefs) {
                result[n++] = field.name;
            }
            return result;
        }
    }
 
}