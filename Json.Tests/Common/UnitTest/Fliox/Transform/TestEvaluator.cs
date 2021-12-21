// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Transform;
using Friflo.Json.Fliox.Transform.Query.Parser;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Transform
{
    public static class TestEvaluator
    {
        [Test]
        public static void TestEval() {
            using (var eval = new JsonEvaluator()) {
                AssertEval(eval);
            }
        }

        private const string  Json    = @"{""strVal"": ""abc"", ""intVal"": 42, ""nullVal"": null}";

        
        private static void AssertEval(JsonEvaluator eval) {
            string  error;
            {
                var result = Eval ("1 == 1", Json, eval, out _);
                AreEqual(true, result);
            }
            // --- arithmetic functions
            {
                Eval ("o => Abs(o.strVal) >= 1", Json, eval, out error);
                AreEqual("expect numeric operand. was: 'abc' in Abs(o.strVal)", error);
            } {
                // Abs(null) = null, 1 < null = false
                var result = Eval ("o => 1 < Abs(o.nullVal)", Json, eval, out error);
                AreEqual(false, result);
            }
            // --- arithmetic operators
            {
                Eval ("o => o.intVal * o.strVal", Json, eval, out error);
                AreEqual("expect two numeric operands. left: 42, right: 'abc'", error);
            } {
                Eval ("o => o.intVal + o.strVal", Json, eval, out error);
                AreEqual("expect two numeric operands. left: 42, right: 'abc'", error);
            } {
                Eval ("o => o.strVal - o.intVal", Json, eval, out error);
                AreEqual("expect two numeric operands. left: 'abc', right: 42", error);
            } {
                Eval ("o => o.strVal / o.intVal", Json, eval, out error);
                AreEqual("expect two numeric operands. left: 'abc', right: 42", error);
            }
            // --- string functions
            {
                Eval ("o => o.strVal.Contains(o.intVal)", Json, eval, out error);
                AreEqual("expect two string operands. left: 'abc', right: 42", error);
            } {
                Eval ("o => o.strVal.StartsWith(o.intVal)", Json, eval, out error);
                AreEqual("expect two string operands. left: 'abc', right: 42", error);
            } {
                Eval ("o => o.strVal.EndsWith(o.intVal)", Json, eval, out error);
                AreEqual("expect two string operands. left: 'abc', right: 42", error);
            } {
                Eval ("o => o.intVal.EndsWith('abc')", Json, eval, out error);
                AreEqual("expect two string operands. left: 42, right: 'abc'", error);
            } /* {
                Eval ("o => o.EndsWith(.bar)", Json, eval, out error);
                AreEqual("expect two string operands. left: 42, right: 'abc'", error);
            } */
        }
        
        [Test]
        public static void TestFilter() {
            using (var eval = new JsonEvaluator()) {
                AssertFilter(eval);
            }
        }
        
        private static void AssertFilter(JsonEvaluator eval) {
            {
                var result = Filter ("o => o.foo.EndsWith('abc')", Json, eval, out _);
                IsFalse(result);
            } {
                var result = Filter ("o => o.strVal.EndsWith('abc')", Json, eval, out _);
                IsTrue(result);
            } {
                var result = Filter ("o => o.strVal.EndsWith(o.foo)", Json, eval, out _);
                IsFalse(result);
            } {
                var result = Filter ("o => 1 < 1.1", Json, eval, out _);
                IsTrue(result);
            } {
                var result = Filter ("o => 1.1 > 1", Json, eval, out _);
                IsTrue(result);
            } {
                var result = Filter ("o => 1.1 < 1.2", Json, eval, out _);
                IsTrue(result);
            } {
                var result = Filter ("o => 1.1 == 1.1", Json, eval, out _);
                IsTrue(result);
            }
        }
        
        private static object Eval(string operation, string json, JsonEvaluator eval, out string error) {
            var op      = QueryBuilder.Parse(operation, out error);
            if (error != null)
                return null;
            AreEqual(operation, op.Linq);
            var lambda  = new JsonLambda(op);
            var value   = new JsonValue(json);
            var result  = eval.Eval(value, lambda, out error);
            return result;
        }
        
        private static bool Filter(string operation, string json, JsonEvaluator eval, out string error) {
            var op      = (FilterOperation)QueryBuilder.Parse(operation, out error);
            if (error != null)
                return false;
            AreEqual(operation, op.Linq);
            var filter  = new JsonFilter(op);
            var value   = new JsonValue(json);
            var result  = eval.Filter(value, filter, out error);
            return result;
        }
        
        // ------------------------ evaluate operations ------------------------ 
        [Test]
        public static void TestFilterUndefinedScalar() {
            using (var eval = new JsonEvaluator()) {
                // use an aggregate (Max) of an empty array and compare it to a scalar
                AssertFilterUndefinedScalar ("o => o.items.Max(item => item.amount) == 1", eval);
                AssertFilterUndefinedScalar ("o => o.items.Max(item => item.amount) != 1", eval);
                AssertFilterUndefinedScalar ("o => o.items.Max(item => item.amount) <  1", eval);
                AssertFilterUndefinedScalar ("o => o.items.Max(item => item.amount) <= 1", eval);
                AssertFilterUndefinedScalar ("o => o.items.Max(item => item.amount) >  1", eval);
                AssertFilterUndefinedScalar ("o => o.items.Max(item => item.amount) >= 1", eval);
            }
        }
        
        private static void AssertFilterUndefinedScalar(string operation, JsonEvaluator eval) {
            var json    = @"{ ""items"": [] }";
            var op      = (FilterOperation)QueryBuilder.Parse(operation, out _);
            var filter  = new JsonFilter(op);
            var result  = eval.Filter(new JsonValue(json), filter, out _);
            IsFalse(result);
        } 
    }
}