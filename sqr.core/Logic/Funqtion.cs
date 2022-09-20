﻿using Newtonsoft.Json;
using Qrakhen.Dependor;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Qrakhen.Sqr.Core
{
    public class Funqtion
    {
        private static readonly OperationResolver operationResolver = Dependor.Dependor.get<OperationResolver>();

        public readonly DeclaredParam[] parameters = new DeclaredParam[0];
        public readonly Type returnType;
        public readonly Body body;

        protected Funqtion() { }

        public Funqtion(Body body, DeclaredParam[] parameters, Type returnType = null)
        {
            this.body = body;
            this.parameters = parameters;
            this.returnType = returnType;
        }
        
        public virtual Value execute(Value[] parameters, Qontext qontext, Value self = null)
        {
            var eq = createExecutionQontext(parameters, qontext);
            if (self != null)
                eq.register("this", self);

            return body.execute(eq);
        }

        protected Qontext createExecutionQontext(Value[] parameters, Qontext qontext)
        {
            var tempQontext = new Qontext(qontext);

            for (int i = 0; i < this.parameters.Length; i++) {
                var p = this.parameters[i];
                if (parameters.Length <= i) {
                    if (p.optional) break;
                    else throw new SqrError("parameter " + p.name + " missing");
                } 
                tempQontext.register(this.parameters[i].name, new Variable(parameters[i]));
            }

            return tempQontext;
        }

        public struct DeclaredParam
        {
            public string name;
            public NativeType type;
            public Value defaultValue;
            public bool optional;
        }
    }

    public class InternalFunqtion : Funqtion
    {
        protected Func<Value[], Value, Value> callback;

        public InternalFunqtion(Func<Value[], Value, Value> callback)
        {
            this.callback = callback;
        }

        public override Value execute(Value[] parameters, Qontext qontext, Value self = null)
        {
            return callback(parameters, self);
        }
    }
}
