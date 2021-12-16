﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Friflo.Json.Fliox.Transform;
using Friflo.Json.Fliox.Transform.Query.Ops;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Tests.Common.Utils;
using NUnit.Framework;
using static NUnit.Framework.Assert;

using static Friflo.Json.Tests.Common.UnitTest.Fliox.Transform.AssertEqual;
using Contains = Friflo.Json.Fliox.Transform.Query.Ops.Contains;


// ReSharper disable CollectionNeverQueried.Global
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Transform
{
    public class Person
    {
        public          string          name;
        public          int             age;
        public          Address         address;
        public readonly List<Person>    children = new List<Person>();
        public readonly List<Hobby>     hobbies = new List<Hobby>();
    }
    
    public class Hobby
    {
        public          string          name;
    }
    
    public class Address
    {
        public          Street          street;
        [Fri.Property(Name = "city")]
        public          string          cityName;
    }
    
    public class Street
    {
        public          string          name;
        public          string          houseNumber;
    }

    
    
    public static class TestQuery
    {
        public static readonly Person Peter =         new Person {
            name = "Peter", age = 40,
            children = {
                new Person {
                    name = "Paul" , age = 20,
                    hobbies = {
                        new Hobby{ name= "Gaming"},
                        new Hobby{ name= "Biking"},
                        new Hobby{ name= "Travelling"},
                    },
                    address = new Address {
                        street  = new Street {
                            name        = "Lombard St",
                            houseNumber = "11"
                        },
                        cityName = "San Francisco"
                    }
                },
                new Person {
                    name = "Marry", age = 20,
                    hobbies = {
                        new Hobby{ name= "Biking"},
                        new Hobby{ name= "Surfing"}
                    }
                }
            }
        };

        public static readonly Person John =         new Person {
            name = "John",  age = 30,
            children = {
                new Person {
                    name = "Simon", age = 10,
                    hobbies = {
                        new Hobby{ name= "Biking"}
                    }
                },
                new Person {
                    name = "Garfunkel", age = 11,
                    hobbies = {
                        new Hobby{ name= "Biking"}
                    }
                }
            }
        };
        
        [Test]
        public static void TestFilter () {
            using (var eval         = new JsonEvaluator())
            using (var jsonMapper   = new ObjectMapper())
            {
                jsonMapper.Pretty = true;
                var peter = jsonMapper.Write(Peter);
                var john  = jsonMapper.Write(John);

                // ---
                var  isPeter         = new Filter("p", new Equal(new Field (".name"), new StringLiteral ("Peter"))).Filter();
                AreEqual("p => p.name == 'Peter'",  isPeter.Linq);
                Cosmos  ("c.name = 'Peter'",        isPeter.Query.Cosmos);
                var  isPeter2        = JsonFilter.Create<Person>(p => p.name == "Peter");
                AreEqual("p => p.name == 'Peter'", isPeter2.Linq);
                
                bool IsPeter(Person p) => p.name == "Peter";

                var  isAgeGreater35Op = new GreaterThan(new Field(".age"), new LongLiteral(35));
                var  isAgeGreater35  = isAgeGreater35Op.Filter();
                bool IsAgeGreater35(Person p) => p.age > 35;
                
                var isNotAgeGreater35  = new Not(isAgeGreater35Op).Filter();
            
                IsTrue  (IsPeter(Peter));
                IsTrue  (eval.Filter(peter, isPeter));
                IsFalse (eval.Filter(john,  isPeter));
                
                IsTrue  (IsAgeGreater35(Peter));
                IsTrue  (eval.Filter(peter, isAgeGreater35));
                IsFalse (eval.Filter(john,  isAgeGreater35));
                // Not
                IsFalse (eval.Filter(peter, isNotAgeGreater35));
                IsTrue  (eval.Filter(john,  isNotAgeGreater35));
                
                var  equalUnknownField         = new Equal(new Field (".unknown"), new StringLiteral ("SomeString")).Filter();
                IsFalse(eval.Filter(john, equalUnknownField));

                // --- Any
                var  hasChildPaul = new Any (new Field (".children"), "child", new Equal (new Field ("child.name"), new StringLiteral ("Paul"))).Filter();
                bool HasChildPaul(Person p) => p.children.Any(child => child.name == "Paul");
                
                var hasChildAgeLess12 = new Any (new Field (".children"), "child", new LessThan (new Field ("child.age"), new LongLiteral (12))).Filter();
                
                IsTrue (HasChildPaul(Peter));
                IsTrue (eval.Filter(peter, hasChildPaul));
                IsFalse(eval.Filter(john,  hasChildPaul));
                
                IsFalse(eval.Filter(peter, hasChildAgeLess12));
                IsTrue (eval.Filter(john,  hasChildAgeLess12));

                var  anyEqualUnknownField  = new Any (new Field (".children"), "child", new Equal (new Field ("child.unknown"), new StringLiteral ("SomeString"))).Filter();
                IsFalse(eval.Filter(john, anyEqualUnknownField));

                // --- All
                var allChildAgeEquals20 = new All (new Field (".children"), "child", new Equal(new Field ("child.age"), new LongLiteral (20))).Filter();
                IsTrue (eval.Filter(peter, allChildAgeEquals20));
                IsFalse(eval.Filter(john,  allChildAgeEquals20));
                
                var  allEqualUnknownField  = new All (new Field (".children"), "child", new Equal (new Field ("child.unknown"), new StringLiteral ("SomeString"))).Filter();
                IsTrue(eval.Filter(john, allEqualUnknownField));
                
                // --- Count() with lambda parameter -> is not a Filter
                var countChildAgeEquals20 = new CountWhere (new Field (".children"), "child", new Equal(new Field ("child.age"), new LongLiteral (20))).Lambda();
                AreEqual(2, eval.Eval(peter, countChildAgeEquals20));
                AreEqual(0, eval.Eval(john,  countChildAgeEquals20));

                // --- test with arithmetic operations
                var  isAge40  = new Equal(new Field (".age"), new Add(new LongLiteral (35), new LongLiteral(5))).Filter();
                IsTrue  (eval.Filter(peter, isAge40));
                
                // var  isChildAge20  = new Equal(new Field (".children[*].age"), new Add(new LongLiteral (15), new LongLiteral(5))).Filter();
                var isChildAge20 = new Filter("p", new All(new Field (".children"), "child", new Equal(new Field ("child.age"), new LongLiteral (20)))).Filter();
                var isChildAge20Expect = Operation.FromFilter((Person p) => p.children.All(child => child.age == 20));
                AreEqual(isChildAge20Expect.Linq, isChildAge20.Linq);
                IsTrue  (eval.Filter(peter, isChildAge20));
                
                
                
                // ------------------------------ Test runtime assertions ------------------------------
                Exception e;
                // --- compare operations must not be reused
                e = Throws<InvalidOperationException>(() => _ = new Equal(isAgeGreater35Op, isAgeGreater35Op).Filter());
                AreEqual("Used operation instance is not applicable for reuse. Use a clone. Type: GreaterThan, instance: .age > 35", e.Message);
                
                // --- group operations must not be reused
                var testGroupOp = new And(new List<FilterOperation> {new Equal(new StringLiteral("A"), new StringLiteral("B"))});
                e = Throws<InvalidOperationException>(() => _ = new Equal(testGroupOp, testGroupOp).Filter());
                AreEqual("Used operation instance is not applicable for reuse. Use a clone. Type: And, instance: 'A' == 'B'", e.Message);

                // --- literal and field operations are applicable for reuse
                var testLiteral = new StringLiteral("Test");
                var reuseLiterals = new Equal(testLiteral, testLiteral).Filter();
                eval.Filter(peter, reuseLiterals);
                
                var testField = new Field (".name");
                var reuseField = new Equal(testField, testField).Filter();
                eval.Filter(peter, reuseField);
            }
        }

        [Test]
        public static void NoAllocFilter() {
            var memLog = new MemoryLogger(10, 10, MemoryLog.Enabled);
            using (var eval = new JsonEvaluator())
            using (var jsonMapper = new ObjectMapper()) {
                jsonMapper.Pretty   = true;
                var peter           = new JsonValue(jsonMapper.WriteAsArray(Peter));
                
                var anyChildAgeWithin10And20 = JsonFilter.Create<Person>(p => p.children.All(child => child.age >= 20 && child.age <= 20));
                bool result = false;
                for (int n = 0; n < 100; n++) {
                    result = eval.Filter(peter, anyChildAgeWithin10And20, out _);
                    memLog.Snapshot();
                }
                IsTrue(result);
            }
            memLog.AssertNoAllocations();
        }

        [Test]
        public static void TestGroupFilter () {
            using (var eval         = new JsonEvaluator())
            using (var jsonMapper   = new ObjectMapper())
            {
                jsonMapper.Pretty = true;
                var peter = jsonMapper.Write(Peter);
                var john  = jsonMapper.Write(John);
                
                // --- Any
                // var  hasChildHobbySurfing = new Any (new Field (".children"), "child", new Equal (new Field ("child.hobbies[*].name"), new StringLiteral ("Surfing"))).Filter();
                var hasHobbySurfing      = new Any (new Field("child.hobbies"), "hobby", new Equal(new Field("hobby.name"), new StringLiteral ("Surfing")));
                var hasChildHobbySurfing = new Any (new Field (".children"), "child", hasHobbySurfing).Filter();
                var hasChildHobbySurfingExp = Filter((Person p) => p.children.Any(child => child.hobbies.Any(hobby => hobby.name == "Surfing")), out string body);
                AreEqual("p.children.Any(child => child.hobbies.Any(hobby => (hobby.name == 'Surfing')))", body);
                AreEqual (".children.Any(child => child.hobbies.Any(hobby => hobby.name == 'Surfing'))", hasChildHobbySurfing.Linq);
                IsTrue (hasChildHobbySurfingExp(Peter));
            //  IsTrue (eval.Filter(peter, hasChildHobbySurfing)); todo
            //  IsFalse(eval.Filter(john,  hasChildHobbySurfing)); todo
            }
        }

        private static Func<T, bool> Filter<T>(Expression<Func<T, bool>> filter, out string name) {
            name = filter.Body.ToString();
            name = name.Replace('"', '\'');
            return filter.Compile();
        }

        [Test]
        public static void TestEval() {
          using (var eval     = new JsonEvaluator())
          using (var mapper   = new ObjectMapper())
          {
            mapper.Pretty = true;
            var john  = mapper.Write(John);
            mapper.Pretty = false;

            // --- use expression
            AreEqual("hello",   eval.Eval("{}", JsonLambda.Create<object>(p => "hello")));
            
            // --- nullary operation (boolean literals)
            {
                var @true   = new TrueLiteral();
                AssertJson(mapper, @true, "{'op':'true'}");
                AreEqual(true,   eval.Eval("{}", @true.Lambda()));
            } {
                var @false   = new FalseLiteral();
                AssertJson(mapper, @false, "{'op':'false'}");
                AreEqual(false,  eval.Eval("{}", @false.Lambda()));
            }
            
            // --- unary literal operations
            {
                var stringLiteral   = new StringLiteral("hello");
                AssertJson(mapper, stringLiteral, "{'op':'string','value':'hello'}");
                AreEqual("hello",   eval.Eval("{}", stringLiteral.Lambda()));
            } {
                var doubleLiteral   = new DoubleLiteral(42.0);
                AssertJson(mapper, doubleLiteral, "{'op':'double','value':42.0}");
                AreEqual(42.0,      eval.Eval("{}", doubleLiteral.Lambda()));
            } {
                var longLiteral   = new LongLiteral(42);
                AssertJson(mapper, longLiteral, "{'op':'int64','value':42}");
                AreEqual(42.0,      eval.Eval("{}", longLiteral.Lambda()));
            } {
                var nullLiteral     = new NullLiteral();
                AssertJson(mapper, nullLiteral, "{'op':'null'}");
                AreEqual(null,      eval.Eval("{}", nullLiteral.Lambda()));
            } 
            
            // --- unary arithmetic operations
            {
                var abs     = new Abs(new LongLiteral(-2));
                AssertJson(mapper, abs, "{'op':'abs','value':{'op':'int64','value':-2}}");
                AreEqual(2,         eval.Eval("{}", abs.Lambda()));
            } {
                var ceiling = new Ceiling(new DoubleLiteral(2.5));
                AssertJson(mapper, ceiling, "{'op':'ceiling','value':{'op':'double','value':2.5}}");
                AreEqual(3,         eval.Eval("{}", ceiling.Lambda()));
            } {
                var floor   = new Floor(new DoubleLiteral(2.5));
                AssertJson(mapper, floor, "{'op':'floor','value':{'op':'double','value':2.5}}");
                AreEqual(2,         eval.Eval("{}", floor.Lambda()));
            } {
                var exp     = new Exp(new DoubleLiteral(Math.Log(2)));
                AssertJson(mapper, exp, "{'op':'exp','value':{'op':'double','value':0.6931471805599453}}");
                AreEqual(2,         eval.Eval("{}", exp.Lambda()));
            } {
                var log     = new Log(new DoubleLiteral(Math.Exp(3)));
                AssertJson(mapper, log, "{'op':'log','value':{'op':'double','value':20.085536923187668}}");
                AreEqual(3,         eval.Eval("{}", log.Lambda()));
            } {
                var sqrt    = new Sqrt(new DoubleLiteral(9));
                AssertJson(mapper, sqrt, "{'op':'sqrt','value':{'op':'double','value':9.0}}");
                AreEqual(3,         eval.Eval("{}", sqrt.Lambda()));
            } {
                var negate  = new Negate(new DoubleLiteral(1));
                AssertJson(mapper, negate, "{'op':'negate','value':{'op':'double','value':1.0}}");
                AreEqual(-1,        eval.Eval("{}", negate.Lambda()));
            }
            
            // --- binary arithmetic operations
            {
                var add      = new Add(new LongLiteral(1), new LongLiteral(2));
                AssertJson(mapper, add, "{'op':'add','left':{'op':'int64','value':1},'right':{'op':'int64','value':2}}");
                AreEqual(3,         eval.Eval("{}", add.Lambda()));
            } {
                var subtract = new Subtract(new LongLiteral(1), new LongLiteral(2));
                AssertJson(mapper, subtract, "{'op':'subtract','left':{'op':'int64','value':1},'right':{'op':'int64','value':2}}");
                AreEqual(-1,        eval.Eval("{}", subtract.Lambda()));
            } {
                var multiply = new Multiply(new LongLiteral(2), new LongLiteral(3));
                AssertJson(mapper, multiply, "{'op':'multiply','left':{'op':'int64','value':2},'right':{'op':'int64','value':3}}");
                AreEqual(6,         eval.Eval("{}", multiply.Lambda()));
            } {
                var divide   = new Divide(new LongLiteral(10), new LongLiteral(2));
                AssertJson(mapper, divide, "{'op':'divide','left':{'op':'int64','value':10},'right':{'op':'int64','value':2}}");
                AreEqual(5,         eval.Eval("{}", divide.Lambda()));
            }
            
            // --- unary aggregate operations
            {
                var min         = new Lambda("o", new Min(new Field(".children"), "child", new Field("child.age")));
                AssertJson(mapper, min, "{'op':'lambda','arg':'o','body':{'op':'min','field':{'name':'.children'},'arg':'child','array':{'op':'field','name':'child.age'}}}");
                AreEqual(10,         eval.Eval(john, min.Lambda()));
                AreEqual("o => o.children.Min(child => child.age)", min.Linq);
            } {
                var max         = new Lambda("o", new Max(new Field(".children"), "child", new Field("child.age")));
                AssertJson(mapper, max, "{'op':'lambda','arg':'o','body':{'op':'max','field':{'name':'.children'},'arg':'child','array':{'op':'field','name':'child.age'}}}");
                AreEqual(11,         eval.Eval(john, max.Lambda()));
                AreEqual("o => o.children.Max(child => child.age)", max.Linq);
            } {
                var sum         = new Lambda("o", new Sum(new Field(".children"), "child", new Field("child.age")));
                AssertJson(mapper, sum, "{'op':'lambda','arg':'o','body':{'op':'sum','field':{'name':'.children'},'arg':'child','array':{'op':'field','name':'child.age'}}}");
                AreEqual(21,         eval.Eval(john, sum.Lambda()));
                AreEqual("o => o.children.Sum(child => child.age)", sum.Linq);
            } {
                var average     = new Lambda("o", new Average(new Field(".children"), "child", new Field("child.age")));
                AssertJson(mapper, average, "{'op':'lambda','arg':'o','body':{'op':'average','field':{'name':'.children'},'arg':'child','array':{'op':'field','name':'child.age'}}}");
                AreEqual(10.5,       eval.Eval(john, average.Lambda()));
                AreEqual("o => o.children.Average(child => child.age)", average.Linq);
            } {
                var count       = new Lambda("o", new Count(new Field(".children")));
                AssertJson(mapper, count, "{'op':'lambda','arg':'o','body':{'op':'count','field':{'name':'.children'}}}");
                AreEqual(2,          eval.Eval(john, count.Lambda()));
                AreEqual("o => o.children.Count()", count.Linq);
            }
            
            // --- binary string operations
            {
                var contains     = new Contains(new StringLiteral("12345"), new StringLiteral("234"));
                AssertJson(mapper, contains, "{'op':'contains','left':{'op':'string','value':'12345'},'right':{'op':'string','value':'234'}}");
                AreEqual(true,         eval.Eval("{}", contains.Lambda()));
            } {
                var startsWith     = new StartsWith(new StringLiteral("12345"), new StringLiteral("123"));
                AssertJson(mapper, startsWith, "{'op':'startsWith','left':{'op':'string','value':'12345'},'right':{'op':'string','value':'123'}}");
                AreEqual(true,         eval.Eval("{}", startsWith.Lambda()));
            } {
                var endsWith     = new EndsWith(new StringLiteral("12345"), new StringLiteral("345"));
                AssertJson(mapper, endsWith, "{'op':'endsWith','left':{'op':'string','value':'12345'},'right':{'op':'string','value':'345'}}");
                AreEqual(true,         eval.Eval("{}", endsWith.Lambda()));
            }
          }
        }

        private static void AssertJson(ObjectMapper mapper, Operation op, string json) {
            var result = mapper.Write(op);
            var singleQuoteResult = result.Replace('\"', '\'');
            if (json != singleQuoteResult) {
                Fail($"Expected: {json}\nBut was:  {singleQuoteResult}");
            }
            // assert mapping of JSON string to/from Operation
            Operation opRead  = mapper.Read<Operation>(result);
            string   opWrite = mapper.Write(opRead);
            AreEqual(result, opWrite);
            
            // assert mapping of JSON string to/from FilterOperation
            if (opRead is FilterOperation) {
                FilterOperation boolOpRead  = mapper.Read<FilterOperation>(result);
                string boolOpWrite = mapper.Write(boolOpRead);
                AreEqual(result, boolOpWrite);
            }
        }

        private static FilterOperation FromFilter<T>(Expression<Func<T, bool>> filter) {
            var lambda = (Filter)Operation.FromFilter(filter);
            return lambda.body;
        }
        
        private static Operation FromLambda<T>(Expression<Func<T, object>> filter) {
            var lambda = (Lambda)Operation.FromLambda(filter);
            return lambda.body;
        }
        
        [Test]
        public static void TestQueryConversion() {
          using (var mapper   = new ObjectMapper()) {
            // --- comparision operations
            {
                var isEqual =           (Equal)             FromFilter((Person p) => p.name == "Peter");
                AssertJson(mapper, isEqual, "{'op':'equal','left':{'op':'field','name':'.name'},'right':{'op':'string','value':'Peter'}}");
                AreEqual(".name == 'Peter'",  isEqual.Linq);
                Cosmos  ("c.name = 'Peter'",  isEqual.query.Cosmos);
            } {
                var isNotEqual =        (NotEqual)          FromFilter((Person p) => p.name != "Peter");
                AssertJson(mapper, isNotEqual, "{'op':'notEqual','left':{'op':'field','name':'.name'},'right':{'op':'string','value':'Peter'}}");
                AreEqual(".name != 'Peter'",  isNotEqual.Linq);
                Cosmos  ("c.name != 'Peter'", isNotEqual.query.Cosmos);
            } {
                var isLess =            (LessThan)          FromFilter((Person p) => p.age < 20);
                AssertJson(mapper, isLess, "{'op':'lessThan','left':{'op':'field','name':'.age'},'right':{'op':'int64','value':20}}");
                AreEqual(".age < 20",         isLess.Linq);
                Cosmos  ("c.age < 20",        isLess.query.Cosmos);
            } {            
                var isLessOrEqual =     (LessThanOrEqual)   FromFilter((Person p) => p.age <= 20);
                AssertJson(mapper, isLessOrEqual, "{'op':'lessThanOrEqual','left':{'op':'field','name':'.age'},'right':{'op':'int64','value':20}}");
                AreEqual(".age <= 20",        isLessOrEqual.Linq);
                Cosmos  ("c.age <= 20",       isLessOrEqual.query.Cosmos);
            } {
                var isGreater =         (GreaterThan)       FromFilter((Person p) => p.age > 20);
                AssertJson(mapper, isGreater, "{'op':'greaterThan','left':{'op':'field','name':'.age'},'right':{'op':'int64','value':20}}");
                AreEqual(".age > 20",         isGreater.Linq);
                Cosmos  ("c.age > 20",        isGreater.query.Cosmos);
            } {            
                var isGreaterOrEqual =  (GreaterThanOrEqual)FromFilter((Person p) => p.age >= 20);
                AssertJson(mapper, isGreaterOrEqual, "{'op':'greaterThanOrEqual','left':{'op':'field','name':'.age'},'right':{'op':'int64','value':20}}");
                AreEqual(".age >= 20",        isGreaterOrEqual.Linq);
                Cosmos  ("c.age >= 20",       isGreaterOrEqual.query.Cosmos);
            }
            
            // --- group operations
            {
                var or =    (Or)        FromFilter((Person p) => p.age >= 20 || p.name == "Peter");
                AssertJson(mapper, or, "{'op':'or','operands':[{'op':'greaterThanOrEqual','left':{'op':'field','name':'.age'},'right':{'op':'int64','value':20}},{'op':'equal','left':{'op':'field','name':'.name'},'right':{'op':'string','value':'Peter'}}]}");
                AreEqual(".age >= 20 || .name == 'Peter'",    or.Linq);
                Cosmos  ("c.age >= 20 || c.name = 'Peter'",   or.query.Cosmos);
            } {            
                var and =   (And)       FromFilter((Person p) => p.age >= 20 && p.name == "Peter");
                AssertJson(mapper, and, "{'op':'and','operands':[{'op':'greaterThanOrEqual','left':{'op':'field','name':'.age'},'right':{'op':'int64','value':20}},{'op':'equal','left':{'op':'field','name':'.name'},'right':{'op':'string','value':'Peter'}}]}");
                AreEqual(".age >= 20 && .name == 'Peter'",    and.Linq);
                Cosmos  ("c.age >= 20 && c.name = 'Peter'",   and.query.Cosmos);
            } {            
                var or2 =   (Or)        FromLambda((Person p) => p.age == 1 || p.age == 2 );
                AssertJson(mapper, or2, "{'op':'or','operands':[{'op':'equal','left':{'op':'field','name':'.age'},'right':{'op':'int64','value':1}},{'op':'equal','left':{'op':'field','name':'.age'},'right':{'op':'int64','value':2}}]}");
                AreEqual(".age == 1 || .age == 2",            or2.Linq);
                Cosmos  ("c.age = 1 || c.age = 2",            or2.query.Cosmos);
            } {            
                var and2 =  (And)       FromLambda((Person p) => p.age == 1 && p.age == 2 );
                AssertJson(mapper, and2, "{'op':'and','operands':[{'op':'equal','left':{'op':'field','name':'.age'},'right':{'op':'int64','value':1}},{'op':'equal','left':{'op':'field','name':'.age'},'right':{'op':'int64','value':2}}]}");
                AreEqual(".age == 1 && .age == 2",            and2.Linq);
                Cosmos  ("c.age = 1 && c.age = 2",            and2.query.Cosmos);
            } { 
                var or3 =   (Or)        FromLambda((Person p) => p.age == 1 || p.age == 2 || p.age == 3);
                AssertJson(mapper, or3, "{'op':'or','operands':[{'op':'or','operands':[{'op':'equal','left':{'op':'field','name':'.age'},'right':{'op':'int64','value':1}},{'op':'equal','left':{'op':'field','name':'.age'},'right':{'op':'int64','value':2}}]},{'op':'equal','left':{'op':'field','name':'.age'},'right':{'op':'int64','value':3}}]}");
                AreEqual(".age == 1 || .age == 2 || .age == 3",   or3.Linq);
                Cosmos  ("c.age = 1 || c.age = 2 || c.age = 3",   or3.query.Cosmos);
            }
            
            // --- unary operations
            {
                var isNot = (Not)       FromFilter((Person p) => !(p.age >= 20));
                AssertJson(mapper, isNot, "{'op':'not','operand':{'op':'greaterThanOrEqual','left':{'op':'field','name':'.age'},'right':{'op':'int64','value':20}}}");
                AreEqual("!(.age >= 20)",     isNot.Linq);
                Cosmos  ("!(c.age >= 20)",    isNot.query.Cosmos);
            }
            
            // --- quantifier operations
            {
                var any =   (Any)       FromFilter((Person p) => p.children.Any(child => child.age == 20));
                AssertJson(mapper, any, "{'op':'any','field':{'name':'.children'},'arg':'child','predicate':{'op':'equal','left':{'op':'field','name':'child.age'},'right':{'op':'int64','value':20}}}");
                AreEqual(".children.Any(child => child.age == 20)",                                     any.Linq);
                Cosmos  ("EXISTS(SELECT VALUE child FROM child IN c.children WHERE child.age = 20)",    any.query.Cosmos);
            } { 
                var all =   (All)       FromFilter((Person p) => p.children.All(child => child.age == 20));
                AssertJson(mapper, all, "{'op':'all','field':{'name':'.children'},'arg':'child','predicate':{'op':'equal','left':{'op':'field','name':'child.age'},'right':{'op':'int64','value':20}}}");
                AreEqual(".children.All(child => child.age == 20)", all.Linq);
                Cosmos  ("(SELECT VALUE Count(1) FROM child IN c.children WHERE child.age = 20) = ARRAY_LENGTH(c.children)", all.query.Cosmos);
            }
            
            // --- literals
            {
                var lng     = (LongLiteral)     FromLambda((object p) => 1);
                AreEqual("1",           lng.Linq);
            } { 
                var dbl     = (DoubleLiteral)   FromLambda((object p) => 1.5);
                AreEqual("1.5",         dbl.Linq);
            } {
                var str     = (StringLiteral)   FromLambda((object p) => "hello");
                AreEqual("'hello'",     str.Linq);
            } {
                var @null   = (NullLiteral)     FromLambda((object p) => null);
                AreEqual("null",        @null.Linq);
            }
            
            // --- unary arithmetic operations
            {
                var abs     = (Abs)     FromLambda((object p) => Math.Abs(-1));
                AreEqual("Abs(-1)", abs.Linq);
            } { 
                var ceiling = (Ceiling) FromLambda((object p) => Math.Ceiling(2.5));
                AreEqual("Ceiling(2.5)", ceiling.Linq);
            } { 
                var floor   = (Floor)   FromLambda((object p) => Math.Floor(2.5));
                AreEqual("Floor(2.5)", floor.Linq);
            } { 
                var exp     = (Exp)     FromLambda((object p) => Math.Exp(2.5));
                AreEqual("Exp(2.5)", exp.Linq);
            } { 
                var log     = (Log)     FromLambda((object p) => Math.Log(2.5));
                AreEqual("Log(2.5)", log.Linq);
            } { 
                var sqrt    = (Sqrt)    FromLambda((object p) => Math.Sqrt(2.5));
                AreEqual("Sqrt(2.5)", sqrt.Linq);
            } { 
                var negate  = (Negate)  FromLambda((object p) => -Math.Abs(-1));
                AreEqual("-(Abs(-1))", negate.Linq);
            } { 
                var plus    = (Abs)     FromLambda((object p) => +Math.Abs(-1)); // + will be eliminated
                AreEqual("Abs(-1)", plus.Linq);
            }
            
            // --- binary arithmetic operations
            {
                var add         = (Add)     FromLambda((object p) => 1 + Math.Abs(1.0));
                AreEqual("1 + Abs(1)", add.Linq);
            } {
                var subtract    = (Subtract)FromLambda((object p) => 1 - Math.Abs(1.0));
                AreEqual("1 - Abs(1)", subtract.Linq);
            } {
                var multiply    = (Multiply)FromLambda((object p) => 1 * Math.Abs(1.0));
                AreEqual("1 * Abs(1)", multiply.Linq);
            } {
                var divide      = (Divide)  FromLambda((object p) => 1 / Math.Abs(1.0));
                AreEqual("1 / Abs(1)", divide.Linq);
            } 
            
            // --- unary aggregate operations
            {
                var min      = (Min)  FromLambda((Person p) => p.children.Min(child => child.age));
                AreEqual(".children.Min(child => child.age)", min.Linq);
            } { 
                var max      = (Max)  FromLambda((Person p) => p.children.Max(child => child.age));
                AreEqual(".children.Max(child => child.age)", max.Linq);
            } {
                var sum      = (Sum)  FromLambda((Person p) => p.children.Sum(child => child.age));
                AreEqual(".children.Sum(child => child.age)", sum.Linq);
            } {
                var count    = (CountWhere)  FromLambda((Person p) => p.children.Count(child => child.age == 20));
                AreEqual(".children.Count(child => child.age == 20)", count.Linq);
            } {
                var count    = (Count)  FromLambda((Person p) => p.children.Count()); // () -> method call
                AreEqual(".children.Count()", count.Linq);
            } { 
                var count2   = (Count)  FromLambda((Person p) => p.children.Count); // no () -> Count property 
                AreEqual(".children.Count()", count2.Linq);
            } {
                var average  = (Average)  FromLambda((Person p) => p.children.Average(child => child.age));
                AreEqual(".children.Average(child => child.age)", average.Linq);
            }
            
            // --- binary string operations
            {
                var contains      = (Contains)  FromFilter((object p) => "12345".Contains("234"));
                AreEqual("'12345'.Contains('234')", contains.Linq);
            } {
                var startsWith    = (StartsWith)  FromFilter((object p) => "12345".StartsWith("123"));
                AreEqual("'12345'.StartsWith('123')", startsWith.Linq);
            } {
                var endsWith      = (EndsWith)  FromFilter((object p) => "12345".EndsWith("345"));
                AreEqual("'12345'.EndsWith('345')", endsWith.Linq);
            }
          } 
        }
        
        [Test]
        public static void TestField() {
          using (var mapper = new ObjectMapper()) {
            {
                var is20 =          (Equal)          FromFilter((Person p) => p.age == 20);
                AreEqual(".age == 20", is20.Linq);
            } {
                var isSf =          (Equal)          FromFilter((Person p) => p.address.cityName == "San Francisco");
                AreEqual(".address.city == 'San Francisco'", isSf.Linq);
            } {
                var isLombardSt =   (Equal)          FromFilter((Person p) => p.address.street.name == "Lombard St");
                AreEqual(".address.street.name == 'Lombard St'", isLombardSt.Linq);
            }
          }
        }
    }
    
    internal static class AssertEqual {
        internal static void Cosmos (string expect, string was) {
            AreEqual(expect, was);
        }
        
    }
        
}