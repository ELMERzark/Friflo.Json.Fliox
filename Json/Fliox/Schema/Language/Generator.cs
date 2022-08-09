﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Friflo.Json.Fliox.Schema.Definition;
using Friflo.Json.Fliox.Schema.Utils;

namespace Friflo.Json.Fliox.Schema.Language
{
    /// <summary>
    /// A context class required to be used for all code / schema generators.<br></br>
    /// Examples available at:
    /// <a href="https://github.com/friflo/Friflo.Json.Fliox/tree/main/Json.Tests/Common/UnitTest/Fliox/Schema">Schema unit tests</a>
    /// <br/>
    /// 
    /// It contains the configuration for schema generation and the result after schema generation was executed.
    /// The main configuration consists of the types to be generated and the file extension for generated files.
    /// The result consists of the generated source <see cref="files"/>.
    /// The utility method <see cref="WriteFiles"/> enables writing these files to a folder.
    /// 
    /// <br></br>
    /// In case of adding an additional code generator which should be part of this project the following
    /// requirements must be met:
    /// <list type="bullet">
    ///   <item>
    ///     In case a code generator is added to the library Friflo.Json.Fliox no dependencies to third party libraries
    ///     must be added. E.g. Serializer libraries to create a specific format (YAML, ...).
    ///     In case of third party dependencies the implementation requires a separate library. 
    ///     Schema / code generators are forced to use <see cref="StringBuilder"/> like in <see cref="TypescriptGenerator"/>.
    ///   </item>
    ///   <item>
    ///     A code generator must not have any mutable state. All used properties must be readonly immutables.
    ///   </item>
    ///   <item>
    ///     Avoid virtual methods or interfaces to establish simplicity. 
    ///   </item>
    ///   <item>
    ///     As initial template <see cref="TypescriptGenerator"/> or <see cref="JsonSchemaGenerator"/> need to be used
    ///     to ensure the resulting generator can be compared to their originals with tools like WinMerge.
    ///     In particular the methods and their order:
    ///     <list type="bullet">
    ///       <item>private constructor using <see cref="Generator"/> as parameter</item>
    ///       <item><see cref="TypescriptGenerator.Generate(Generator)"/></item>
    ///       <item><see cref="TypescriptGenerator.GetStandardTypes"/></item>
    ///       <item><see cref="TypescriptGenerator.EmitStandardType"/></item>
    ///       <item><see cref="TypescriptGenerator.EmitType"/></item>
    ///       <item><see cref="TypescriptGenerator.EmitClassType"/></item>
    ///       <item><see cref="TypescriptGenerator.EmitMessages"/></item>
    ///       <item><see cref="TypescriptGenerator.GetMessageArg"/></item>
    ///       <item><see cref="TypescriptGenerator.GetFieldType"/></item>
    ///       <item><see cref="TypescriptGenerator.GetElementType"/></item>
    ///       <item><see cref="TypescriptGenerator.GetTypeName"/></item>
    ///       <item><see cref="TypescriptGenerator.GetDoc"/></item>
    ///       <item><see cref="TypescriptGenerator.EmitFileHeaders"/></item>
    ///     </list>
    ///     Helper methods need to be added on the bottom.
    ///   </item>
    ///   <item>
    ///     The implementation should be small similar to <see cref="TypescriptGenerator"/> and <see cref="JsonSchemaGenerator"/>
    ///   </item>
    ///   <item>
    ///     The generated files (PocStore and Protocol) must be committed to a folder in 'assets~/Schema' and
    ///     a lean setup should be added ensuring the generated files are valid / compile successful.
    ///   </item>
    /// </list>
    /// </summary>
    public sealed class Generator
    {
        public   readonly   TypeSchema                      typeSchema;
        public   readonly   string                          fileExt;
        
        public   readonly   TypeDef                         rootType;
        public   readonly   StandardTypes                   standardTypes;
        public   readonly   string                          databaseUrl;


        /// map of all <see cref="TypeDef"/>'s required by the types provided for schema generation
        public   readonly   IReadOnlyList<TypeDef>          types;
        /// map of all generated files. key: file path  
        public   readonly   Dictionary<string, EmitFile>    fileEmits       = new Dictionary<string, EmitFile>();
        /// set of generated files and their source content. key: file name
        public   readonly   Dictionary<string, string>      files           = new Dictionary<string, string>();
        /// set of files where each type is generated into a separate file
        public   readonly   ICollection<TypeDef>            separateTypes;
        
