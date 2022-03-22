﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Fliox.Mapper;

// ReSharper disable CollectionNeverUpdated.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnassignedField.Global
namespace Friflo.Json.Fliox.Hub.Host.Auth.Rights
{
    public sealed class PredicateRight : Right
    {
        /// <summary>a specific predicate: 'TestPredicate', multiple predicates by prefix: 'Test*', all predicates: '*'</summary>
        [Fri.Required]  public  List<string>    names;
        
        public  override        RightType       RightType => RightType.predicate;
        public  override        IAuthorizer     ToAuthorizer() => throw new NotImplementedException();
        
    }
    
    // ReSharper disable InconsistentNaming
    public enum RightType {
        allow,
        task,
        message,
        subscribeMessage,
        operation,
        predicate
    }
}