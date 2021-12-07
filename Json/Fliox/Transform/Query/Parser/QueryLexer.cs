// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Globalization;

namespace Friflo.Json.Fliox.Transform.Query.Parser
{
    public static class QueryLexer
    {
        private const int End = -1;
        
        public static TokenList Tokenize(string operation, out string error) {
            var     tokens      = new List<Token>();
            int     pos         = 0;
            var     lastType    = TokenType.Start;
            while (true) {
                var token = GetToken(lastType, operation, ref pos, out error);
                switch (token.type) {
                    case TokenType.End:
                        return new TokenList(tokens.ToArray());
                    case TokenType.Whitespace:
                        break;
                    default:
                        lastType = token.type;
                        tokens.Add(token);
                        break;
                }
            }
        }
        
        private static Token GetToken(TokenType lastType, string operation, ref int pos, out string error) {
            int c = GetChar(operation, pos++);
            error = null;
            switch (c) {
                case '+':
                    if (!IsOperand(lastType)) {
                        c = GetChar(operation, pos);
                        if (IsDigit(c))
                            return GetNumber(false, operation, ref pos, out error);
                    }
                    return new Token(TokenType.Add);
                case '-':
                    if (!IsOperand(lastType)) {
                        c = GetChar(operation, pos);
                        if (IsDigit(c))
                            return GetNumber(true, operation, ref pos, out error);
                    }
                    return new Token(TokenType.Sub);
                case '*':   return new Token(TokenType.Mul);
                case '/':   return new Token(TokenType.Div);
                case '.':   return new Token(TokenType.Dot);
                case '(':   return new Token(TokenType.BracketOpen);
                case ')':   return new Token(TokenType.BracketClose);
                case '>':   
                    c = GetChar(operation, pos);
                    if (c == '=') {
                            pos++; return new Token(TokenType.GreaterOrEqual);
                    }
                    return new Token(TokenType.Greater);
                case '<':
                    c = GetChar(operation, pos);
                    if (c == '=') {
                            pos++; return new Token(TokenType.LessOrEqual);
                    }
                    return new Token(TokenType.Less);
                case '!':
                    c = GetChar(operation, pos);
                    if (c == '=') {
                            pos++; return new Token(TokenType.NotEquals);
                    }
                    return new Token(TokenType.Not);
                case '=':
                    c = GetChar(operation, pos);
                    if (c == '=') {
                            pos++; return new Token(TokenType.Equals);
                    }
                    if (c == '>') {
                            pos++; return new Token(TokenType.Arrow);
                    }
                    error = $"unexpected character: '${(char)c}'";
                    return new Token(TokenType.Error);
                case '|':
                    c = GetChar(operation, pos);
                    if (c == '|') {
                            pos++; return new Token(TokenType.Or);
                    }
                    error = $"expect character '|'. was: '${(char)c}'";
                    return new Token(TokenType.Error);
                case '&':
                    c = GetChar(operation, pos);
                    if (c == '&') {
                            pos++; return new Token(TokenType.And);
                    }
                    error = $"expect character '&'. was: '${(char)c}'";
                    return new Token(TokenType.Error);
                case '\'':
                    return GetString(operation, ref pos, out error);
                case ' ':
                case '\t':
                case '\r':
                case '\n':
                    return GetWhitespace(operation, ref pos);
                case End:
                    pos--;
                    return new Token(TokenType.End);
                default:
                    pos--;
                    return GetSymbol(c, operation, ref pos, out error);
            }
        }

        private static int GetChar(string operation, int pos) {
            if (pos < operation.Length) {
                return operation[pos];
            }
            return End;
        }
        
        private static bool IsChar(int c) {
            return  'a' <= c && c <= 'z' ||
                    'A' <= c && c <= 'Z' ||
                    '_' == c;
        }
        
        private static bool IsDigit(int c) {
            return  '0' <= c && c <= '9';
        }
        
        private static bool IsOperand(TokenType type) {
            return type == TokenType.Symbol || type == TokenType.Long || type == TokenType.Double || type == TokenType.BracketClose;
        }

        private static Token GetSymbol(int c, string operation, ref int pos, out string error) {
            if (IsDigit(c)) {
                return GetNumber (false, operation, ref pos, out error);
            }
            if (IsChar(c)) {
                var start = pos;
                while (true) {
                    pos++;
                    c = GetChar(operation, pos);
                    if (IsChar(c))
                        continue;
                    var str = operation.Substring(start, pos - start);
                    error = null;
                    return new Token(TokenType.Symbol, str);
                }
            }
            error = $"unexpected character: '${(char)c}'";
            return default;
        }
        
        private static Token GetNumber(bool negative, string operation, ref int pos, out string error) {
            int     start = pos++;
            bool    isFloat = false;
            while (true) {
                int c = GetChar(operation, pos);
                if (IsDigit(c)) {
                    pos++;
                    continue;
                }
                if ('.' == c) {
                    if (isFloat) {
                        error = "invalid floating point number";
                        return new Token(TokenType.Error);
                    }
                    pos++;
                    isFloat = true;
                    continue;
                }
                break;
            }
            error = null;
            var str = operation.Substring(start, pos - start);
            if (isFloat) {
                double dbl = double.Parse(str, NumberStyles.Float, NumberFormatInfo.InvariantInfo);
                dbl = negative ? -dbl : dbl;
                return new Token(dbl);
            }
            long lng = long.Parse(str);
            lng = negative ? -lng : lng;
            return new Token(lng);
        }
        
        private static Token GetString(string operation, ref int pos, out string error) {
            int start = pos;
            while (true) {
                int c = GetChar(operation, pos);
                if (c == End) {
                    error = "missing string terminator '\"'";
                    return new Token(TokenType.Error);
                }
                if (c == '\'') {
                    error = null;
                    var str = operation.Substring(start, pos - start);
                    pos++;
                    return new Token(TokenType.String, str);
                }
                pos++;
            }
        }
        
        private static Token GetWhitespace(string operation, ref int pos) {
            while (true) {
                int c = GetChar(operation, pos);
                switch (c) {
                    case ' ':   case '\t':  case '\r':  case '\n':
                        pos++;
                        continue;
                }
                return new Token(TokenType.Whitespace);
            }
        }
    }
}