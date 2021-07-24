﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Mapper.Map;
using Friflo.Json.Flow.Schema.Utils;
using Friflo.Json.Flow.Schema.Utils.Mapper;

namespace Friflo.Json.Flow.Schema
{
    /// <summary>
    /// A context class required to be used for all code / schema generators.<br></br>
    /// Examples available at:
    /// <see href="https://github.com/friflo/Friflo.Json.Flow/blob/main/Json.Tests/Common/UnitTest/Flow/Schema/GenerateSchema.cs"/>
    /// <br></br>
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
    ///     No dependencies to third party libraries. E.g. Serializer libraries to create a specific format (YAML, ...)
    ///     Schema / code generators are forced to use <see cref="StringBuilder"/> like in <see cref="Typescript"/>     
    ///   </item>
    ///   <item>
    ///     A code generator must not have any mutable state. All used properties must be readonly immutables.
    ///   </item>
    ///   <item>
    ///     Avoid virtual methods or interfaces to establish simplicity. 
    ///   </item>
    ///   <item>
    ///     As initial template <see cref="Typescript"/> or <see cref="JsonSchema"/> need to be used to ensure the resulting
    ///     generator can be compared to their originals with tools like WinMerge.
    ///     In particular the methods and their order <see cref="Typescript.GenerateSchema"/>, <see cref="Typescript.EmitType"/>,
    ///     <see cref="Typescript.GetFieldType"/> and <see cref="Typescript.EmitPackageHeaders"/>.
    ///     Helper methods need to be added on the bottom.
    ///   </item>
    ///   <item>
    ///     The implementation should be small similar to <see cref="Typescript"/> and <see cref="JsonSchema"/>
    ///   </item>
    ///   <item>
    ///     The generated files (PocStore and Sync) must be committed to a folder in 'assets/Schema' and
    ///     a lean setup should be added ensuring the generated files are valid / compile successful.
    ///   </item>
    /// </list>
    /// </summary>
    public class Generator
    {
        public   readonly   string                                  fileExt;
        
        public   readonly   ITypeSystem                             system;
        private  readonly   Dictionary<ITyp, string>                standardTypes;

        /// map of all <see cref="TypeMapper"/>'s required by the types provided for schema generation
        public   readonly   ICollection<ITyp>                       types;
        /// map of all generated packages. key: package name  
        public   readonly   Dictionary<string, Package>             packages        = new Dictionary<string, Package>();
        
        // --- private
        /// map of all emitted types and their emitted code 
        private  readonly   Dictionary<ITyp, EmitType>              emitTypes       = new Dictionary<ITyp, EmitType>();
        /// set of generated files and their source content. key: file name
        public   readonly   Dictionary<string, string>              files           = new Dictionary<string, string>();
        /// Return a package name for the given type. By Default it is <see cref="Type.Namespace"/>
        private             Func<ITyp, string>                      getPackageName;
        private  readonly   Dictionary<ITyp, string>                packageCache    = new Dictionary<ITyp, string>();
        private  readonly   ICollection<string>                     stripNamespaces;

        public   readonly   ICollection<ITyp>                       separateTypes;
        
        public const string Note = "Generated by: https://github.com/friflo/Friflo.Json.Flow/tree/main/Json/Flow/Schema";

        public Generator (ITypeSystem system, ICollection<string> stripNamespaces, string fileExtension, ICollection<ITyp> separateTypes) {
            this.system             = system;
            fileExt                 = fileExtension;
            types                   = system.Types;
            this.stripNamespaces    = stripNamespaces ?? new List<string>();
            this.separateTypes      = separateTypes ?? new List<ITyp>();
            getPackageName          = GetPackageNameCallback;
            standardTypes           = GetStandardTypes(system);
        }
        
        public static string Indent(int max, string str) {
            return new string(' ', Math.Max(max - str.Length, 0));
        }
        
        private string Strip (string ns) {
            ns  = ns ?? "default";
            foreach (var stripNamespace in stripNamespaces) {
                var pos = ns.IndexOf(stripNamespace, StringComparison.InvariantCulture);
                if (pos == 0) {
                    return ns.Substring(stripNamespace.Length);
                }
            }
            return ns;
        }
        
        private string GetPackageNameCallback(ITyp type) {
            if (separateTypes.Contains(type)) {
                return $"{type.Namespace}.{type.Name}";
            }
            return type.Namespace;
        }
        
        public string GetPackageName (ITyp type) {
            if (packageCache.TryGetValue(type, out var packageName))
                return packageName;
            if (standardTypes.ContainsKey(type)) {
                packageName = "Standard";
            } else {
                packageName = getPackageName(type);
                packageName = Strip(packageName);
            }
            packageCache.Add(type, packageName);
            return packageName;
        }
        
        public string GetTypeName (ITyp type) {
            if (standardTypes.TryGetValue(type, out string typeName))
                return typeName;
            return type.Name;
        }
        
        public static void AddType (Dictionary<ITyp, string> types, ITyp type, string value) {
            if (type == null)
                return;
            types.Add(type, value);
        }
        
