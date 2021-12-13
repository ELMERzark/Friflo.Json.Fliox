﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Text;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Transform.Query.Arity;

// ReSharper disable FieldCanBeMadeReadOnly.Global
namespace Friflo.Json.Fliox.Transform.Query.Ops
{
    // -------------------------------------- comparison operations --------------------------------------
    public abstract class BinaryBoolOp : FilterOperation
    {
        [Fri.Required]  public  Operation   left;
        [Fri.Required]  public  Operation   right;
        
        protected BinaryBoolOp() { }
        protected BinaryBoolOp(Operation left, Operation right) {
            this.left = left;
            this.right = right;
        }
        
        internal override void Init(OperationContext cx, InitFlags flags) {
            cx.ValidateReuse(this); // results are reused
            left.Init(cx, 0);
            right.Init(cx, 0);
        }
    }
    
    // --- associative comparison operations ---
    public sealed class Equal : BinaryBoolOp
    {
        public Equal() { }
        public Equal(Operation left, Operation right) : base(left, right) { }

        public   override void AppendLinq(AppendCx cx) => AppendLinqBinary(cx, "==", left, right);
        
        internal override EvalResult Eval(EvalCx cx) {
            evalResult.Clear();
            var eval = new BinaryResult(left.Eval(cx), right.Eval(cx));
            foreach (var pair in eval) {
                if (pair.BothOperandsDefined(out Scalar result, left, right))
                    result = pair.left.CompareTo(pair.right) == 0 ? True : False;
                evalResult.Add(result);
            }
            return evalResult;
        }
    }
    
    public sealed class NotEqual : BinaryBoolOp
    {
        public NotEqual() { }
        public NotEqual(Operation left, Operation right) : base(left, right) { }

        public   override void AppendLinq(AppendCx cx) => AppendLinqBinary(cx, "!=", left, right);
        
        internal override EvalResult Eval(EvalCx cx) {
            evalResult.Clear();
            var eval = new BinaryResult(left.Eval(cx), right.Eval(cx));
            foreach (var pair in eval) {
                if (pair.BothOperandsDefined(out Scalar result, left, right))
                    result = pair.left.CompareTo(pair.right) != 0 ? True : False;
                evalResult.Add(result);
            }
            return evalResult;
        }
    }

    // --- non-associative comparison operations -> call Order() --- 
    public sealed class LessThan : BinaryBoolOp
    {
        public LessThan() { }
        public LessThan(Operation left, Operation right) : base(left, right) { }
        
        public   override void AppendLinq(AppendCx cx) => AppendLinqBinary(cx, "<", left, right);
        
        internal override EvalResult Eval(EvalCx cx) {
            evalResult.Clear();
            var eval = new BinaryResult(left.Eval(cx), right.Eval(cx));
            foreach (var pair in eval) {
                if (pair.BothOperandsDefined(out Scalar result, left, right))
                    result = pair.left.CompareTo(pair.right) < 0 ? True : False;
                evalResult.Add(result);
            }
            return evalResult;
        }
    }
    
    public sealed class LessThanOrEqual : BinaryBoolOp
    {
        public LessThanOrEqual() { }
        public LessThanOrEqual(Operation left, Operation right) : base(left, right) { }
        
        public   override void AppendLinq(AppendCx cx) => AppendLinqBinary(cx, "<=", left, right);
        
        internal override EvalResult Eval(EvalCx cx) {
            evalResult.Clear();
            var eval = new BinaryResult(left.Eval(cx), right.Eval(cx));
            foreach (var pair in eval) {
                if (pair.BothOperandsDefined(out Scalar result, left, right))
                    result = pair.left.CompareTo(pair.right) <= 0 ? True : False;
                evalResult.Add(result);
            }
            return evalResult;
        }
    }
    
    public sealed class GreaterThan : BinaryBoolOp
    {
        public GreaterThan() { }
        public GreaterThan(Operation left, Operation right) : base(left, right) { }
        
        public   override void AppendLinq(AppendCx cx) => AppendLinqBinary(cx, ">", left, right);
        
        internal override EvalResult Eval(EvalCx cx) {
            evalResult.Clear();
            var eval = new BinaryResult(left.Eval(cx), right.Eval(cx));
            foreach (var pair in eval) {
                if (pair.BothOperandsDefined(out Scalar result, left, right))
                    result = pair.left.CompareTo(pair.right) > 0 ? True : False;
                evalResult.Add(result);
            }
            return evalResult;
        }
    }
    
    public sealed class GreaterThanOrEqual : BinaryBoolOp
    {
        public GreaterThanOrEqual() { }
        public GreaterThanOrEqual(Operation left, Operation right) : base(left, right) { }
        
        public   override void AppendLinq(AppendCx cx) => AppendLinqBinary(cx, ">=", left, right);
        
        internal override EvalResult Eval(EvalCx cx) {
            evalResult.Clear();
            var eval = new BinaryResult(left.Eval(cx), right.Eval(cx));
            foreach (var pair in eval) {
                if (pair.BothOperandsDefined(out Scalar result, left, right))
                    result = pair.left.CompareTo(pair.right) >= 0 ? True : False;
                evalResult.Add(result);
            }
            return evalResult;
        }
    }
}