        // --- private
        /// map of all emitted types and their emitted code 
        private  readonly   Dictionary<TypeDef, EmitType>   emitTypes       = new Dictionary<TypeDef, EmitType>();
        private  readonly   ICollection<Replace>            replacements;

        public const string Note = "Generated by: " + Link;
        public const string Link = "https://github.com/friflo/Friflo.Json.Fliox#schema";

        /// <summary>
        /// The generator context class used for specific code generators like <see cref="TypescriptGenerator"/>.
        /// This class contains also general configuration for code generation. 
        /// </summary>
        /// <param name="schema">
        ///   The schema containing the types used to generate code. The library itself provide
        ///   <see cref="Native.NativeTypeSchema"/> and <see cref="JSON.JsonTypeSchema"/> as input schemas.
        /// </param>
        /// <param name="fileExtension">
        ///   file extension of the generated files
        /// </param>
        /// <param name="replacements">
        ///   Optional namespace prefixes used short the namespaces used in the generated files
        /// </param>
        /// <param name="separateTypes">
        ///   Optional list of types enabling each listed type is generated in its own file.
        ///   JSON Schema related tools often expect this schema structure like VSCode. See:
        ///   [JSON editing in Visual Studio Code] https://code.visualstudio.com/docs/languages/json#_mapping-in-the-user-settings
        /// </param>
        /// <param name="getPath">
        ///   An optional callback function used to customize the path of generated files.
        ///   E.g. in case of Java it would be: <code>type => $"{type.Namespace}.{type.Name}"</code> to generate
        ///   each type in its own file.
        /// </param>
        /// <param name="databaseUrl">database url for OpenAPI</param>
        public Generator (
            TypeSchema              schema,
            string                  fileExtension,
            ICollection<Replace>    replacements    = null,
            ICollection<TypeDef>    separateTypes   = null,
            Func<TypeDef, string>   getPath         = null,
            string                  databaseUrl     = null)
        {
            rootType                = schema.RootType;
            standardTypes           = schema.StandardTypes;
            fileExt                 = fileExtension;
            types                   = schema.Types;
            this.replacements       = replacements  ?? new List<Replace>();
            this.separateTypes      = separateTypes ?? new List<TypeDef>();
            typeSchema              = schema;
            this.databaseUrl        = databaseUrl;
            
            getPath = getPath ?? GetPathCallback;
            foreach (var type in types) {
                var path    = getPath(type);
                path        = Strip(path);
                type.Path   = path;
            }
        }
        
        public TypeDef FindTypeDef (string @namespace, string name) {
            return typeSchema.FindTypeDef(@namespace, name);
        }
        
        public TypeDef FindSchemaType() {
            foreach (var type in types) {
                if (type.IsSchema)
                    return type;
            }
            return null;
        }
        
        private string Strip (string ns) {
            ns  = ns ?? "default";
            foreach (var replacement in replacements) {
                var search = replacement.@namespace;
                var pos = ns.IndexOf(search, StringComparison.InvariantCulture);
                if (pos == 0) {
                    var subStr = ns.Substring(search.Length);
                    var replace = replacement.replacement;
                    return replace.Length >= 0 ? replace + subStr : subStr;
                }
            }
            return ns;
        }
        
        private string GetPathCallback(TypeDef type) {
            if (separateTypes.Contains(type)) {
                return $"{type.Namespace}.{type.Name}";
            }
            return type.Namespace;
        }
        
        public static void AddType (Dictionary<TypeDef, string> types, TypeDef type, string value) {
            if (type == null)
                return;
            types.Add(type, value);
        }
        
        // ---------------------------------- output generation  ----------------------------------
        public void AddEmitType(EmitType emit) {
            emitTypes.Add(emit.type, emit);
        }
        
        public void GroupTypesByPath(bool sortDependencies) {
            ICollection<EmitType> emits;
            if (sortDependencies) {
                emits = SortDependencies();
            } else {
                emits = emitTypes.Values;
            }
            foreach (var emit in emits) {
                var filePath = emit.path;
                if (!fileEmits.TryGetValue(filePath, out var emitFile)) {
                    var @namespace = Strip(emit.type.Namespace);
                    fileEmits.Add(filePath, emitFile = new EmitFile(filePath, @namespace));
                }
                emitFile.emitTypes.Add(emit);
                foreach (var type in emit.imports) {
                    if (emitFile.imports.ContainsKey(type))
                        continue;
                    var import = new Import(type, Strip(type.Namespace));
                    emitFile.imports.Add(type, import);
                }
            }
        }
        
