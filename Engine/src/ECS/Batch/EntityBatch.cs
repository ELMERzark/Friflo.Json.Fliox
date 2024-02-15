﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Text;
using static System.Diagnostics.DebuggerBrowsableState;
using Browse = System.Diagnostics.DebuggerBrowsableAttribute;

// ReSharper disable RedundantTypeDeclarationBody
// ReSharper disable once CheckNamespace
namespace Friflo.Engine.ECS;

internal class BatchComponent { }

internal class BatchComponent<T> : BatchComponent where T : struct, IComponent
{
    internal    T   value;
}


internal sealed class  EntityBatch
{
#region public properties
    public              int     CommandCount    => GetCommandCount();
    public   override   string  ToString()      => GetString();
    #endregion

#region internal fields
    [Browse(Never)] internal            BatchComponent[]    batchComponents;    //  8
    [Browse(Never)] private  readonly   ComponentType[]     componentTypes;     //  8
    [Browse(Never)] private  readonly   EntityStoreBase     store;              //  8   - used only for Entity.Batch
    [Browse(Never)] internal            int                 entityId;           //  4   - used only for Entity.Batch
    [Browse(Never)] internal            Tags                addTags;            // 32
    [Browse(Never)] internal            Tags                removeTags;         // 32
    [Browse(Never)] internal            ComponentTypes      addComponents;      // 32
    [Browse(Never)] internal            ComponentTypes      removeComponents;   // 32
    #endregion
    
#region internal methods
    public EntityBatch()
    {
        var schema          = EntityStoreBase.Static.EntitySchema;
        componentTypes      = schema.components;
    }
    
    internal EntityBatch(EntityStoreBase store) : this()
    {
        this.store = store;
    }
    
    public void Clear()
    {
        entityId            = 0;
        addTags             = default;
        removeTags          = default;
        addComponents       = default;
        removeComponents    = default;
    }
    
    private int GetCommandCount()
    {
        return addComponents    .Count +
               removeComponents .Count +
               addTags          .Count +
               removeTags       .Count;
    }
    
    private string GetString()
    {
        var hasAdds     = addComponents.Count    > 0 || addTags.Count    > 0;
        var hasRemoves  = removeComponents.Count > 0 || removeTags.Count > 0;
        if (!hasAdds && !hasRemoves) {
            return "empty";
        }
        var sb = new StringBuilder();
        if (hasAdds) {
            sb.Append("add: [");
            foreach (var component in addComponents) {
                sb.Append(component.Name);
                sb.Append(", ");
            }
            foreach (var tag in addTags) {
                sb.Append('#');
                sb.Append(tag.Name);
                sb.Append(", ");
            }
            sb.Length -= 2;
            sb.Append("]  ");
        }
        if (hasRemoves) {
            sb.Append("remove: [");
            foreach (var component in removeComponents) {
                sb.Append(component.Name);
                sb.Append(", ");
            }
            foreach (var tag in removeTags) {
                sb.Append('#');
                sb.Append(tag.Name);
                sb.Append(", ");
            }
            sb.Length -= 2;
            sb.Append(']');
        }
        return sb.ToString();
    }
    #endregion
    
#region commands
    public void Apply()
    {
        if (entityId == 0) throw new InvalidOperationException("Apply() can only be used on Entity.Batch. Use ApplyTo()");
        try {
            store.ApplyBatchTo(this, entityId);
        }
        finally {
            Clear();
        }
    }
    
    public void ApplyTo(Entity entity)
    {
        entity.store.ApplyBatchTo(this, entity.Id);
    }
    
    public EntityBatch AddComponent<T>(T component) where T : struct, IComponent
    {
        var structIndex = StructHeap<T>.StructIndex;
        addComponents.      bitSet.SetBit   (structIndex);
        removeComponents.   bitSet.ClearBit (structIndex);
        var components      = batchComponents           ??= CreateBatchComponents();
        var batchComponent  = components[structIndex]   ??= CreateBatchComponent(structIndex);
        ((BatchComponent<T>)batchComponent).value = component;
        return this;   
    }
    
    private static BatchComponent[] CreateBatchComponents() {
        var maxStructIndex = EntityStoreBase.Static.EntitySchema.maxStructIndex;
        return new BatchComponent[maxStructIndex];
    }
    
    private BatchComponent CreateBatchComponent(int structIndex) {
        return componentTypes[structIndex].CreateBatchComponent();
    }
    
    public EntityBatch RemoveComponent<T>() where T : struct, IComponent
    {
        var structIndex = StructHeap<T>.StructIndex;
        removeComponents.   bitSet.SetBit   (structIndex);
        addComponents.      bitSet.ClearBit (structIndex);
        return this;   
    }
    
    public EntityBatch AddTag<T>() where T : struct, ITag
    {
        var tagIndex = TagType<T>.TagIndex;
        addTags.    bitSet.SetBit   (tagIndex);
        removeTags. bitSet.ClearBit (tagIndex);
        return this;
    }
    
    public EntityBatch AddTags(in Tags tags)
    {
        addTags.    Add     (tags);
        removeTags. Remove  (tags);
        return this;
    }
    
    public EntityBatch RemoveTag<T>() where T : struct, ITag
    {
        var tagIndex = TagType<T>.TagIndex;
        removeTags. bitSet.SetBit   (tagIndex);
        addTags.    bitSet.ClearBit (tagIndex);
        return this;
    }
    
    public EntityBatch RemoveTags(in Tags tags)
    {
        addTags.    Remove  (tags);
        removeTags. Add     (tags);
        return this;
    }
    #endregion
}
