﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Friflo.Json.Fliox.Mapper.Utils;

namespace Friflo.Json.Fliox.Mapper.Map.Utils
{
    public static class CommandUtils
    {
        private static readonly Dictionary<Type, CommandInfo[]> CommandInfoCache = new Dictionary<Type, CommandInfo[]>();

        private const string CommandType = "Friflo.Json.Fliox.Hub.Client.CommandTask`1";

        public static CommandInfo[] GetCommandTypes(Type type) {
            if (CommandInfoCache.TryGetValue(type, out  CommandInfo[] result)) {
                return result;
            }
            var commands = new List<CommandInfo>();
            var flags   = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            MethodInfo[] methods = type.GetMethods(flags);
            for (int n = 0; n < methods.Length; n++) {
                var  method         = methods[n];
                if (!IsCommand(method, out CommandInfo commandInfo))
                    continue;
                commands.Add(commandInfo);
            }
            if (commands.Count == 0) {
                CommandInfoCache[type] = null;
                return null;
            }
            var array = commands.ToArray();
            CommandInfoCache[type] = array;
            return array;
        }
        
        private static bool IsCommand(MethodInfo methodInfo, out CommandInfo commandInfo) {
            commandInfo = new CommandInfo();
            var returnType = methodInfo.ReturnType;
            if (!returnType.IsGenericType)
                return false;
            if (returnType.GetGenericTypeDefinition().FullName != CommandType)
                return false;
            var returnTypeArgs = returnType.GenericTypeArguments;
            if (returnTypeArgs.Length != 1)
                return false;
            var resultType = returnTypeArgs[0];
            if (resultType.IsGenericParameter)
                return false;
            var parameters = methodInfo.GetParameters();
            if (parameters.Length != 1)
                return false;
            var name = AttributeUtils.CommandName(methodInfo.CustomAttributes);
            if (name == null)
                name = methodInfo.Name;
            var param = parameters[0];
            var valueType = param.ParameterType;
            commandInfo = new CommandInfo(name, valueType, resultType);
            return true;
        }
    }
    
    public readonly struct CommandInfo {
        public  readonly    string  name;
        public  readonly    Type    valueType;
        public  readonly    Type    resultType;

        public  override    string  ToString() => name;

        internal CommandInfo (
            string         name,
            Type           valueType,
            Type           resultType)
        {
            this.name       = name;
            this.valueType  = valueType;
            this.resultType = resultType;
        }
    }
}