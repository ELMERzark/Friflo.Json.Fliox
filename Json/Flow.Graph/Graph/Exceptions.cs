﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Flow.Graph.Internal;
using Friflo.Json.Flow.Sync;

namespace Friflo.Json.Flow.Graph
{
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class UnresolvedRefException : Exception
    {
        public readonly Entity entity;
        
        public UnresolvedRefException(string message, Entity entity)
            : base ($"{message} Ref<{entity.GetType().Name}> id: {entity.id}")
        {
            this.entity = entity;
        }
    }
    
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class TaskNotSyncedException : Exception
    {
        public TaskNotSyncedException(string message) : base (message) { }
    }
    
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class TaskAlreadySyncedException : Exception
    {
        public TaskAlreadySyncedException(string message) : base (message) { }
    }
    
    public class TaskResultException : Exception
    {
        public readonly TaskError                               taskError;
        public readonly SortedDictionary<string, EntityError>   entityErrors;

        internal TaskResultException(TaskErrorInfo taskErrorInfo) : base(taskErrorInfo.GetMessage()) {
            taskError       = taskErrorInfo.TaskError;
            entityErrors    = taskErrorInfo.EntityErrors;
        }
    }
    
    public class TaskEntityError : TaskError
    {
        public readonly SortedDictionary<string, EntityError> entityErrors;

        internal TaskEntityError(SortedDictionary<string, EntityError> entityErrors) {
            this.entityErrors   = entityErrors;
            message             = $"Task failed by entity errors";
            type                = TaskErrorType.EntityErrors;
        }
    } 
}