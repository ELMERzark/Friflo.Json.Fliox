// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using Friflo.Json.Burst;
using Friflo.Json.Burst.Utils;
using Friflo.Json.Fliox.Mapper.Utils;

namespace Friflo.Json.Fliox.Mapper.Map.Obj.Reflect
{
    // PropertyFields
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public sealed class PropertyFields : IDisposable
    {
        public   readonly   PropField []                    fields;
        public   readonly   Bytes32 []                      names32;
        public   readonly   int                             num;
        public   readonly   int                             primCount;
        public   readonly   int                             objCount;

        // ReSharper disable once NotAccessedField.Local
        private  readonly   string                          typeName;
        
        private  readonly   Dictionary <string, PropField>  strMap      = new Dictionary <string, PropField>(13);
        private  readonly   HashMapOpen<Bytes,  PropField>  fieldMap;
        
        private  readonly   Bytes                           removedKey;
        

        public PropertyFields (Type type, TypeStore typeStore, FieldFilter memberFilter = null)
        {
            memberFilter    = memberFilter ?? FieldFilter.DefaultMemberFilter;
            typeName        = type. ToString();
            var query       = new FieldQuery(typeStore, type, memberFilter);
            primCount       = query.primCount;
            objCount        = query.objCount;
            var fieldList   = query.fieldList;
            num             = fieldList. Count;
            removedKey      = new Bytes("__REMOVED", Untracked.Bytes);
            fieldMap        = new HashMapOpen<Bytes, PropField>(11, removedKey);
            
            fields          = new PropField [num];
            names32         = new Bytes32[num];
            
            for (int n = 0; n < num; n++) {
                fields[n] = fieldList[n];
                var field = fields[n];
                if (strMap.ContainsKey(field.name))
                    throw new InvalidOperationException("assert field is accessible via string lookup");
                strMap.Add(field.name, field);
                fieldMap.Put(ref field.nameBytes, field);
                names32[n].FromBytes(ref field.nameBytes);
            }
            fieldList. Clear();
        }
        
        public bool Contains(string fieldName) {
            return strMap.ContainsKey(fieldName);
        }
        
        public PropField GetField (ref Bytes fieldName) {
            // Note: its likely that hashcode ist not set properly. So calculate anyway
            fieldName.UpdateHashCode();
            PropField pf = fieldMap.Get(ref fieldName);
            return pf;
        }
        
        public PropField GetField (string fieldName) {
            strMap.TryGetValue(fieldName, out PropField field);
            return field;
        }
        
        public void Dispose() {
            for (int i = 0; i < fields.Length; i++)
                fields[i].Dispose();
            removedKey.Dispose(Untracked.Bytes);
        }
    }
}
