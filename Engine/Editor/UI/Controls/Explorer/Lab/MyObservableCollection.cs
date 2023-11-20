﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.ObjectModel;
using System.Text;

namespace Friflo.Fliox.Editor.UI.Controls.Explorer.Lab;

/// <summary>
/// Sample assigning an event handler to <see cref="ObservableCollection{T}.PropertyChanged"/> 
/// </summary>
public class MyObservableCollection<T> : ObservableCollection<T>
{
    public void AddPropertyChangedHandler()
    {
        PropertyChanged += (_, args) => {
            var sb = new StringBuilder();
            sb.Append("PropertyChanged: ");
            var name = args.PropertyName;
            sb.Append(name);
            sb.Append(" = ");
            switch (name) {
                case "Count":   sb.Append(Count); break;
                case "Item[]":  sb.Append(Count); break;
            }
            Console.WriteLine(sb.ToString());
        }; 
    }
}