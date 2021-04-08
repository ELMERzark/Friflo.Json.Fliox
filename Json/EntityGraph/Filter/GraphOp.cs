﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Mapper.Graph;

namespace Friflo.Json.EntityGraph.Filter
{
    
    internal class GraphOpContext
    {
        internal readonly Dictionary<string, Field> selectors = new Dictionary<string, Field>();
    }

    public abstract class GraphOp
    {
        internal virtual void Init(GraphOpContext cx) { }

        internal virtual List<SelectorValue> Eval() {
            throw new NotImplementedException($"Eval() not implemented for: {GetType().Name}");
        }
        
        internal static readonly SelectorValue True  = new SelectorValue(true); 
        internal static readonly SelectorValue False = new SelectorValue(false);
        
        internal static readonly List<SelectorValue> SingleTrue  = new List<SelectorValue>{ True  };
        internal static readonly List<SelectorValue> SingleFalse = new List<SelectorValue>{ False };
    }
    
    // -------------------- unary operators --------------------
    
    // op: Field, String-/Number Literal, Not
    
    public class Field : GraphOp
    {
        public          string                  field;
        public          List<SelectorValue>     values = new List<SelectorValue>();

        public override string                  ToString() => field;

        internal override void Init(GraphOpContext cx) {
            cx.selectors.TryAdd(field, this);
        }

        internal override List<SelectorValue> Eval() { return values; }
    }
    
    public class StringLiteral : GraphOp
    {
        public              string      value;
        
        public override     string      ToString() => value;
        
        internal override List<SelectorValue> Eval() {
            var result = new List<SelectorValue> { new SelectorValue(value) };
            return result;
        }
    }
    
    public class NumberLiteral : GraphOp
    {
        public double       doubleValue;  // or long
    }
    
    public class BooleanLiteral : GraphOp
    {
        public bool         value;
    }

    public class NullLiteral : GraphOp
    {
    }
    
    public class NotOp : GraphOp
    {
        public BoolOp       lambda;
    }

    
    // -------------------- binary operators --------------------

    // op: Equals, NotEquals, Add, Subtract, Multiply, Divide, Remainder, Min, Max, ...
    //     All, Any, Count, Min, Max, ...

    public abstract class BoolOp : GraphOp {
    }
    
    public class Equals : BoolOp
    {
        public GraphOp      left;
        public GraphOp      right;
        
        internal override void Init(GraphOpContext cx) {
            left.Init(cx);
            right.Init(cx);
        }

        internal override List<SelectorValue> Eval() {
            var leftResult  = left.Eval();
            var rightResult = right.Eval();
            var opResult = new OpResult(leftResult, rightResult);
            foreach (var value in opResult.values) {
                if (opResult.value.CompareTo(value) == 0)
                    return SingleTrue;
            }
            return SingleFalse;
        }
    }
    
    internal readonly struct  OpResult
    {
        internal readonly SelectorValue          value;
        internal readonly List<SelectorValue>    values;

        internal OpResult(List<SelectorValue> left, List<SelectorValue> right) {
            if (left.Count == 1) {
                value   = left[0];
                values  = right;
                return;
            }
            if (right.Count == 1) {
                value   = right[0];
                values  = left;
                return;
            }
            throw new InvalidOperationException("Expect at least an operation result with one element");
        }
    }
    
    public class LessThan : BoolOp
    {
        public GraphOp      left;
        public GraphOp      right;
    }
    
    public class GreaterThan : BoolOp
    {
        public GraphOp      left;
        public GraphOp      right;
    }

    public class Any : BoolOp
    {
        public BoolOp       lambda;     // e.g.   i => i.amount < 1
        
        internal override void Init(GraphOpContext cx) {
            lambda.Init(cx);
        }
        
        internal override List<SelectorValue> Eval() {
            var evalResult = lambda.Eval();
            foreach (var result in evalResult) {
                if (result.CompareTo(True) == 0)
                    return SingleTrue;
            }
            return SingleFalse;
        }
    }
    
    public class All : BoolOp
    {
        public BoolOp       lambda;     // e.g.   i => i.amount < 1
        
        internal override void Init(GraphOpContext cx) {
            lambda.Init(cx);
        }
        
        internal override List<SelectorValue> Eval() {
            var evalResult = lambda.Eval();
            foreach (var result in evalResult) {
                if (result.CompareTo(True) != 0)
                    return SingleFalse;
            }
            return SingleTrue;
        }
    }
}
