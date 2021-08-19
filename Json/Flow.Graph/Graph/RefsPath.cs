﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Friflo.Json.Flow.Graph.Internal;

namespace Friflo.Json.Flow.Graph
{
    public class RefsPath<TEntity, TRef, TKey>  where TEntity : class
                                                where TRef    : class 
    {
        public readonly string path;

        public override string ToString() => path;

        internal RefsPath(string path) {
            this.path = path;
        }
        
        public static RefsPath<TEntity, TRef, TKey> MemberRefs(Expression<Func<TEntity, IEnumerable<Ref<TRef, TKey>>>> selector)  {
            string selectorPath = ExpressionSelector.PathFromExpression(selector, out _);
            return new RefsPath<TEntity, TRef, TKey>(selectorPath);
        }
    }
    
    public class RefPath<TEntity, TRef, TKey> : RefsPath<TEntity, TRef, TKey>
                                            where TEntity : class
                                            where TRef    : class 
    {
        internal RefPath(string path) : base (path) { }
        
        public static RefPath<TEntity, TRef, TKey> MemberRef(Expression<Func<TEntity, Ref<TRef, TKey>>> selector) {
            string selectorPath = ExpressionSelector.PathFromExpression(selector, out _);
            return new RefPath<TEntity, TRef, TKey>(selectorPath);
        }
    }
}