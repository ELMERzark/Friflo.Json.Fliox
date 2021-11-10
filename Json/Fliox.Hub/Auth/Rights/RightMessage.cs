﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Mapper;

// ReSharper disable CollectionNeverUpdated.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnassignedField.Global
namespace Friflo.Json.Fliox.Hub.Auth.Rights
{
    public sealed class RightMessage : Right
    {
                        public  string          database;
        [Fri.Required]  public  List<string>    names;
        public  override        RightType       RightType => RightType.message;
        
        public override Authorizer ToAuthorizer() {
            if (names.Count == 1) {
                return new AuthorizeMessage(names[0], database);
            }
            var list = new List<Authorizer>(names.Count);
            foreach (var message in names) {
                list.Add(new AuthorizeMessage(message, database));
            }
            return new AuthorizeAny(list);
        }
    }
}