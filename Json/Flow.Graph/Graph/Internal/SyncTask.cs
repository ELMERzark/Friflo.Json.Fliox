﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using Friflo.Json.Flow.Database.Models;

namespace Friflo.Json.Flow.Graph.Internal
{
    public abstract class SyncTask
    {
        internal abstract   string      Label  { get; }
        internal abstract   TaskState   State { get; }

        internal bool IsOk(string syncError, out Exception e) {
            if (State.Error.Errors != null) {
                e = new TaskErrorException(State.Error.Errors);
                return false;
            }
            if (State.IsSynced()) {
                e = null;
                return true;
            }
            e = RequiresSyncError($"{syncError} requires Sync().");
            return false;
        }
        
        internal Exception AlreadySyncedError() {
            return new TaskAlreadySyncedException($"Task already synced. {Label}");
        }
        
        internal Exception RequiresSyncError(string message) {
            return new TaskNotSyncedException($"{message} {Label}");
        }
    }

    internal struct TaskState
    {
        internal bool       Synced { private get; set; }
        internal TaskError  Error  { get; set; }

        internal bool       IsSynced() { return Synced; }

        public override string ToString() => Synced ? Error.HasErrors ? $"synced - errors: {Error.Errors.Count}" : "synced" : "not synced";
    }
    
    internal struct TaskError
    {
        internal    List<EntityError>   Errors { get; private set; }
        internal    bool                HasErrors => Errors != null;

        internal void AddError(EntityError error) {
            if (Errors == null)
                Errors = new List<EntityError>();
            Errors.Add(error);
        }
    }
    
    
}