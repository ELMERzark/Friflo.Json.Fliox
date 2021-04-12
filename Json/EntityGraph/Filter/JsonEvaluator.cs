﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Linq.Expressions;
using Friflo.Json.Mapper.Graph;

namespace Friflo.Json.EntityGraph.Filter
{
    public class JsonEvaluator : IDisposable
    {
        private readonly JsonSelector   jsonSelector    = new JsonSelector();
        private readonly JsonLambda     jsonLambda      = new JsonLambda();       

        public void Dispose() {
            jsonSelector.Dispose();
        }

        public bool Filter(string json, string filter) {
            return true;
        }

        // --- Filter
        // Filter(Expression) variant only for development
        public bool Filter<T>(string json, Expression<Func<T, bool>> filter) {
            var op = Operator.FromFilter(filter);
            return Filter(json, (BoolOp)op);
        }

        public bool Filter(string json, BoolOp filter) {
            jsonLambda.InitLambda(filter);
            return Filter(json, jsonLambda);
        }

        public bool Filter(string json, JsonLambda filter) {
            ReadJsonFields(json, jsonLambda);
            
            var evalResult = filter.op.Eval();
            
            foreach (var result in evalResult.values) {
                if (result.CompareTo(Operator.True) != 0)
                    return false;
            }
            return true;
        }

        // --- Eval
        public object Eval(string json, Operator op) {
            jsonLambda.InitLambda(op);
            return Eval(json, jsonLambda);
        }

        public object Eval(string json, JsonLambda lambda) {
            ReadJsonFields(json, jsonLambda);

            var evalResult = lambda.op.Eval();
            
            if (evalResult.values.Count == 1)
                return evalResult.values[0].AsObject();
            
            object[] evalResults = new object[evalResult.values.Count];
            for (int n = 0; n < evalResult.values.Count; n++) {
                var result = evalResult.values[n];
                evalResults[n] = result.AsObject();
            }
            return evalResults;
        }

        private void ReadJsonFields(string json, JsonLambda lambda) {
            var query = lambda.selectorQuery;
            jsonSelector.Select(json, query);
            var selectorResults = query.GetResult();
            var fields = lambda.fields;
            for (int n = 0; n < fields.Count; n++) {
                Field field = fields[n];
                field.evalResult = new EvalResult(selectorResults[n].values);
            }
        }

    }
}