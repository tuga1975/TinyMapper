﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using TinyMapper.Nelibur.Sword.DataStructures;
using TinyMapper.Nelibur.Sword.Extensions;

namespace TinyMapper.Mappers
{
    internal class PrimitiveTypeMapper
    {
        private const BindingFlags StaticNonPublic = BindingFlags.Static | BindingFlags.NonPublic;
        private readonly List<Func<Type, Type, Option<Func<object, object>>>> _converters = new List<Func<Type, Type, Option<Func<object, object>>>>();

        public PrimitiveTypeMapper()
        {
            _converters.Add(GetConversionMethod2);
        }

        public TTo Map<TFrom, TTo>(TFrom value)
        {
            if (value.IsNull())
            {
                return default(TTo);
            }
            Option<Func<object, object>> converter = GetConverter2<TFrom, TTo>();
            if (converter.HasValue)
            {
                return (TTo)converter.Value(value);
            }
            return default(TTo);
        }

        internal static TTo ToEnum<TFrom, TTo>(object value)
        {
            if (value is string)
            {
                string textValue = value.ToString();
                return (TTo)Enum.Parse(typeof(TTo), textValue);
            }
            return (TTo)Convert.ChangeType(value, typeof(TFrom));
        }

        private static T ConvertTo<T>(object value, TypeConverter converter)
        {
            return (T)converter.ConvertTo(value, typeof(T));
        }

        private static Option<MethodInfo> GetConversionMethod(Type from, Type to)
        {
            if (from == null || to == null)
            {
                return Option<MethodInfo>.Empty;
            }

            TypeConverter converter = TypeDescriptor.GetConverter(from);

            if (converter.CanConvertTo(to))
            {
                MethodInfo result = typeof(PrimitiveTypeMapper)
                    .GetMethod("ConvertTo", StaticNonPublic)
                    .MakeGenericMethod(to, converter.GetType());
                return new Option<MethodInfo>(result);
            }

            if (to.IsEnum)
            {
                MethodInfo result = typeof(PrimitiveTypeMapper)
                    .GetMethod("ToEnum", StaticNonPublic)
                    .MakeGenericMethod(from, to);
                return new Option<MethodInfo>(result);
            }

            return Option<MethodInfo>.Empty;
        }

        private static Option<Func<object, object>> GetConversionMethod2(Type source, Type target)
        {
            if (source == null || target == null)
            {
                return Option<Func<object, object>>.Empty;
            }

            TypeConverter fromConverter = TypeDescriptor.GetConverter(source);
            if (fromConverter.CanConvertTo(target))
            {
                Func<object, object> result = x => fromConverter.ConvertTo(x, target);
                return result.ToOption();
            }

            TypeConverter toConverter = TypeDescriptor.GetConverter(target);
            if (toConverter.CanConvertFrom(source))
            {
                Func<object, object> result = x => toConverter.ConvertFrom(x);
                return result.ToOption();
            }

            if (source.IsEnum && target.IsEnum)
            {
                Func<object, object> result = x => Convert.ChangeType(x, source);
                return result.ToOption();
            }
            return Option<Func<object, object>>.Empty;
        }

        //        private Option<MethodInfo> GetConverter<TFrom, TTo>()
        //        {
        //            foreach (Func<Type, Type, Option<MethodInfo>> converter in _converters)
        //            {
        //                Option<MethodInfo> methodInfo = converter(typeof(TFrom), typeof(TTo));
        //                if (methodInfo.HasValue)
        //                {
        //                    return methodInfo;
        //                }
        //            }
        //            return Option<MethodInfo>.Empty;
        //        }
        private Option<Func<object, object>> GetConverter2<TFrom, TTo>()
        {
            foreach (Func<Type, Type, Option<Func<object, object>>> converter in _converters)
            {
                Option<Func<object, object>> func = converter(typeof(TFrom), typeof(TTo));
                if (func.HasValue)
                {
                    return func;
                }
            }
            return Option<Func<object, object>>.Empty;
        }

        private TTo Map<TFrom, TTo>(TFrom value, MethodInfo converter)
        {
            var result = (TTo)converter.Invoke(null, new[] { (object)value });
            return result;
        }
    }
}
