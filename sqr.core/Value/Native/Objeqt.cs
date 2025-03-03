﻿using Newtonsoft.Json;
using Qrakhen.SqrDI;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Qrakhen.Sqr.Core
{
    public class Objeqt : ItemSet
    {
        public Storage<string, Variable> properties = new Storage<string, Variable>();

        [NativeField]
        public override Number length() => properties.count;

        public Objeqt() : base(Type.Objeqt)
        {

        }

        public override Value accessMember(Value name)
        {
            var member = base.accessMember(name);
            string key = name as String;
            if (member == Null) {
                if (properties.contains(key))
                    return properties[key];
                else
                    return Null;
            }
            return member;
        }

        [NativeMethod]
        public override Value get(Value index)
        {
            return properties[(string)index.raw]?.obj;
        }

        [NativeMethod]
        public override void set(Value index, Value value)
        {
            var key = (string)index.raw;
            if (!properties.contains(key))
                properties[key] = new Variable();
            properties[key].set(value);
        }

        public override string ToString()
        {
            return render();
        }

        public string render(int __level = 0)
        {
            var ident = "".PadLeft(__level * 2);
            var r = ident + "{\n";
            foreach (var p in properties) {
                var v = p.Value.ToString();
                if (p.Value.obj is Objeqt)
                    v = (p.Value.obj as Objeqt).render(__level++);
                r += ident + "  " + p.Key + ": " + v + "\n";
            }
            return r + ident + "}";
        }
    }
}
