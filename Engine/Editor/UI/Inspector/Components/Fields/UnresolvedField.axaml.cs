﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Avalonia.Controls;
using Friflo.Fliox.Engine.ECS;

// ReSharper disable once CheckNamespace
namespace Friflo.Fliox.Editor.UI.Inspector;

public partial class UnresolvedField : UserControl, IFieldControl
{
    private Unresolved      unresolved;
    public  ComponentField  ComponentField { get; init; }
    
    
    internal void Set(Unresolved unresolved)
    {
        this.unresolved     = unresolved;
        var tags            = unresolved.tags;
        var components      = unresolved.components;
        var tagItems        = Tags.Items;
        var componentItems = Components.Items;
        tagItems.Clear();
        componentItems.Clear();

        if (tags != null) {
            foreach (var tag in tags) {
                tagItems.Add(new ListBoxItem { Content = tag });
            }
        }
        if (components != null) {
            foreach (var component in components) {
                tagItems.Add(new ListBoxItem { Content = component.key });
            }
        }
    }

    public UnresolvedField()
    {
        InitializeComponent();
    }
}