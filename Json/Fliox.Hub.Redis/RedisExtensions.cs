// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using Friflo.Json.Fliox.Transform;
using Friflo.Json.Fliox.Transform.Query.Ops;

namespace Friflo.Json.Fliox.Hub.Redis
{
    public static class RedisExtensions
    {
        public static string RedisFilter(this FilterOperation op) {
            var cx      = new ConvertContext("c", op);
            var result  = cx.Traverse(op);
            return result;
        }
    }
    
    internal sealed class ConvertContext {
        private readonly   string           collection;
        private readonly   string           collectionStart;
        private readonly   FilterOperation  filterOp;
        
        internal ConvertContext (string collection, FilterOperation filterOp) {
            this.collection = collection;
            if (filterOp is Filter filter) {
                collectionStart = $"{filter.arg}.";
            }
            this.filterOp   = filterOp;
        }
        
        /// <summary>
        /// Create CosmosDB query filter specified at: 
        /// https://github.com/friflo/Friflo.Json.Fliox/tree/main/Json/Fliox.Hub/Client#query-filter
        /// </summary>
        internal string Traverse(Operation operation) {
            switch (operation) {
                case Field field: {
                    if (collectionStart != null && field.name.StartsWith(collectionStart)) {
                        var fieldName = field.name.Substring(collectionStart.Length);
                        return $"JSON_VALUE(data,'$.{fieldName}')";
                    }
                    throw new InvalidOperationException($"expect field {field.name} starts with {collectionStart}");
                }
                
                // --- literal --- 
                case StringLiteral stringLiteral:
                    return $"'{stringLiteral.value}'";
                case DoubleLiteral doubleLiteral:
                    return doubleLiteral.value.ToString(CultureInfo.InvariantCulture);
                case LongLiteral longLiteral:
                    return longLiteral.value.ToString();
                case TrueLiteral    _:
                    return "true";
                case FalseLiteral   _:
                    return "false";
                case NullLiteral    _:
                    return "null";
                
                // --- compare ---
                case Equal equal:
                    var left    = Traverse(equal.left);
                    var right   = Traverse(equal.right);
                    // e.g. WHERE json_extract(data,'$.int32') is null || JSON_TYPE(json_extract(data,'$.int32')) = 'NULL'
                    if (left  == "null") return $"({right} is null)";
                    if (right == "null") return $"({left} is null)";
                    return $"{left} = {right}";
                case NotEqual notEqual:
                    left    = Traverse(notEqual.left);
                    right   = Traverse(notEqual.right);
                    // e.g WHERE json_extract(data,'$.int32') is not null && JSON_TYPE(json_extract(data,'$.int32')) != 'NULL'
                    if (left  == "null") return $"({right} is not null)";
                    if (right == "null") return $"({left} is not null)";
                    return $"({left} is null or {right} is null or {left} != {right})";
                case Less lessThan:
                    left    = Traverse(lessThan.left);
                    right   = Traverse(lessThan.right);
                    return $"{left} < {right}";
                case LessOrEqual lessThanOrEqual:
                    left    = Traverse(lessThanOrEqual.left);
                    right   = Traverse(lessThanOrEqual.right);
                    return $"{left} <= {right}";
                case Greater greaterThan:
                    left    = Traverse(greaterThan.left);
                    right   = Traverse(greaterThan.right);
                    return $"{left} > {right}";
                case GreaterOrEqual greaterThanOrEqual:
                    left    = Traverse(greaterThanOrEqual.left);
                    right   = Traverse(greaterThanOrEqual.right);
                    return $"{left} >= {right}";
                
                // --- logical ---
                case Not @not:
                    var operand = Traverse(@not.operand);
                    return $"NOT({operand})";
                case Or or:
                    var operands = GetOperands(or.operands);
                    return string.Join(" OR ", operands);
                case And and:
                    operands = GetOperands(and.operands);
                    return string.Join(" AND ", operands);
                
                // --- string ---
                case StartsWith startsWith:
                    left    = Traverse(startsWith.left);
                    right   = Traverse(startsWith.right);
                    return $"{left} LIKE '{UnString(right)}%'";
                case EndsWith endsWith:
                    left    = Traverse(endsWith.left);
                    right   = Traverse(endsWith.right);
                    return $"{left} LIKE '%{UnString(right)}'";
                case Contains contains:
                    left    = Traverse(contains.left);
                    right   = Traverse(contains.right);
                    return $"{left} LIKE '%{UnString(right)}%'";
                case Length length:
                    var value = Traverse(length.value);
                    return $"LENGTH({value})";
                
                // --- arithmetic: operators ---
                case Add add:
                    left    = Traverse(add.left);
                    right   = Traverse(add.right);
                    return $"{left} + {right}";
                case Subtract subtract:
                    left    = Traverse(subtract.left);
                    right   = Traverse(subtract.right);
                    return $"{left} - {right}";
                case Multiply multiply:
                    left    = Traverse(multiply.left);
                    right   = Traverse(multiply.right);
                    return $"{left} * {right}";
                case Divide divide:
                    left    = Traverse(divide.left);
                    right   = Traverse(divide.right);
                    return $"{left} / {right}";
                case Modulo modulo:
                    left    = Traverse(modulo.left);
                    right   = Traverse(modulo.right);
                    return $"{left} % {right}";
                
                // --- arithmetic: methods ---
                case Abs abs:
                    value = Traverse(abs.value);
                    return $"ABS({value})";
                case Ceiling ceiling:
                    value = Traverse(ceiling.value);
                    return $"ROUND({value}+0.5)";
                case Floor floor:
                    value = Traverse(floor.value);
                    return $"ROUND({value}-0.5)";
                case Exp exp:
                    value = Traverse(exp.value);
                    return $"EXP({value})";
                case Log log:
                    value = Traverse(log.value);
                    return $"LOG({value})";
                case Sqrt sqrt:
                    value = Traverse(sqrt.value);
                    return $"SQRT({value})";
                
                // --- constants ---
                case PiLiteral:
                    return "PI()";
                case EulerLiteral:
                    return "EXP(1)";
                
                // --- aggregate ---
                case CountWhere countWhere: {
                    var cx              = new ConvertContext ("", filterOp);
                    operand             = cx.Traverse(countWhere.predicate);
                    string fieldName    = Traverse(countWhere.field);
                    string arg          = countWhere.arg;
                    return $"(SELECT VALUE Count(1) FROM {arg} IN {fieldName} WHERE {operand})";
                }
                // --- quantify ---
                case Any any: {
                    var cx              = new ConvertContext ("", filterOp);
                    operand             = cx.Traverse(any.predicate);
                    string fieldName    = Traverse(any.field);
                    var arg             = any.arg;
                    return $"EXISTS(SELECT VALUE {arg} FROM {arg} IN {fieldName} WHERE {operand})";
                }
                case All all: {
                    var cx              = new ConvertContext ("", filterOp);
                    operand             = cx.Traverse(all.predicate);
                    var fieldName       = Traverse(all.field);
                    var arg             = all.arg;
                    // treat array == null and missing array as empty array <=> array[]
                    return $"IS_NULL({fieldName}) OR NOT IS_DEFINED({fieldName}) OR (SELECT VALUE Count(1) FROM {arg} IN {fieldName} WHERE {operand}) = ARRAY_LENGTH({fieldName})";
                }
                // --- query filter expression
                case Filter filter: {
                    var cx              = new ConvertContext (collection, filterOp);
                    operand             = cx.Traverse(filter.body);
                    return $"{operand}";
                }
                default:
                    throw new NotImplementedException($"missing conversion for operation: {operation}, filter: {filterOp}");
            }
        }
        
        private string ToBoolean(string operand) {
            return operand;
        }
        
        private string[] GetOperands (List<FilterOperation> operands) {
            var result = new string[operands.Count];
            for (int n = 0; n < operands.Count; n++) {
                var operand = Traverse(operands[n]);
                operand     = ToBoolean(operand);
                result[n]   = operand;
            }
            return result;
        }
        
        private static string UnString(string value) {
            if (value[0] == '\'') {
                return value.Substring(1, value.Length - 2);
            }
            return value;
        }
    }
}