﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Mapper.Map;
using Friflo.Json.Mapper.Map.Obj.Class.IL;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Mapper
{
    // This class contains IL specific state/data which is used by JsonReader & JsonWriter. So its not thread safe.
    partial class JsonWriter
    {
        private  readonly   List<ClassPayload>      handlerStack = new List<ClassPayload>(16);
        private             int                     classLevel;
        internal readonly   bool                    useIL;

        private void DisposePayloads() {
            for (int n = 0; n < handlerStack.Count; n++)
                handlerStack[n].Dispose();
        }

        private void ClearObjectReferences() {
            for (int n = 0; n < handlerStack.Count; n++)
                handlerStack[n].ClearObjectReferences();
        }
        
        /// <summary> Load the fields of a class instance into the <see cref="ClassPayload.data"/> array. </summary>
        internal ClassPayload InstanceLoad(TypeMapper classType, object obj) {
            if (classLevel >= handlerStack.Count)
                handlerStack.Add(new ClassPayload());
            var handler = handlerStack[classLevel++];
            handler.LoadInstance(classType, obj);
            return handler;
        }

        internal void InstancePop() {
            --classLevel;
        }
    }
}