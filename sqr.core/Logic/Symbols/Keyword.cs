﻿using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Qrakhen.Sqr.Core
{
    public class Keyword : ITyped<Keyword.Type>
    {
        private static readonly Storage<Type, Keyword> keywords = new Storage<Type, Keyword>();

        public string symbol;
        public Func<object, Stack<Token>> resolve;
        public List<string> aliases = new List<string>();

        public Keyword(Type type, string symbol)
        {
            this.type = type;
            this.symbol = symbol;
            alias(symbol);
        }

        public Keyword alias(string alias)
        {
            aliases.Add(alias);
            return this;
        }

        public override string ToString()
        {
            return symbol;
        }

        public static Keyword get(string symbol)
        {
            if (symbol.StartsWith("@")) return keywords[Type.DECLARE_TYPED];
            return keywords.findOne(_ => _.aliases.Contains(symbol));
        }

        public static Keyword get(Type type)
        {
            return keywords[type];
        }

        [Flags]
        public enum Type
        {
            DECLARE_DYN = 1,
            DECLARE_REF = 2,
            DECLARE_TYPED = 4,
            DECLARE_FUNQTION = 8,
            DECLARE_QLASS = 16,
            DECLARE = DECLARE_DYN | DECLARE_REF | DECLARE_FUNQTION | DECLARE_QLASS,
            IMPORT = 64,
            QONDITION_IF = 128,
            QONDITION_ELSE = 256,
            LOOP_FOR = 512,
            LOOP_WHILE = 1024,
            LOOP_DO = 2049,
            FUNQTION_RETURN = 4096,
        }

        public static Keyword register(Type type, string symbol)
        {
            var word = new Keyword(type, symbol);
            keywords.Add(type, word);
            return word;
        }

        static Keyword()
        {
            register(Type.DECLARE_DYN, "var"); 
            register(Type.DECLARE_REF, "ref DISCONTINUED, use var& name instead");
            register(Type.DECLARE_TYPED, "@");
            register(Type.DECLARE_FUNQTION, "funqtion")
                .alias("funq")
                .alias("fn");
            register(Type.DECLARE_QLASS, "qlass");
            register(Type.IMPORT, "import");
            register(Type.QONDITION_IF, "if");
            register(Type.QONDITION_ELSE, "else");
            register(Type.LOOP_FOR, "for");
            register(Type.LOOP_WHILE, "while");
            register(Type.LOOP_DO, "do");
            register(Type.FUNQTION_RETURN, "return");
        }
    }
}