        public void GroupToSingleFile(string name) {
            ICollection<EmitType> emits = emitTypes.Values;
            var @namespace = Strip(name);
            var emitFile = new EmitFile($"{name}{fileExt}", @namespace);
            fileEmits.Add(name, emitFile);
            foreach (var emit in emits) {
                emitFile.emitTypes.Add(emit);
            }
        }
        
        private ICollection<EmitType> SortDependencies() {
            foreach (var pair in emitTypes) {
                var emitType = pair.Value;
                foreach (var type in emitType.typeDependencies) {
                    var emitDependency = emitTypes[type];
                    emitType.emitDependencies.Add(emitDependency);
                }
            }
            return TopologicalSort.Sort(emitTypes.Values, x => x.emitDependencies);
        }
        
        public static string Indent(int max, string str) {
            return new string(' ', Math.Max(max - str.Length, 0));
        }
        
        public static void Delimiter (StringBuilder sb, string delimiter, ref bool first) {
            if (first) {
                first = false;
                return;
            }
            sb.Append(delimiter);
        }
        
        public void EmitFiles(StringBuilder sb, Func<string, string> toFilename, string delimiter = null) {
            foreach (var pair in fileEmits) {
                string      path        = pair.Key;
                EmitFile    emitFile    = pair.Value;
                sb.Clear();
                sb.AppendLF(emitFile.header);
                bool first = true;
                foreach (var result in emitFile.emitTypes) {
                    if (delimiter != null)
                        Delimiter(sb, delimiter, ref first);
                    sb.Append(result.content);
                }
                if (emitFile.footer != null)
                    sb.AppendLF(emitFile.footer);
                var filename = toFilename(path);
                files.Add(filename, sb.ToString());
            }
        }
        
        public List<EmitFile> OrderNamespaces() {
            var emitFiles   = new List<EmitFile>(fileEmits.Values);
            emitFiles.Sort((file1, file2) => {
                // namespace Standard to bottom
                if (file1.@namespace == "Standard")
                    return +1;
                if (file2.@namespace == "Standard")
                    return -1;
                // namespace containing root type (schema) on top
                var type1 = file1.emitTypes[0].type; 
                var type2 = file2.emitTypes[0].type; 
                if (type1 == rootType)
                    return -1;
                if (type2 == rootType)
                    return +1;
                // remaining namespace by comparing theirs names
                return string.Compare(file1.@namespace, file2.@namespace, StringComparison.Ordinal);
            });
            return emitFiles;
        }
        
        /// <summary>
        /// Write the generated file to the given folder and remove all others file with the used <see cref="fileExt"/>
        /// </summary>
        public void WriteFiles(string folder, bool cleanFolder = true) {
            WriteFilesInternal(folder, files, fileExt, cleanFolder);
        }
        
        internal static void WriteFilesInternal(string folder, IReadOnlyDictionary<string, string> files, string fileExt, bool cleanFolder = true) {
            // folder = Path.GetFullPath (folder);
            folder = folder.Replace('\\', '/');
            Directory.CreateDirectory(folder);
            string[] fileNames = Directory.GetFiles(folder, $"*{fileExt}", SearchOption.TopDirectoryOnly);
            for (int i = 0; i < fileNames.Length; i++) { fileNames[i] = fileNames[i].Replace('\\', '/'); }
            var fileSet = new HashSet<string>(fileNames);
            var utf8    = new UTF8Encoding(false);
            foreach (var file in files) {
                var filename    = file.Key;
                var content     = file.Value;
                var path        = $"{folder}/{filename}";
                fileSet.Remove(path);
                var lastSlash   = path.LastIndexOf("/", StringComparison.InvariantCulture);
                var fileFolder  = lastSlash == -1 ? folder : path.Substring(0, lastSlash);
                Directory.CreateDirectory(fileFolder);
                File.WriteAllText(path, content, utf8);
            }
            if (!cleanFolder)
                return;
            foreach (var fileName in fileSet) {
                File.Delete(fileName);
            }
        }
    }
}