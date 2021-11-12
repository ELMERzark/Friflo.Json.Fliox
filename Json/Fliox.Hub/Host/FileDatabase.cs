﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.


using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host.Utils;
using Friflo.Json.Fliox.Hub.Threading;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.Hub.Host
{
    public sealed class FileDatabase : EntityDatabase
    {
        private  readonly   string  databaseFolder;
        private  readonly   bool    pretty;
        
        public FileDatabase(string databaseFolder, TaskHandler handler = null, DbOpt opt = null, bool pretty = true)
            : base(handler, opt)
        {
            this.pretty = pretty;
            this.databaseFolder = databaseFolder + "/";
            Directory.CreateDirectory(databaseFolder);
        }

        public override EntityContainer CreateContainer(string name, EntityDatabase database) {
            return new FileContainer(name, this, databaseFolder, pretty);
        }
    }
    
    public sealed class FileContainer : EntityContainer
    {
        private  readonly   string                  folder;
        private  readonly   AsyncReaderWriterLock   rwLock;

        public   override   bool                    Pretty      { get; }


        public FileContainer(string name, EntityDatabase database, string databaseFolder, bool pretty)
            : base (name, database)
        {
            this.Pretty = pretty;
            this.folder = databaseFolder + instanceName + "/";
            rwLock      = new AsyncReaderWriterLock();
            Directory.CreateDirectory(folder);
        }

        public override void Dispose() {
            rwLock.Dispose();
            base.Dispose();
        }

        private string FilePath(string key) {
            return folder + key + ".json";
        }
        
        public override async Task<CreateEntitiesResult> CreateEntities(CreateEntities command, MessageContext messageContext) {
            var entities = command.entities;
            AssertEntityCounts(command.entityKeys, entities);
            Dictionary<JsonKey, EntityError> createErrors = null;
            await rwLock.AcquireWriterLock().ConfigureAwait(false);
            try {
                for (int n = 0; n < entities.Count; n++) {
                    var payload = entities[n];
                    // if (payload.json == null)  continue; // TAG_ENTITY_NULL
                    var key     = command.entityKeys[n];
                    var path = FilePath(key.AsString());
                    try {
                        await WriteText(path, payload, FileMode.CreateNew).ConfigureAwait(false);
                    } catch (Exception e) {
                        var error = CreateEntityError(EntityErrorType.WriteError, key, e);
                        AddEntityError(ref createErrors, key, error);
                    }
                }
            } finally {
                rwLock.ReleaseWriterLock();
            }
            return new CreateEntitiesResult{createErrors = createErrors};
        }

        public override async Task<UpsertEntitiesResult> UpsertEntities(UpsertEntities command, MessageContext messageContext) {
            var entities = command.entities;
            AssertEntityCounts(command.entityKeys, entities);
            Dictionary<JsonKey, EntityError> upsertErrors = null;
            await rwLock.AcquireWriterLock().ConfigureAwait(false);
            try {
                for (int n = 0; n < entities.Count; n++) {
                    var payload = entities[n];
                    // if (payload.json == null)  continue; // TAG_ENTITY_NULL
                    var key     = command.entityKeys[n];
                    var path = FilePath(key.AsString());
                    try {
                        await WriteText(path, payload, FileMode.Create).ConfigureAwait(false);
                    } catch (Exception e) {
                        var error = CreateEntityError(EntityErrorType.WriteError, key, e);
                        AddEntityError(ref upsertErrors, key, error);
                    }
                }
            } finally {
                rwLock.ReleaseWriterLock();
            }
            return new UpsertEntitiesResult{upsertErrors = upsertErrors};
        }

        public override async Task<ReadEntitiesSetResult> ReadEntitiesSet(ReadEntitiesSet command, MessageContext messageContext) {
            var keys        = command.ids;
            var entities    = new Dictionary<JsonKey, EntityValue>(keys.Count, JsonKey.Equality);
            await rwLock.AcquireReaderLock().ConfigureAwait(false);
            try {
                foreach (var key in keys) {
                    var filePath = FilePath(key.AsString());
                    EntityValue entry;
                    if (File.Exists(filePath)) {
                        try {
                            var payload = await ReadText(filePath).ConfigureAwait(false);
                            entry = new EntityValue(payload);
                        } catch (Exception e) {
                            var error = CreateEntityError(EntityErrorType.ReadError, key, e);
                            entry = new EntityValue(error);
                        }
                    } else {
                        entry = new EntityValue();
                    }
                    entities.TryAdd(key, entry);
                }
            } finally {
                rwLock.ReleaseReaderLock();
            }
            var result = new ReadEntitiesSetResult{entities = entities};
            result.ValidateEntities(name, command.keyName, messageContext);
            return result;
        }

        public override async Task<QueryEntitiesResult> QueryEntities(QueryEntities command, MessageContext messageContext) {
            var ids     = GetIds(folder);
            var result  = await FilterEntityIds(command, ids, messageContext).ConfigureAwait(false);
            return result;
        }

        public override async Task<DeleteEntitiesResult> DeleteEntities(DeleteEntities command, MessageContext messageContext) {
            var keys = command.ids;
            Dictionary<JsonKey, EntityError> deleteErrors = null;
            await rwLock.AcquireWriterLock().ConfigureAwait(false);
            try {
                if (keys != null && keys.Count > 0) {
                    foreach (var key in keys) {
                        string path = FilePath(key.AsString());
                        try {
                            DeleteFile(path);
                        } catch (Exception e) {
                            var error = CreateEntityError(EntityErrorType.DeleteError, key, e);
                            AddEntityError(ref deleteErrors, key, error);
                        }
                    }
                }
                var all = command.all;
                if (all != null && all.Value) {
                    string[] fileNames = Directory.GetFiles(folder, "*.json", SearchOption.TopDirectoryOnly);
                    foreach (var fileName in fileNames) {
                        DeleteFile(fileName);
                    }
                }
            } finally {
                rwLock.ReleaseWriterLock();
            }
            var result = new DeleteEntitiesResult{deleteErrors = deleteErrors};
            return result;
        }


        // -------------------------------------- helper methods --------------------------------------
        private static string GetHResultDetails(int hresult) {
            var lng = hresult & 0xffffffffL;
            switch (lng) {
                case 0x0000007B:   return "invalid file name";
                case 0x80070002:   return "file not found";
                case 0x80070050:   return "file already exists";
                case 0x80070052:   return "file cannot be created";
                case 0x80070570:   return "file corrupt";
            }
            return null;
        }
        
        private EntityError CreateEntityError (EntityErrorType type, in JsonKey key, Exception e) {
            var hresult = e.HResult;
            var details = GetHResultDetails(hresult);
            var sb = new StringBuilder();
            sb.Append($"HResult: 0x{hresult:X8}");
            if (details != null) {
                sb.Append(" - ");
                sb.Append(details);
            }
            var error = new EntityError(type, name, key, sb.ToString());
            return error;
        }
        
        private static HashSet<JsonKey> GetIds(string folder) {
            string[] fileNames = Directory.GetFiles(folder, "*.json", SearchOption.TopDirectoryOnly);
            var ids = Helper.CreateHashSet(fileNames.Length, JsonKey.Equality);
            for (int n = 0; n < fileNames.Length; n++) {
                var fileName = fileNames[n];
                var len = fileName.Length;
                var id = fileName.Substring(folder.Length, len - folder.Length - ".json".Length);
                ids.Add(new JsonKey(id));
            }
            return ids;
        }
        
        /// <summary>
        /// Write with <see cref="FileShare.Read"/> as on a developer machine other processes like virus scanner or file watcher
        /// may access the file concurrently resulting in:
        /// IOException: The process cannot access the file 'path' because it is being used by another process
        /// </summary>
        private static async Task WriteText(string filePath, JsonValue json, FileMode fileMode) {
            using (var destStream = new FileStream(filePath, fileMode, FileAccess.Write, FileShare.Read, bufferSize: 4096, useAsync: false)) {
                await destStream.WriteAsync(json, 0, json.Length).ConfigureAwait(false);
            }
        }
        
        private static async Task<JsonValue> ReadText(string filePath) {
            using (var sourceStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: false)) {
                return await EntityUtils.ReadToEnd(sourceStream).ConfigureAwait(false);
            }
        }
        
        private static void DeleteFile(string filePath) {
            File.Delete(filePath);
        }
    }
}
