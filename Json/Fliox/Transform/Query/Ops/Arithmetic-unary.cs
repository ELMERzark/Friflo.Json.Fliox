﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;

// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable MemberCanBeProtected.Global
namespace Friflo.Json.Fliox.Transform.Query.Ops
{
    // ------------------------------------ unary arithmetic operations ------------------------------------
    public abstract class UnaryArithmeticOp : Operation
    {
        [Required]  public              Operation   value;
        internal override               bool        IsNumeric() => true;

        protected UnaryArithmeticOp() { }

        protected UnaryArithmeticOp(Operation value) {
            this.value = value;
        }
        
        internal override void Init(OperationContext cx) {
            cx.ValidateReuse(this); // results are reused
            value.Init(cx);
        }
    }
    
    public sealed class Abs : UnaryArithmeticOp
    {
        public Abs() { }
        public Abs(Operation value) : base(value) { }

        public   override string    OperationName           => "Abs";
        public   override void      AppendLinq(AppendCx cx) => AppendLinqFunction("Abs", value, cx);
        
        internal override Scalar Eval(EvalCx cx) {
            var eval = value.Eval(cx);
            return eval.Abs(this);
        }
    }
    
    public sealed class Ceiling : UnaryArithmeticOp
    {
        public Ceiling() { }
        public Ceiling(Operation value) : base(value) { }

        public   override string    OperationName           => "Ceiling";
        public   override void      AppendLinq(AppendCx cx) => AppendLinqFunction("Ceiling", value, cx);
        
        internal override Scalar Eval(EvalCx cx) {
            var eval = value.Eval(cx);
            return eval.Ceiling(this);
        }
    }
    
    public sealed class Floor : UnaryArithmeticOp
    {
        public Floor() { }
        public Floor(Operation value) : base(value) { }

        public   override string    OperationName           => "Floor";
        public   override void      AppendLinq(AppendCx cx) => AppendLinqFunction("Floor", value, cx);
        
        internal override Scalar Eval(EvalCx cx) {
            var eval = value.Eval(cx);
            return eval.Floor(this);
        }
    }
    
    public sealed class Exp : UnaryArithmeticOp
    {
        public Exp() { }
        public Exp(Operation value) : base(value) { }

        public   override string    OperationName           => "Exp";
        public   override void      AppendLinq(AppendCx cx) => AppendLinqFunction("Exp", value, cx);
        
        internal override Scalar Eval(EvalCx cx) {
            var eval = value.Eval(cx);
            return eval.Exp(this);
        }
    }
    
    public sealed class Log : UnaryArithmeticOp
    {
        public Log() { }
        public Log(Operation value) : base(value) { }

        public   override string    OperationName           => "Log";
        public   override void      AppendLinq(AppendCx cx) => AppendLinqFunction("Log", value, cx);
        
        internal override Scalar Eval(EvalCx cx) {
            var eval = value.Eval(cx);
            return eval.Log(this);
        }
    }
    
    public sealed class Sqrt : UnaryArithmeticOp
    {
        public Sqrt() { }
        public Sqrt(Operation value) : base(value) { }

        public   override string    OperationName           => "Sqrt";
        public   override void      AppendLinq(AppendCx cx) => AppendLinqFunction("Sqrt", value, cx);
        
        internal override Scalar Eval(EvalCx cx) {
            var eval = value.Eval(cx);
            return eval.Sqrt(this);
        }
    }
    
    public sealed class Negate : UnaryArithmeticOp
    {
        public Negate() { }
        public Negate(Operation value) : base(value) { }

        public   override string    OperationName           => "-";
        public   override void      AppendLinq(AppendCx cx) { cx.Append("-("); value.AppendLinq(cx); cx.Append(")"); }

        internal override Scalar Eval(EvalCx cx) {
            var eval = value.Eval(cx);
            return Scalar.Zero.Subtract(eval, this);
        }
    }
}
