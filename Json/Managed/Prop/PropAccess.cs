// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;

namespace Friflo.Json.Managed.Prop
{
    public class PropAccess
    {
               readonly Type        type;
        public readonly Type        typeInterface;
        public readonly Type        elementType;
        
        internal PropAccess (Type typeInterface, Type type, Type elementType)
        {
            this.type           = type;
            this.typeInterface  = typeInterface;
            this.elementType    = elementType;
        }
    }
}
