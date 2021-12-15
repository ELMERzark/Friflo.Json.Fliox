﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Mapper;

// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable MemberCanBeProtected.Global
namespace Friflo.Json.Fliox.Transform.Query.Ops
{
    // ------------------------------------ unary arithmetic operations ------------------------------------
    public abstract class UnaryArithmeticOp : Operation
    {
        [Fri.Required] public           Operation   value;
        [Fri.Ignore] internal  readonly EvalResult  evalResult = new EvalResult(new List<Scalar>());
        internal override               bool        IsNumeric => true;

        protected UnaryArithmeticOp() { }

        protected UnaryArithmeticOp(Operation value) {
            this.value = value;
        }
        
        internal override void Init(OperationContext cx, InitFlags flags) {
            cx.ValidateReuse(this); // results are reused
            value.Init(cx, 0);
        }
    }
    
    public sealed class Abs : UnaryArithmeticOp
    {
        public Abs() { }
        public Abs(Operation value) : base(value) { }

        public   override void AppendLinq(AppendCx cx) => AppendLinqFunction("Abs", value, cx);
        
        internal override EvalResult Eval(EvalCx cx) {
            evalResult.Clear();
            var eval = value.Eval(cx);
            foreach (var val in eval.values) {
                var result = val.Abs();
                evalResult.Add(result);
            }
            return evalResult;
        }
    }
    
    public sealed class Ceiling : UnaryArithmeticOp
    {
        public Ceiling() { }
        public Ceiling(Operation value) : base(value) { }

        public   override void AppendLinq(AppendCx cx) => AppendLinqFunction("Ceiling", value, cx);
        
        internal override EvalResult Eval(EvalCx cx) {
            evalResult.Clear();
            var eval = value.Eval(cx);
            foreach (var val in eval.values) {
                var result = val.Ceiling();
                evalResult.Add(result);
            }
            return evalResult;
        }
    }
    
    public sealed class Floor : UnaryArithmeticOp
    {
        public Floor() { }
        public Floor(Operation value) : base(value) { }

        public   override void AppendLinq(AppendCx cx) => AppendLinqFunction("Floor", value, cx);
        
        internal override EvalResult Eval(EvalCx cx) {
            evalResult.Clear();
            var eval = value.Eval(cx);
            foreach (var val in eval.values) {
                var result = val.Floor();
                evalResult.Add(result);
            }
            return evalResult;
        }
    }
    
    public sealed class Exp : UnaryArithmeticOp
    {
        public Exp() { }
        public Exp(Operation value) : base(value) { }

        public   override void AppendLinq(AppendCx cx) => AppendLinqFunction("Exp", value, cx);
        
        internal override EvalResult Eval(EvalCx cx) {
            evalResult.Clear();
            var eval = value.Eval(cx);
            foreach (var val in eval.values) {
                var result = val.Exp();
                evalResult.Add(result);
            }
            return evalResult;
        }
    }
    
    public sealed class Log : UnaryArithmeticOp
    {
        public Log() { }
        public Log(Operation value) : base(value) { }

        public   override void AppendLinq(AppendCx cx) => AppendLinqFunction("Log", value, cx);
        
        internal override EvalResult Eval(EvalCx cx) {
            evalResult.Clear();
            var eval = value.Eval(cx);
            foreach (var val in eval.values) {
                var result = val.Log();
                evalResult.Add(result);
            }
            return evalResult;
        }
    }
    
    public sealed class Sqrt : UnaryArithmeticOp
    {
        public Sqrt() { }
        public Sqrt(Operation value) : base(value) { }

        public   override void AppendLinq(AppendCx cx) => AppendLinqFunction("Sqrt", value, cx);
        
        internal override EvalResult Eval(EvalCx cx) {
            evalResult.Clear();
            var eval = value.Eval(cx);
            foreach (var val in eval.values) {
                var result = val.Sqrt();
                evalResult.Add(result);
            }
            return evalResult;
        }
    }
    
    public sealed class Negate : UnaryArithmeticOp
    {
        public Negate() { }
        public Negate(Operation value) : base(value) { }

        public   override void AppendLinq(AppendCx cx) { cx.Append("-("); value.AppendLinq(cx); cx.Append(")"); }

        internal override EvalResult Eval(EvalCx cx) {
            var zero = new Scalar(0);
            evalResult.Clear();
            var eval = value.Eval(cx);
            foreach (var val in eval.values) {
                var result = zero.Subtract(val);
                evalResult.Add(result);
            }
            return evalResult;
        }
    }
}
