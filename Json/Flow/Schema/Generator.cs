﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Mapper.Map;
using Friflo.Json.Flow.Mapper.Map.Obj.Reflect;
using Friflo.Json.Flow.Schema.Utils;

namespace Friflo.Json.Flow.Schema
{
    public class Generator
    {
        public   readonly   string                                  extension;
        /// map of all <see cref="TypeMapper"/>'s required by the types provided for schema generation
        public   readonly   IReadOnlyDictionary<Type, TypeMapper>   typeMappers;
        /// map of all generated packages. key: package name  
        public   readonly   Dictionary<string, Package>             packages    = new Dictionary<string, Package>();
        
        // --- private
        /// map of all emitted types and their emitted code 
        private  readonly   Dictionary<Type, EmitType>              emitTypes   = new Dictionary<Type, EmitType>();
        /// set of generated files and their source content. key: file name
        private  readonly   Dictionary<string, string>              files       = new Dictionary<string, string>();
        /// Return a package name for the given type. By Default it is <see cref="Type.Namespace"/>
        private             Func<Type, string>                      getPackageName = type => type.Namespace;

        public Generator (TypeStore typeStore, string extension) {
            this.extension  = extension;
            typeMappers     = typeStore.GetTypeMappers();
        }
        
        public static string Indent(int max, string str) {
            return new string(' ', Math.Max(max - str.Length, 0));
        }
        
        public string GetPackageName (Type type) {
            return getPackageName(type);
        }
        
        /// <summary>
        /// Enables customizing package names for types. By Default it is <see cref="Type.Namespace"/>
        /// </summary> 
        public void SetPackageNameCallback (Func<Type, string> callback) {
            getPackageName = callback;
        }
        
        public bool IsUnionType (Type type) {
            var instanceFactory = typeMappers[type].instanceFactory;
            return instanceFactory != null;
        }
        
        public bool IsDerivedField(Type type, PropField field) {
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

        public static TypeMapper GetUnderlyingTypeMapper (TypeMapper mapper) {
            var underlyingMapper    = mapper.GetUnderlyingMapper();
            if (underlyingMapper != null)
                return underlyingMapper;
            return mapper;
        }
        
        /// <summary>Same as <see cref="GetUnderlyingTypeMapper"/> - keep two equal version for debugging</summary>
        public static TypeMapper GetUnderlyingFieldMapper (TypeMapper mapper) {
            var underlyingMapper    = mapper.GetUnderlyingMapper();
            if (underlyingMapper != null)
                return underlyingMapper;
            return mapper;
        }
        
        public void AddEmitType(EmitType emit) {
            emitTypes.Add(emit.mapper.type, emit);
        }
        
        public void GroupTypesByPackage() {
            foreach (var pair in emitTypes) {
                EmitType    emit        = pair.Value;
                var         packageName = emit.package;
                if (!packages.TryGetValue(packageName, out var package)) {
                    packages.Add(packageName, package = new Package());
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
        /// Write the generated file to the given folder and remove all others file with the used <see cref="extension"/>
        /// </summary>
        public void WriteFiles(string folder) {
            folder = folder.Replace('\\', '/');
            string[] fileNames = Directory.GetFiles(folder, $"*{extension}", SearchOption.TopDirectoryOnly);
            for (int i = 0; i < fileNames.Length; i++) {
                fileNames[i] = fileNames[i].Replace('\\', '/');
            }
            var fileSet = new HashSet<string>(fileNames);
            foreach (var file in files) {
                var filename    = file.Key;
                var content     = file.Value;
                var path = $"{folder}/{filename}";
                fileSet.Remove(path);
                var lastSlash = path.LastIndexOf("/", StringComparison.InvariantCulture);
                var fileFolder = lastSlash == -1 ? folder : path.Substring(0, lastSlash);
                Directory.CreateDirectory(fileFolder);
                File.WriteAllText(path, content, Encoding.UTF8);
            }
            foreach (var fileName in fileSet) {
                File.Delete(fileName);
            }
        }
        
        public TypeMapper GetPolymorphBaseMapper(Type type) {
            var baseType = type.BaseType;
            if (baseType == null)
                throw new InvalidOperationException("");
            TypeMapper mapper;
            
            // When searching for polymorph base class there may be are classes in this hierarchy. E.g. BinaryBoolOp. 
            // If these classes may have a protected constructor they need to be skipped. These classes have no TypeMapper. 
            while (!typeMappers.TryGetValue(baseType, out mapper)) {
                baseType = baseType.BaseType;
                if (baseType == null)
                    throw new InvalidOperationException("");
            }
            return mapper;
        }
    }
}