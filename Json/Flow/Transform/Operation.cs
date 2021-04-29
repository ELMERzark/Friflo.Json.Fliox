﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Transform.Query;
using Friflo.Json.Flow.Transform.Query.Ops;

namespace Friflo.Json.Flow.Transform
{
    [Fri.Discriminator("op")]
    //
    [Fri.Polymorph(typeof(Field),               Discriminant = "field")]
    //  
    [Fri.Polymorph(typeof(StringLiteral),       Discriminant = "string")]
    [Fri.Polymorph(typeof(DoubleLiteral),       Discriminant = "double")]
    [Fri.Polymorph(typeof(LongLiteral),         Discriminant = "int64")]
    [Fri.Polymorph(typeof(NullLiteral),         Discriminant = "null")]
    //  
    [Fri.Polymorph(typeof(Abs),                 Discriminant = "abs")]
    [Fri.Polymorph(typeof(Ceiling),             Discriminant = "ceiling")]
    [Fri.Polymorph(typeof(Floor),               Discriminant = "floor")]
    [Fri.Polymorph(typeof(Exp),                 Discriminant = "exp")]
    [Fri.Polymorph(typeof(Log),                 Discriminant = "log")]
    [Fri.Polymorph(typeof(Sqrt),                Discriminant = "sqrt")]
    [Fri.Polymorph(typeof(Negate),              Discriminant = "negate")]
    //  
    [Fri.Polymorph(typeof(Add),                 Discriminant = "add")]
    [Fri.Polymorph(typeof(Subtract),            Discriminant = "subtract")]
    [Fri.Polymorph(typeof(Multiply),            Discriminant = "multiply")]
    [Fri.Polymorph(typeof(Divide),              Discriminant = "divide")]
    //  
    [Fri.Polymorph(typeof(Min),                 Discriminant = "min")]
    [Fri.Polymorph(typeof(Max),                 Discriminant = "max")]
    [Fri.Polymorph(typeof(Sum),                 Discriminant = "sum")]
    [Fri.Polymorph(typeof(Average),             Discriminant = "average")]
    [Fri.Polymorph(typeof(Count),               Discriminant = "count")]
    
    // --- FilterOperation  
    [Fri.Polymorph(typeof(Equal),               Discriminant = "equal")]
    [Fri.Polymorph(typeof(NotEqual),            Discriminant = "notEqual")]
    [Fri.Polymorph(typeof(LessThan),            Discriminant = "lessThan")]
    [Fri.Polymorph(typeof(LessThanOrEqual),     Discriminant = "lessThanOrEqual")]
    [Fri.Polymorph(typeof(GreaterThan),         Discriminant = "greaterThan")]
    [Fri.Polymorph(typeof(GreaterThanOrEqual),  Discriminant = "greaterThanOrEqual")]
    //
    [Fri.Polymorph(typeof(And),                 Discriminant = "and")]
    [Fri.Polymorph(typeof(Or),                  Discriminant = "or")]
    //
    [Fri.Polymorph(typeof(TrueLiteral),         Discriminant = "true")]
    [Fri.Polymorph(typeof(FalseLiteral),        Discriminant = "false")]
    [Fri.Polymorph(typeof(Not),                 Discriminant = "not")]
    [Fri.Polymorph(typeof(Any),                 Discriminant = "any")]
    [Fri.Polymorph(typeof(All),                 Discriminant = "all")]
    //
    [Fri.Polymorph(typeof(Contains),            Discriminant = "contains")]
    [Fri.Polymorph(typeof(StartsWith),          Discriminant = "startsWith")]
    [Fri.Polymorph(typeof(EndsWith),            Discriminant = "endsWith")]
    
    // ----------------------------- Operation --------------------------
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public abstract class Operation
    {
        public   abstract   string      Linq { get; }
        internal abstract   void        Init (OperationContext cx, InitFlags flags);
        internal abstract   EvalResult  Eval (EvalCx cx);

        public   override   string      ToString() => Linq; 

        internal static readonly Scalar         True  = Scalar.True; 
        internal static readonly Scalar         False = Scalar.False;
        internal static readonly Scalar         Null  = Scalar.Null;

        internal static readonly EvalResult     SingleTrue  = new EvalResult(True);
        internal static readonly EvalResult     SingleFalse = new EvalResult(False);
        
        public   static readonly TrueLiteral    FilterTrue  = new TrueLiteral();
        public   static readonly FalseLiteral   FilterFalse = new FalseLiteral();

       
        public JsonLambda Lambda() {
            return new JsonLambda(this);
        }
        
        public static Operation FromLambda<T>(Expression<Func<T, object>> lambda) {
            return QueryConverter.OperationFromExpression(lambda);
        }
        
        public static FilterOperation FromFilter<T>(Expression<Func<T, bool>> filter) {
            return (FilterOperation)QueryConverter.OperationFromExpression(filter);
        }
    }
    
    [Fri.Discriminator("op")]
    // --- FilterOperation  
    [Fri.Polymorph(typeof(Equal),               Discriminant = "equal")]
    [Fri.Polymorph(typeof(NotEqual),            Discriminant = "notEqual")]
    [Fri.Polymorph(typeof(LessThan),            Discriminant = "lessThan")]
    [Fri.Polymorph(typeof(LessThanOrEqual),     Discriminant = "lessThanOrEqual")]
    [Fri.Polymorph(typeof(GreaterThan),         Discriminant = "greaterThan")]
    [Fri.Polymorph(typeof(GreaterThanOrEqual),  Discriminant = "greaterThanOrEqual")]
    //
    [Fri.Polymorph(typeof(And),                 Discriminant = "and")]
    [Fri.Polymorph(typeof(Or),                  Discriminant = "or")]
    //
    [Fri.Polymorph(typeof(TrueLiteral),         Discriminant = "true")]
    [Fri.Polymorph(typeof(FalseLiteral),        Discriminant = "false")]
    [Fri.Polymorph(typeof(Not),                 Discriminant = "not")]
    [Fri.Polymorph(typeof(Any),                 Discriminant = "any")]
    [Fri.Polymorph(typeof(All),                 Discriminant = "all")]
    //
    [Fri.Polymorph(typeof(Contains),            Discriminant = "contains")]
    [Fri.Polymorph(typeof(StartsWith),          Discriminant = "startsWith")]
    [Fri.Polymorph(typeof(EndsWith),            Discriminant = "endsWith")]
    
    // ----------------------------- FilterOperation --------------------------
    public abstract class FilterOperation : Operation
    {
        [Fri.Ignore]
        internal readonly  EvalResult   evalResult = new EvalResult(new List<Scalar>());

        public JsonFilter Filter() {
            return new JsonFilter(this);
        }
    }
}