        private static Dictionary<ITyp, string> GetStandardTypes(ITypeSystem system) {
            var types = new Dictionary<ITyp, string>();
            AddType(types, system.Unit8,         "uint8" );
            AddType(types, system.Int16,         "int16" );
            AddType(types, system.Int32,         "int32" );
            AddType(types, system.Int64,         "int64" );
                
            AddType(types, system.Double,        "double" );
            AddType(types, system.Float,         "float" );
                
            AddType(types, system.BigInteger,    "BigInteger" );
            AddType(types, system.DateTime,      "DateTime" );
            return types;
        }
        
        /// <summary>
        /// Enables customizing package names for types. By Default it is <see cref="ITyp.Namespace"/>
        /// </summary> 
        public void SetPackageNameCallback (Func<ITyp, string> callback) {
            getPackageName = callback;
        }
        
        // ---------------------------------- retrieve type information ---------------------------------- 
        /* public static ITyp GetType(TypeMapper mapper) {
            if (mapper.isNullable && mapper.nullableUnderlyingType != null) {
                return mapper.nullableUnderlyingType;
            }
            return mapper.type;
        }
        
        public bool IsUnionType (ITyp type) {
            if (!typeMappers.TryGetValue(type, out var mapper))
                return false;
            var instanceFactory = mapper.InstanceFactory;
            return instanceFactory != null;
        }
        
        public bool IsDerivedField(ITyp type, PropField field) {
            var baseType = type.BaseType;
            while (baseType != null) {
                if (typeMappers.TryGetValue(baseType, out var mapper)) {
                    if (mapper.propFields.Contains(field.name))
                        return true;
                }
                baseType = baseType.BaseType;
            }
            return false;
        }
        
        public TypeMapper GetBaseMapper(ITyp type) {
            var baseType = type.BaseType;
            if (baseType == null)
                return null;
            TypeMapper mapper;
            
            // When searching for polymorph base class there may be are classes in this hierarchy. E.g. BinaryBoolOp. 
            // If these classes may have a protected constructor they need to be skipped. These classes have no TypeMapper. 
            while (!typeMappers.TryGetValue(baseType, out mapper)) {
                baseType = baseType.BaseType;
                if (baseType == null)
                    return null;
            }
            return mapper;
        } */
        
        // ---------------------------------- output generation  ---------------------------------- 
        public void AddEmitType(EmitType emit) {
            emitTypes.TryAdd(emit.type, emit);
        }
        
        public void GroupTypesByPackage(bool sortDependencies) {
            ICollection<EmitType> emits;
            if (sortDependencies) {
                emits = SortDependencies();
            } else {
                emits = emitTypes.Values;
            }
            foreach (var emit in emits) {
                var packageName = emit.package;
                if (!packages.TryGetValue(packageName, out var package)) {
                    packages.Add(packageName, package = new Package(packageName));
                }
                package.emitTypes.Add(emit);
                foreach (var type in emit.imports) {
                    if (package.imports.ContainsKey(type))
                        continue;
                    var typePackage = GetPackageName(type); 
                    var import = new Import(type, typePackage);  
                    package.imports.Add(type, import);
                }
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
        
        public static void Delimiter (StringBuilder sb, string delimiter, ref bool first) {
            if (first) {
                first = false;
                return;
            }
            sb.Append(delimiter);
        }
        
        public void CreateFiles(StringBuilder sb, Func<string, string> toFilename, string delimiter = null) {
            foreach (var pair in packages) {
                string      ns      = pair.Key;
                Package     package = pair.Value;
                sb.Clear();
                sb.AppendLine(package.header);
                bool first = true;
                foreach (var result in package.emitTypes) {
                    if (delimiter != null)
                        Delimiter(sb, delimiter, ref first);
                    sb.Append(result.content);
                }
                if (package.footer != null)
                    sb.AppendLine(package.footer);
                var filename = toFilename(ns);
                files.Add(filename, sb.ToString());
            }
        }
        
        /// <summary>
        /// Write the generated file to the given folder and remove all others file with the used <see cref="fileExt"/>
        /// </summary>
        public void WriteFiles(string folder) {
            // folder = Path.GetFullPath (folder);
            folder = folder.Replace('\\', '/');
            Directory.CreateDirectory(folder);
            string[] fileNames = Directory.GetFiles(folder, $"*{fileExt}", SearchOption.TopDirectoryOnly);
            for (int i = 0; i < fileNames.Length; i++) { fileNames[i] = fileNames[i].Replace('\\', '/'); }
            var fileSet = new HashSet<string>(fileNames);
            var utf8 = new UTF8Encoding(false);
            foreach (var file in files) {
                var filename    = file.Key;
                var content     = file.Value;
                var path = $"{folder}/{filename}";
                fileSet.Remove(path);
                var lastSlash = path.LastIndexOf("/", StringComparison.InvariantCulture);
                var fileFolder = lastSlash == -1 ? folder : path.Substring(0, lastSlash);
                Directory.CreateDirectory(fileFolder);
                File.WriteAllText(path, content, utf8);
            }
            foreach (var fileName in fileSet) {
                File.Delete(fileName);
            }
        }
    }
}