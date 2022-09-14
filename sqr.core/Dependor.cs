﻿using Qrakhen.Sqr.Core;
using System;
using System.Linq;
using System.Reflection;

namespace Qrakhen.Dependor
{  
    public abstract class Injector
    {
        protected Injector()
        {
            Dependor.apply(this);
        }
    }

    public static class Dependor
    {
        private static Storage<Type, object> injectables = new Storage<Type, object>();

        static Dependor()
        {
            build();
        }

        public static T get<T>() where T : class
        {
            if (!injectables.ContainsKey(typeof(T)))
                return default(T);

            return injectables[typeof(T)] as T;
        }

        public static void build()
        {
            instantiateInjectables();
        }

        private static void instantiateInjectables()
        {
            var types = AppDomain
               .CurrentDomain
               .GetAssemblies()
               .SelectMany(t => t.GetTypes())
               .Where(
                   t => t.IsClass &&
                   t.GetCustomAttributes(typeof(InjectableAttribute), true).Length > 0);

            types.ToList().ForEach(Console.WriteLine);

            foreach (var t in types) {
                if (injectables.ContainsKey(t))
                    continue;

                if (t.GetConstructor(Type.EmptyTypes) == null)
                    throw new Exception("DI Error: " + t.Name + " needs to have either no, or one empty public constructor.");

                injectables.Add(t, Activator.CreateInstance(t));
            }

            foreach (var i in injectables.Values) {
                apply(i);
            }
        }

        public static void apply(object instance)
        {
            var dependencies = instance.GetType()
                .GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(f => injectables.ContainsKey(f.FieldType));

            foreach (var f in dependencies) {
                if (Attribute.GetCustomAttribute(f, typeof(NoInjectAttribute)) != null)
                    continue;

                if (f.GetValue(instance) == null)
                    f.SetValue(instance, injectables[f.FieldType]);
            }
        }
    }

    public class InjectableAttribute : Attribute
    {

    }

    public class NoInjectAttribute : Attribute
    {

    }
}
