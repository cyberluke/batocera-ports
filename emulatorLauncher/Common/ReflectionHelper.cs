﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace emulatorLauncher
{
    static class ReflectionHelper
    {
        private static Dictionary<Type, ReflectionProperties> _types = new Dictionary<Type, ReflectionProperties>();

        public static ReflectionProperties GetReflectionProperties(this Type type)
        {
            ReflectionProperties prop;
            if (_types.TryGetValue(type, out prop))
                return prop;

            prop = new ReflectionProperties(type);
            _types[type] = prop;
            return prop;
        }

        public static object GetValue(this Type t, object instance, string propertyName)
        {
            var rp = GetReflectionProperties(t);
            var pi = rp.GetProperty(propertyName);
            if (pi == null)
                return null;

            return pi.GetValue(instance);
        }

        public static T GetValue<T>(this Type t, object instance, string propertyName) 
        {
            var rp = GetReflectionProperties(t);
            var pi = rp.GetProperty(propertyName);
            if (pi == null)
                return default(T);

            var obj = pi.GetValue(instance);
            if (obj is T)
                return (T)obj;

            if (obj != null)
            {
                try { return (T) Convert.ChangeType(obj, typeof(T)); }
                catch { }
            }

            return default(T);
        }
    }

    class ReflectionProperty
    {
        private System.Reflection.PropertyInfo _property;

        public ReflectionProperty(System.Reflection.PropertyInfo pi)
        {
            _property = pi;
        }

        public object GetValue(object instance, object[] index = null)
        {
            return _property.GetValue(instance, index);
        }
    }

    class ReflectionProperties
    {
        private Type _type;

        public ReflectionProperties(Type t)
        {
            _type = t;
        }

        public ReflectionProperty GetProperty(string name)
        {
            ReflectionProperty prop;
            if (_properties.TryGetValue(name, out prop))
                return prop;

            var propInfo = _type.GetProperty(name);
            if (propInfo == null)
                return null;

            prop = new ReflectionProperty(propInfo);
            _properties[name] = prop;
            return prop;

        }

        private Dictionary<string, ReflectionProperty> _properties = new Dictionary<string, ReflectionProperty>();
    }


}
