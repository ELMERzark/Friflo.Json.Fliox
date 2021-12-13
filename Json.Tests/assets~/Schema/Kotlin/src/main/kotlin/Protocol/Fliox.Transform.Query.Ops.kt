// Generated by: https://github.com/friflo/Friflo.Json.Fliox/tree/main/Json/Fliox/Schema
package Fliox.Transform.Query.Ops

import kotlinx.serialization.*
import CustomSerializer.*
import Fliox.Transform.*

@Serializable
@SerialName("equal")
data class Equal (
    override  val left  : Operation,
    override  val right : Operation,
) : BinaryBoolOp()

@Serializable
abstract class BinaryBoolOp {
    abstract  val left  : Operation
    abstract  val right : Operation
}

@Serializable
@SerialName("field")
data class Field (
              val name : String,
) : Operation()

@Serializable
@SerialName("string")
data class StringLiteral (
              val value : String,
) : Literal()

@Serializable
abstract class Literal {
}

@Serializable
@SerialName("double")
data class DoubleLiteral (
              val value : Double,
) : Literal()

@Serializable
@SerialName("int64")
data class LongLiteral (
              val value : Long,
) : Literal()

@Serializable
@SerialName("null")
class NullLiteral (
) : Literal()

@Serializable
@SerialName("abs")
data class Abs (
    override  val value : Operation,
) : UnaryArithmeticOp()

@Serializable
abstract class UnaryArithmeticOp {
    abstract  val value : Operation
}

@Serializable
@SerialName("ceiling")
data class Ceiling (
    override  val value : Operation,
) : UnaryArithmeticOp()

@Serializable
@SerialName("floor")
data class Floor (
    override  val value : Operation,
) : UnaryArithmeticOp()

@Serializable
@SerialName("exp")
data class Exp (
    override  val value : Operation,
) : UnaryArithmeticOp()

@Serializable
@SerialName("log")
data class Log (
    override  val value : Operation,
) : UnaryArithmeticOp()

@Serializable
@SerialName("sqrt")
data class Sqrt (
    override  val value : Operation,
) : UnaryArithmeticOp()

@Serializable
@SerialName("negate")
data class Negate (
    override  val value : Operation,
) : UnaryArithmeticOp()

@Serializable
@SerialName("add")
data class Add (
    override  val left  : Operation,
    override  val right : Operation,
) : BinaryArithmeticOp()

@Serializable
abstract class BinaryArithmeticOp {
    abstract  val left  : Operation
    abstract  val right : Operation
}

@Serializable
@SerialName("subtract")
data class Subtract (
    override  val left  : Operation,
    override  val right : Operation,
) : BinaryArithmeticOp()

@Serializable
@SerialName("multiply")
data class Multiply (
    override  val left  : Operation,
    override  val right : Operation,
) : BinaryArithmeticOp()

@Serializable
@SerialName("divide")
data class Divide (
    override  val left  : Operation,
    override  val right : Operation,
) : BinaryArithmeticOp()

@Serializable
@SerialName("min")
data class Min (
    override  val field : Field,
    override  val arg   : String,
    override  val array : Operation,
) : BinaryAggregateOp()

@Serializable
abstract class BinaryAggregateOp {
    abstract  val field : Field
    abstract  val arg   : String
    abstract  val array : Operation
}

@Serializable
@SerialName("max")
data class Max (
    override  val field : Field,
    override  val arg   : String,
    override  val array : Operation,
) : BinaryAggregateOp()

@Serializable
@SerialName("sum")
data class Sum (
    override  val field : Field,
    override  val arg   : String,
    override  val array : Operation,
) : BinaryAggregateOp()

@Serializable
@SerialName("average")
data class Average (
    override  val field : Field,
    override  val arg   : String,
    override  val array : Operation,
) : BinaryAggregateOp()

@Serializable
@SerialName("count")
data class Count (
    override  val field : Field,
) : UnaryAggregateOp()

@Serializable
abstract class UnaryAggregateOp {
    abstract  val field : Field
}

@Serializable
@SerialName("notEqual")
data class NotEqual (
    override  val left  : Operation,
    override  val right : Operation,
) : BinaryBoolOp()

@Serializable
@SerialName("lessThan")
data class LessThan (
    override  val left  : Operation,
    override  val right : Operation,
) : BinaryBoolOp()

@Serializable
@SerialName("lessThanOrEqual")
data class LessThanOrEqual (
    override  val left  : Operation,
    override  val right : Operation,
) : BinaryBoolOp()

@Serializable
@SerialName("greaterThan")
data class GreaterThan (
    override  val left  : Operation,
    override  val right : Operation,
) : BinaryBoolOp()

@Serializable
@SerialName("greaterThanOrEqual")
data class GreaterThanOrEqual (
    override  val left  : Operation,
    override  val right : Operation,
) : BinaryBoolOp()

@Serializable
@SerialName("and")
data class And (
    override  val operands : List<FilterOperation>,
) : BinaryLogicalOp()

@Serializable
abstract class BinaryLogicalOp {
    abstract  val operands : List<FilterOperation>
}

@Serializable
@SerialName("or")
data class Or (
    override  val operands : List<FilterOperation>,
) : BinaryLogicalOp()

@Serializable
@SerialName("true")
class TrueLiteral (
) : FilterOperation()

@Serializable
@SerialName("false")
class FalseLiteral (
) : FilterOperation()

@Serializable
@SerialName("not")
data class Not (
    override  val operand : FilterOperation,
) : UnaryLogicalOp()

@Serializable
abstract class UnaryLogicalOp {
    abstract  val operand : FilterOperation
}

@Serializable
@SerialName("lambda")
data class Lambda (
              val arg  : String,
              val body : Operation,
) : Operation()

@Serializable
@SerialName("filter")
data class Filter (
              val arg  : String,
              val body : FilterOperation,
) : FilterOperation()

@Serializable
@SerialName("any")
data class Any (
    override  val field     : Field,
    override  val arg       : String,
    override  val predicate : FilterOperation,
) : BinaryQuantifyOp()

@Serializable
abstract class BinaryQuantifyOp {
    abstract  val field     : Field
    abstract  val arg       : String
    abstract  val predicate : FilterOperation
}

@Serializable
@SerialName("all")
data class All (
    override  val field     : Field,
    override  val arg       : String,
    override  val predicate : FilterOperation,
) : BinaryQuantifyOp()

@Serializable
@SerialName("countWhere")
data class CountWhere (
              val field     : Field,
              val arg       : String,
              val predicate : FilterOperation,
) : Operation()

@Serializable
@SerialName("contains")
data class Contains (
    override  val left  : Operation,
    override  val right : Operation,
) : BinaryBoolOp()

@Serializable
@SerialName("startsWith")
data class StartsWith (
    override  val left  : Operation,
    override  val right : Operation,
) : BinaryBoolOp()

@Serializable
@SerialName("endsWith")
data class EndsWith (
    override  val left  : Operation,
    override  val right : Operation,
) : BinaryBoolOp()

