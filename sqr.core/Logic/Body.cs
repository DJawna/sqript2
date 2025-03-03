﻿using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;


using static Qrakhen.Sqr.Core.Operation;

namespace Qrakhen.Sqr.Core
{
    internal class Body
    {
        private static readonly OperationResolver operationResolver = 
            SqrDI.Dependor.get<OperationResolver>(); // use static fields for these kinds of injectors

        protected readonly Token[] content;

        public Body(Token[] content)
        {
            this.content = content;
        }

        internal Stack<Token> getStack()
        {
            return new Stack<Token>(content);
        }

        static Operation _ = null; 
        public void execute(Qontext qontext, JumpCallback callback)
        {
            var stack = getStack();
            var statement = Statement.None;
            var result = Value.Void;
            string jumpTarget = null;
            JumpCallback localCallback = (v, s, t) => { 
                result = v; 
                statement = s;
                jumpTarget = t;
            };
            while (!stack.done) {
                var op = operationResolver.resolveOne(stack, qontext);
                if (_ == null) _ = op;
                op.execute(localCallback, qontext);
                if (statement != Statement.None) {
                    callback?.Invoke(result, statement, jumpTarget);
                    return;
                }
            }
            callback?.Invoke(Value.Void, Statement.None);
        }
    }
}
