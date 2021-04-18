﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Friflo.Json.Burst;  // UnityExtension.TryAdd()
using Friflo.Json.Flow.Mapper; 

// ReSharper disable ConvertToAutoProperty
namespace Friflo.Json.Flow.Graph.Query
{
    [Fri.Discriminator("op")]
    //
    [Fri.Polymorph(typeof(Field),               Discriminant = "field")]
    //  
    [Fri.Polymorph(typeof(StringLiteral),       Discriminant = "string")]
    [Fri.Polymorph(typeof(DoubleLiteral),       Discriminant = "double")]
    [Fri.Polymorph(typeof(LongLiteral),         Discriminant = "int64")]
    [Fri.Polymorph(typeof(BoolLiteral),         Discriminant = "bool")]
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
    
    // --- BoolOp  
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
    [Fri.Polymorph(typeof(Not),                 Discriminant = "not")]
    [Fri.Polymorph(typeof(Any),                 Discriminant = "any")]
    [Fri.Polymorph(typeof(All),                 Discriminant = "all")]
    //
    [Fri.Polymorph(typeof(Contains),            Discriminant = "contains")]
    [Fri.Polymorph(typeof(StartsWith),          Discriminant = "startsWith")]
    [Fri.Polymorph(typeof(EndsWith),            Discriminant = "endsWith")]
    
    // ----------------------------- Operation --------------------------
    public abstract class Operation
    {
        internal abstract void Init(OperationContext cx, InitFlags flags);
        internal abstract EvalResult            Eval(EvalCx cx);
        
        internal static readonly Scalar         True  = Scalar.True; 
        internal static readonly Scalar         False = Scalar.False;
        internal static readonly Scalar         Null  = Scalar.Null;

        internal static readonly EvalResult     SingleTrue  = new EvalResult(True);
        internal static readonly EvalResult     SingleFalse = new EvalResult(False);

        public JsonLambda Lambda() {
            return new JsonLambda(this);
        }
        
        public static Operation FromLambda<T>(Expression<Func<T, object>> lambda) {
            return QueryConverter.OperationFromExpression(lambda);
        }
        
        public static BoolOp FromFilter<T>(Expression<Func<T, bool>> filter) {
            return (BoolOp)QueryConverter.OperationFromExpression(filter);
        }
    }

    internal readonly struct EvalCx
    {
        private readonly    int     groupIndex;

        public              int     GroupIndex => groupIndex;
        
        internal EvalCx(int groupIndex) {
            this.groupIndex = groupIndex;
        }
    }

    [Flags]
    internal enum InitFlags
    {
        ArrayField = 1
    }

    internal class OperationContext
    {
        internal readonly List<Field>                   selectors = new List<Field>();
        private  readonly HashSet<Operation>            operations = new HashSet<Operation>();
        internal readonly Dictionary<string, Field>     parameters = new Dictionary<string, Field>();

        internal void Init() {
            selectors.Clear();
            operations.Clear();
            parameters.Clear();
        }

        internal void ValidateReuse(Operation op) {
            if (operations.Add(op))
                return;
            var msg = $"Used operation instance is not applicable for reuse. Use a clone. Type: {op.GetType().Name}, instance: {op}";
            throw new InvalidOperationException(msg);
        }
    }
    
    // ------------------------------------- unary operations -------------------------------------
    public class Field : Operation
    {
        public          string                  name;
        
        [Fri.Ignore]
        internal        string                  selector;   // == field if field starts with . otherwise appended to a lambda parameter
        [Fri.Ignore]
        internal        EvalResult              evalResult;

        public override string                  ToString() => name;

        public Field() { }
        public Field(string name) { this.name = name; }

        internal override void Init(OperationContext cx, InitFlags flags) {
            bool isArrayField = (flags & InitFlags.ArrayField) != 0;
            if (name.StartsWith(".")) {
                selector = isArrayField ? name + "[=>]" : name;
            } else {
                var dotPos = name.IndexOf('.');
                if (dotPos == -1)
                    throw new InvalidOperationException("expect a dot in field name");
                var parameter = name.Substring(0, dotPos);
                var lambda = cx.parameters[parameter];
                var path = name.Substring(dotPos + 1);
                selector = lambda.name + "[=>]." + path;
            }
            cx.selectors.Add(this);
        }

        internal override EvalResult Eval(EvalCx cx) {
            int groupIndex = cx.GroupIndex;
            if (groupIndex == -1)
                return evalResult;
            
            var groupIndices = evalResult.groupIndices;
            int startIndex = groupIndices[groupIndex];
            int endIndex;
            if (groupIndex + 1 < groupIndices.Count) {
                endIndex = groupIndices[groupIndex + 1];
            } else {
                endIndex = evalResult.values.Count;
            }
            evalResult.SetRange(startIndex, endIndex);
            return evalResult;
        }
    }
}
