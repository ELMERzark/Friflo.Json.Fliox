// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Host.Utils;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Schema.Native;
using Friflo.Json.Fliox.Schema.Validation;

// ReSharper disable ConvertToAutoProperty
namespace Friflo.Json.Fliox.Hub.Host
{
    /// <summary>
    /// <see cref="SharedEnv"/> provide a set of shared resources.
    /// In particular a <see cref="TypeStore"/> and a <see cref="Pool"/>.
    /// The resources contained by a <see cref="SharedEnv"/> are designed for being reused to avoid expensive
    /// heap allocations when required.
    /// The intention is to use only a single <see cref="SharedEnv"/> instance within the whole application.
    /// <br/>
    /// <see cref="SharedEnv"/> references are passed as a parameter to every <see cref="FlioxHub"/> constructor.
    /// If null it defaults to the <see cref="Default"/> <see cref="SharedEnv"/> instance.
    /// If an application needs to control the lifecycle of all shared resources it needs to create its own
    /// <see cref="SharedEnv()"/> instance and pass it to the constructor to all <see cref="FlioxHub"/> instances it creates.
    /// <br/>
    /// Access to shared resources is thread safe.
    /// </summary>
    public class SharedEnv : IDisposable
    {
        private  readonly   TypeStore       typeStore;
        internal readonly   SharedCache     sharedCache;
        public   virtual    TypeStore       TypeStore   => typeStore;
        public              Pool            Pool        { get; }
        
        public static readonly SharedEnv Default = new DefaultSharedEnv();


        public SharedEnv() {
            typeStore       = new TypeStore();
            Pool            = new Pool(this);
            sharedCache     = new SharedCache();
        }
        
        public SharedEnv(TypeStore typeStore) {
            this.typeStore  = typeStore;
            Pool            = new Pool(this);
            sharedCache     = new SharedCache();
        }

        public virtual void Dispose () {
            Pool.Dispose();
            TypeStore.Dispose();
        }
    }
    
    internal sealed class DefaultSharedEnv : SharedEnv
    {
        public  override    TypeStore   TypeStore   => SharedTypeStore.Get();

        internal DefaultSharedEnv() : base (null) { }
        
        public override void Dispose () {
            Pool.Dispose();
        }
    }
    
    public class SharedCache
    {
        private readonly    Dictionary<Type, ValidationType> validationTypes = new Dictionary<Type, ValidationType>();
        
        /// <summary> Return an immutable <see cref="ValidationSet"/> for the given <param name="type"></param></summary>
        public ValidationType GetValidationType(Type type) {
            if (!validationTypes.TryGetValue(type, out var validationType)) {
                using (var nativeSchema = new NativeTypeSchema(type)) {
                    var validationSet   = new ValidationSet(nativeSchema);
                    var typeDef         = nativeSchema.TypeAsTypeDef(type);
                    validationType      = validationSet.TypeDefAsValidationType(typeDef);
                    validationTypes.Add(type, validationType);
                }
            }
            return validationType;
        }
    }
}