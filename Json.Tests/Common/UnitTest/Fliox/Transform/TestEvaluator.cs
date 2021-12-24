// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Transform;
using Friflo.Json.Fliox.Transform.Query.Parser;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Transform
{
    public static class TestEvaluator
    {
        private const string  Json    =
@"{
    ""strVal"":  ""abc"",
    ""intVal"":  42,
    ""nullVal"": null,
    ""boolVal"": true,
    ""obj"":     {},
    ""array"":   [1,2,3]
}";

        [Test]
        public static void TestEvalArithmeticFunctions() {
            using (var eval = new JsonEvaluator()) {
                string  error;
                // --- error
                {
                    Eval ("o => Abs(o.strVal) >= 1", Json, eval, out error);
                    AreEqual("expect numeric operand. was: 'abc' in Abs(o.strVal)", error);
                }
                // --- success
                {
                    var result = Eval ("o => Abs(-1)", Json, eval, out error);
                    AreEqual(1, result);
                } {
                    var result = Eval ("o => Log(E)", Json, eval, out error);
                    AreEqual(1, result);
                } {
                    var result = Eval ("o => Exp(1)", Json, eval, out error);
                    AreEqual(Math.E, result);
                } {
                    var result = Eval ("o => Sqrt(4)", Json, eval, out error);
                    AreEqual(2, result);
                } {
                    var result = Eval ("o => Floor(2.5)", Json, eval, out error);
                    AreEqual(2, result);
                } {
                    var result = Eval ("o => Ceiling(2.5)", Json, eval, out error);
                    AreEqual(3, result);
                }
                // --- null
                {
                    var result = Eval ("o => Abs(o.nullVal)", Json, eval, out error);
                    IsNull(result);
                } {
                    var result = Eval ("o => Log(o.nullVal)", Json, eval, out error);
                    IsNull(result);
                } {
                    var result = Eval ("o => Exp(o.nullVal)", Json, eval, out error);
                    IsNull(result);
                } {
                    var result = Eval ("o => Sqrt(o.nullVal)", Json, eval, out error);
                    IsNull(result);
                } {
                    var result = Eval ("o => Floor(o.nullVal)", Json, eval, out error);
                    IsNull(result);
                } {
                    var result = Eval ("o => Ceiling(o.nullVal)", Json, eval, out error);
                    IsNull(result);
                } /* { todo
                    var result = Eval ("o => -o.nullVal", Json, eval, out error);
                    IsNull(result);
                } */
            }
        }
        
        [Test]
        public static void TestEvalArithmeticOperators() {
            using (var eval = new JsonEvaluator()) {
                string  error;
                // --- error
                {
                    Eval ("o => o.intVal * o.strVal", Json, eval, out error);
                    AreEqual("expect numeric operands. left: 42, right: 'abc' in o.intVal * o.strVal", error);
                } {
                    Eval ("o => o.intVal + o.strVal", Json, eval, out error);
                    AreEqual("expect numeric operands. left: 42, right: 'abc' in o.intVal + o.strVal", error);
                } {
                    Eval ("o => o.strVal - o.intVal", Json, eval, out error);
                    AreEqual("expect numeric operands. left: 'abc', right: 42 in o.strVal - o.intVal", error);
                } {
                    Eval ("o => o.strVal / o.intVal", Json, eval, out error);
                    AreEqual("expect numeric operands. left: 'abc', right: 42 in o.strVal / o.intVal", error);
                }
                // --- success
                {
                    var result = Eval ("o => o.intVal * 1", Json, eval, out error);
                    AreEqual(42, result);
                } {
                    var result = Eval ("o => o.intVal + 1", Json, eval, out error);
                    AreEqual(43, result);
                } {
                    var result = Eval ("o => o.intVal - 1", Json, eval, out error);
                    AreEqual(41, result);
                } {
                    var result = Eval ("o => o.intVal / 2", Json, eval, out error);
                    AreEqual(21, result);
                }
                // --- null left
                {
                    var result = Eval ("o => o.nullVal * 1", Json, eval, out error);
                    IsNull(result);
                } {
                    var result = Eval ("o => o.nullVal + 1", Json, eval, out error);
                    IsNull(result);
                } {
                    var result = Eval ("o => o.nullVal - 1", Json, eval, out error);
                    IsNull(result);
                } {
                    var result = Eval ("o => o.nullVal / 2", Json, eval, out error);
                    IsNull(result);
                }
                // --- null right
                {
                    var result = Eval ("o => 1 * o.nullVal", Json, eval, out error);
                    IsNull(result);
                } {
                    var result = Eval ("o => 1 + o.nullVal", Json, eval, out error);
                    IsNull(result);
                } {
                    var result = Eval ("o => 1 - o.nullVal", Json, eval, out error);
                    IsNull(result);
                } {
                    var result = Eval ("o => 2 / o.nullVal", Json, eval, out error);
                    IsNull(result);
                }
            }
        }
        
        [Test]
        public static void TestEvalString() {
            using (var eval = new JsonEvaluator()) {
                string  error;
                // --- error
                {
                    Eval ("o => o.strVal.Contains(o.intVal)", Json, eval, out error);
                    AreEqual("expect string operands. left: 'abc', right: 42 in o.strVal.Contains(o.intVal)", error);
                } {
                    Eval ("o => o.strVal.StartsWith(o.intVal)", Json, eval, out error);
                    AreEqual("expect string operands. left: 'abc', right: 42 in o.strVal.StartsWith(o.intVal)", error);
                } {
                    Eval ("o => o.strVal.EndsWith(o.intVal)", Json, eval, out error);
                    AreEqual("expect string operands. left: 'abc', right: 42 in o.strVal.EndsWith(o.intVal)", error);
                } {
                    Eval ("o => o.intVal.EndsWith('abc')", Json, eval, out error);
                    AreEqual("expect string operands. left: 42, right: 'abc' in o.intVal.EndsWith('abc')", error);
                }
                // --- success
                {
                    var result = Filter ("o => o.strVal.EndsWith('abc')", Json, eval, out _);
                    IsTrue(result);
                } {
                    // missing member o.foo => o.foo = null
                    var result = Filter ("o => o.foo.EndsWith('abc')", Json, eval, out _);
                    IsFalse(result);
                } {
                    // missing member o.foo => o.foo = null
                    var result = Filter ("o => o.strVal.EndsWith(o.foo)", Json, eval, out _);
                    IsFalse(result);
                }
                // --- null left
                {
                    var result = Eval ("o => o.nullVal.Contains('abc')", Json, eval, out _);
                    IsNull(result);
                } {
                    var result = Eval ("o => o.nullVal.EndsWith('abc')", Json, eval, out _);
                    IsNull(result);
                } {
                    var result = Eval ("o => o.nullVal.StartsWith('abc')", Json, eval, out _);
                    IsNull(result);
                }
                // --- null right
                {
                    var result = Eval ("o => 'abc'.Contains(o.nullVal)", Json, eval, out _);
                    IsNull(result);
                } {
                    var result = Eval ("o => 'abc'.EndsWith(o.nullVal)", Json, eval, out _);
                    IsNull(result);
                } {
                    var result = Eval ("o => 'abc'.StartsWith(o.nullVal)", Json, eval, out _);
                    IsNull(result);
                }
            }
        }
        
        [Test]
        public static void TestEvalEquality() {
            using (var eval = new JsonEvaluator()) {
                string  error;
                // --- error
                {
                    Eval ("o => o.strVal == 1", Json, eval, out error);
                    AreEqual("incompatible operands: 'abc' == 1 in o.strVal == 1", error);
                } {
                    Eval ("o => o.strVal == true", Json, eval, out error);
                    AreEqual("incompatible operands: 'abc' == true in o.strVal == true", error);
                } {
                    Eval ("o => true == o.strVal", Json, eval, out error);
                    AreEqual("incompatible operands: true == 'abc' in true == o.strVal", error);
                } {
                    Eval ("o => 1 == o.strVal", Json, eval, out error);
                    AreEqual("incompatible operands: 1 == 'abc' in 1 == o.strVal", error);
                }
                // --- error: object / array
                {
                    Eval ("o => o.obj == 'abc'", Json, eval, out error);
                    AreEqual("invalid operand: (object) == 'abc' in o.obj == 'abc'", error);
                } {
                    Eval ("o => o.array == 'abc'", Json, eval, out error);
                    AreEqual("invalid operand: (array) == 'abc' in o.array == 'abc'", error);
                } {
                    Eval ("o => 'abc' == o.obj", Json, eval, out error);
                    AreEqual("invalid operand: 'abc' == (object) in 'abc' == o.obj", error);
                } {
                    Eval ("o => 'abc' == o.array", Json, eval, out error);
                    AreEqual("invalid operand: 'abc' == (array) in 'abc' == o.array", error);
                }
                // --- success
                {
                    var result = Filter ("o => 1.1 == 1.1", Json, eval, out _);
                    IsTrue(result);
                }
                // --- null left / right
                {
                    var result = Eval ("o => o.nullVal == 1", Json, eval, out error);
                    IsNull(result);
                } {
                    var result = Eval ("o => 1 == o.nullVal", Json, eval, out error);
                    IsNull(result);
                } {
                    var result = Eval ("o => 1.1 == o.nullVal", Json, eval, out error);
                    IsNull(result);
                } {
                    var result = Eval ("o => 'abc' == o.nullVal", Json, eval, out error);
                    IsNull(result);
                } {
                    var result = Eval ("o => true == o.nullVal", Json, eval, out error);
                    IsNull(result);
                }
            }
        }
        
        [Test]
        public static void TestEvalCompare() {
            using (var eval = new JsonEvaluator()) {
                string  error;
                // --- error
                {
                    Eval ("o => o.strVal < 1", Json, eval, out error);
                    AreEqual("incompatible operands: 'abc' < 1 in o.strVal < 1", error);
                } {
                    Eval ("o => o.strVal < o.boolVal", Json, eval, out error);
                    AreEqual("incompatible operands: 'abc' < true in o.strVal < o.boolVal", error);
                } {
                    Eval ("o => o.boolVal < o.strVal", Json, eval, out error);
                    AreEqual("incompatible operands: true < 'abc' in o.boolVal < o.strVal", error);
                } {
                    Eval ("o => 1 < o.strVal", Json, eval, out error);
                    AreEqual("incompatible operands: 1 < 'abc' in 1 < o.strVal", error);
                }
                // --- error: object / array
                {
                    Eval ("o => o.obj < 'abc'", Json, eval, out error);
                    AreEqual("invalid operand: (object) < 'abc' in o.obj < 'abc'", error);
                } {
                    Eval ("o => o.array < 'abc'", Json, eval, out error);
                    AreEqual("invalid operand: (array) < 'abc' in o.array < 'abc'", error);
                } {
                    Eval ("o => 'abc' < o.obj", Json, eval, out error);
                    AreEqual("invalid operand: 'abc' < (object) in 'abc' < o.obj", error);
                } {
                    Eval ("o => 'abc' < o.array", Json, eval, out error);
                    AreEqual("invalid operand: 'abc' < (array) in 'abc' < o.array", error);
                }
                // --- success
                {
                    var result = Filter ("o => 1 < 1.1", Json, eval, out _);
                    IsTrue(result);
                } {
                    var result = Filter ("o => 1.1 > 1", Json, eval, out _);
                    IsTrue(result);
                } {
                    var result = Filter ("o => 1.1 < 1.2", Json, eval, out _);
                    IsTrue(result);
                } {
                    var result = Eval ("1 == 1", Json, eval, out _);
                    AreEqual(true, result);
                }
                // --- null
                {
                    var result = Filter ("o => o.nullVal < 1", Json, eval, out _);
                    IsFalse(result);
                } {
                    var result = Filter ("o => 1 < o.nullVal", Json, eval, out _);
                    IsFalse(result);
                } {
                    var result = Filter ("o => 1.1 < o.nullVal", Json, eval, out _);
                    IsFalse(result);
                } {
                    var result = Filter ("o => 'abc' < o.nullVal", Json, eval, out _);
                    IsFalse(result);
                } {
                    // Abs(null) = null, 1 < null = null
                    var result = Eval ("o => 1 < Abs(o.nullVal)", Json, eval, out error);
                    IsNull(result);
                }
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