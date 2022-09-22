﻿using Qrakhen.SqrDI;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

namespace Qrakhen.Sqr.Core
{  
    [Injectable]
    internal class TokenResolver : Resolver<Stack<char>, Stack<Token>>
    {
        private readonly Logger log;

        public Stack<Token> resolve(Stack<char> input)
        {
            log.debug("in " + GetType().Name);
            var result = new List<Token>();
            long row = 0, count = 0, __prev = 0, __end;
            while (!input.done) {
                var pos = input.index;
                if (input.peek() == '\n') {
                    row++;
                    __prev = pos;
                }
                var type = matchType(input.peek());
                var value = readValue(type, input);
                __end = input.index;
                if (value != null) {
                    var token = Token.create(value, type);
                    token.__row = row;
                    token.__col = pos - __prev;
                    token.__pos = pos;
                    token.__end = __end;
                    result.Add(token);
                }
            }
            log.spam(string.Join(", ", result.Select(_ => _.type + ": '" + _.raw + "'")));
            count = input.length;
            return new Stack<Token>(result.ToArray());
        }

        private string readString(Stack<char> input)
        {
            var start = input.digest();
            string buffer = "";

            do {
                buffer += new string(input.digestUntil(start));
                if (buffer.EndsWith('\\')) // escapes
                    buffer = buffer.Substring(0, buffer.Length - 1) + input.digest();
                else
                    break;
            } while (!input.done);

            if (input.digest() != start) // toss last ' or " and check if string is finished
                throw new SqrError("string without end detected! position: " + (input.index - buffer.Length) + ", content: " + buffer);

            return buffer;
        }

        private string readType(Stack<char> input, Token.Type type)
        {
            string buffer = "";
            while (matchType(input.peek()) == type) {
                buffer += input.digest();
                if (input.done)
                    break;
            }
            return buffer;
        }

        private string readIdentifier(Stack<char> input)
        {
            string buffer = "";
            while (
                    matchType(input.peek()) == Token.Type.Identifier ||
                    Regex.IsMatch(input.peek().ToString(), @"\d")) {
                buffer += input.digest();
                if (input.done)
                    break;
            }
            return buffer;
        }

        private string readValue(Token.Type type, Stack<char> input)
        {
            if (type == Token.Type.Comment) {
                input.digestUntil('\n');
                return null;
            }

            if (type == Token.Type.Whitespace) {
                input.digest();
                return null;
            }

            if (type == Token.Type.String)
                return readString(input);

            if (type == Token.Type.Type) { 
                var r = input.digest() + readType(input, Token.Type.Identifier);
                return r;
            }

            if (type == Token.Type.Identifier)
                return readIdentifier(input);

            string buffer = "";
            while (type == matchType(input.peek())) {
                buffer += input.digest();
                if (input.done || type == Token.Type.Structure)
                    break;
            }

            return buffer;
        }

        private Token.Type matchType(char input)
        {
            foreach (var m in matches) {
                if (Regex.IsMatch(input.ToString(), m.Value))
                    return m.Key;
            }
            return Token.Type.Identifier;
        }

        static readonly private Dictionary<Token.Type, string> matches = new Dictionary<Token.Type, string>() {
            { Token.Type.Operator, @"[\/\-\*+=&<>^?!~]" },
            { Token.Type.Number, @"[\d.]" },
            { Token.Type.String, "[\"']" },
            { Token.Type.Structure, @"[{}()[\],]" },
            { Token.Type.End, @";" },
            { Token.Type.Accessor, @"[:]" },
            { Token.Type.Type, "@" },
            { Token.Type.Whitespace, @"\s" },
            { Token.Type.Comment, @"#" },
        };
    }

    public class Token : ITyped<Token.Type>
    {
        public const string end = ";";

        public long __row, __col, __pos = -1, __end = -1;

        public readonly string raw;
        public readonly object value;
        public new Type type => base.type;

        private Token(object value, Type type, string raw)
        {
            this.value = value;
            base.type = type;
            this.raw = raw;
        }

        public T get<T>()
        {
            if (!(value is T))
                return default(T);

            return (T)value;
        }

        public Value makeValue()
        {
            if (!isType(Type.Value))
                throw new SqrError("can not make value out of token: not a value token" + this, this);

            if (type == Type.Boolean) return new Boolean((bool)value);
            if (type == Type.Float) return new Number((float)value);
            if (type == Type.Number) return new Number((double)value);
            if (type == Type.String) return new String((string)value);
            throw new SqrError("no known native type applied to token " + this, this);
        }

        public Variable makeVariable(bool isReference = false, Core.Type strictType = null, bool isReadonly = false)
        {
            if (!isType(Type.Identifier))
                throw new SqrError("can not make variable out of token: not an identifier token" + this, this);

            return new Variable(null, isReference, strictType, isReadonly);
        }

        public NativeType asNativeType(Type type)
        {
            if (type == Type.Boolean) return NativeType.Boolean;
            if (type == Type.Float) return NativeType.Float;
            if (type == Type.Number) return NativeType.Number;
            if (type == Type.String) return NativeType.String;
            if (type == Type.Identifier) return NativeType.Variable;
            return NativeType.None;
        }

        public static Token create(string raw, Type type)
        {
            Type parsedType;
            var value = parse(raw, type, out parsedType);

            if (value == null)
                throw new SqrError("could not parse value " + raw + ", it's not a known " + type);

            return new Token(value, parsedType, raw);
        }

        public static object parse(string raw, Type type, out Type parsedType)
        {
            try {
                parsedType = type;
                if (raw == "true" || raw == "false") {
                    parsedType = Type.Boolean;
                    return (raw == "true" ? true : false);
                }
                if (type == Type.Number) {
                    parsedType = Type.Number;
                    return double.Parse(raw, System.Globalization.NumberFormatInfo.InvariantInfo);
                }
                if (type == Type.Operator) {
                    parsedType = Type.Operator;
                    return Operator.get(raw);
                }
                if (type == Type.Structure) {
                    parsedType = Type.Structure;
                    return Structure.get(raw); 
                }
                if (raw.StartsWith("@")) {
                    parsedType = Type.Type;
                    return Core.Type.get(raw.Substring(1));
                }
                if (type == Type.Keyword || type == Type.Type || type == Type.Identifier) {
                    var v = Keyword.get(raw);
                    if (v != null) {
                        parsedType = Type.Keyword;
                        return v;
                    }
                    var t = Core.Type.get(raw);
                    if (t != null) { 
                        parsedType = Type.Type;
                        return t;
                    }
                }
                return raw;
            } catch (Exception e) {
                throw new SqrError("trying to parse raw token value " + raw + " as " + type + ". didn't work.");
            }
        }

        public override string ToString()
        {
            return type + " [ " + raw + " ] @" + __row + ":" + __col + ", p" + __pos;
        }

        [Flags]
        public enum Type
        {
            Operator = 1,
            Boolean = 2,
            Float = 4,
            Number = 8,
            String = 16,
            Structure = 32,
            Accessor = 64,
            Keyword = 128,
            Identifier = 256,
            Whitespace = 512,
            Comment = 1024,
            End = 2048,
            Type = 4096,

            Value = Boolean | Float | Number | String | Identifier
        }
    }
}
