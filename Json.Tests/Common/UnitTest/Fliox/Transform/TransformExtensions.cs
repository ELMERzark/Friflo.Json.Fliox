// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.


using System.Collections.Generic;
using Friflo.Json.Fliox;
using Friflo.Json.Fliox.Transform;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Transform
{
    public static class TransformExtensions
    {
        public static object Eval (this JsonEvaluator evaluator, string json, JsonLambda lambda) {
            var jsonValue = new JsonValue(json);
            return evaluator.Eval(jsonValue, lambda, out _);
        }
        
        public static bool Filter(this JsonEvaluator evaluator, string json, JsonFilter filter) {
            var jsonValue = new JsonValue(json);
            return evaluator.Filter(jsonValue, filter, out _);
        }
        
        public static IReadOnlyList<ScalarSelectResult> Select(this ScalarSelector selector, string json, ScalarSelect scalarSelect) {
            var jsonValue = new JsonValue(json);
            return selector.Select(jsonValue, scalarSelect);
        }
    }
}