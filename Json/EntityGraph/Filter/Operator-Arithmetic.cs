﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System.Collections.Generic;
using Friflo.Json.EntityGraph.Filter.Arity;
using Friflo.Json.Mapper.Graph;

namespace Friflo.Json.EntityGraph.Filter
{
    // ------------------------------------ unary arithmetic operators ------------------------------------
    public abstract class UnaryArithmeticOp : BoolOp
    {
        protected           Operator            operand;
        protected readonly  List<SelectorValue> results = new List<SelectorValue>();

        protected UnaryArithmeticOp(Operator operand) { this.operand = operand; }
        
        internal override void Init(GraphOpContext cx) {
            cx.ValidateReuse(this); // results are reused
            operand.Init(cx);
        }
    }
    
    
    
    
    // ------------------------------------ binary arithmetic operators ------------------------------------
    public abstract class BinaryArithmeticOp : Operator
    {
        protected           Operator            left;
        protected           Operator            right;
        protected readonly  List<SelectorValue> results = new List<SelectorValue>();
        
        protected BinaryArithmeticOp(Operator left, Operator right) {
            this.left = left;
            this.right = right;
        }
        
        internal override void Init(GraphOpContext cx) {
            cx.ValidateReuse(this); // results are reused
            left.Init(cx);
            right.Init(cx);
        }
    }
    
    public class Add : BinaryArithmeticOp
    {
        public Add(Operator left, Operator right) : base(left, right) { }

        public override     string      ToString() => $"{left} + {right}";
        
        internal override List<SelectorValue> Eval() {
            results.Clear();
            var eval = new BinaryResult(left.Eval(), right.Eval());
            foreach (var pair in eval) {
                var result = pair.left.Add(pair.right);
                results.Add(result);
            }
            return results;
        }
    }
    
    public class Subtract : BinaryArithmeticOp
    {
        public Subtract(Operator left, Operator right) : base(left, right) { }

        public override     string      ToString() => $"{left} - {right}";
        
        internal override List<SelectorValue> Eval() {
            results.Clear();
            var eval = new BinaryResult(left.Eval(), right.Eval());
            foreach (var pair in eval) {
                var result = pair.left.Subtract(pair.right);
                results.Add(result);
            }
            return results;
        }
    }
    
    public class Multiply : BinaryArithmeticOp
    {
        public Multiply(Operator left, Operator right) : base(left, right) { }

        public override     string      ToString() => $"{left} * {right}";
        
        internal override List<SelectorValue> Eval() {
            results.Clear();
            var eval = new BinaryResult(left.Eval(), right.Eval());
            foreach (var pair in eval) {
                var result = pair.left.Multiply(pair.right);
                results.Add(result);
            }
            return results;
        }
    }
    
    public class Divide : BinaryArithmeticOp
    {
        public Divide(Operator left, Operator right) : base(left, right) { }

        public override     string      ToString() => $"{left} / {right}";
        
        internal override List<SelectorValue> Eval() {
            results.Clear();
            var eval = new BinaryResult(left.Eval(), right.Eval());
            foreach (var pair in eval) {
                var result = pair.left.Divide(pair.right);
                results.Add(result);
            }
            return results;
        }
    }

}