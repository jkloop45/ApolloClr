﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
#if !JS
using System.Reflection.Emit;
#endif
using System.Text;
using System.Threading.Tasks;

namespace ApolloClr
{
    public static class Extensions
    {
        private static Action<object, object> DeleageSetFun = null;

        public static Delegate SetTarget(this Delegate @delegate, object target)
        {

#if !BRIDGE
            if (DeleageSetFun == null)
            {
                 BindingFlags flag = BindingFlags.Instance | BindingFlags.NonPublic;

                 var xfield = @delegate.GetType().GetField("_target", flag);


                if (xfield == null)
                {
                    throw new NotSupportedException("_target field was not found!");
                }
                DeleageSetFun = GetFSet(xfield);
             }
            DeleageSetFun(@delegate, target);

#else
            var _old = @delegate;
            Func<object> action = () =>
            {
                target["$scope"] = target;
                return Apply(_old, target);
            };
            @delegate = action;
#endif

            return @delegate;

        }
#if BRIDGE
        [Bridge.Template("{input}.apply({target}, arguments)")]
        public static object Apply(object input,object target)
        {
            return null;
        }
#endif
        //TODO UNITY 如果支持的话 回头再修改
        public static Action<object, object> GetFSet(FieldInfo field)
        {
#if JS
            Action<object, object> action = (send, v) =>
            {
                field.SetValue(send, v);
            };

            return action;

#else
            DynamicMethod dm = new DynamicMethod(String.Concat("_Set", field.Name, "_"), typeof(void),
                new Type[] {typeof(object), typeof(object)},
                field.DeclaringType, true);
            ILGenerator generator = dm.GetILGenerator();

            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldarg_1);
            if (field.FieldType.IsValueType)
                generator.Emit(OpCodes.Unbox_Any, field.FieldType);
            generator.Emit(OpCodes.Stfld, field);
            generator.Emit(OpCodes.Ret);

            return (Action<object, object>) dm.CreateDelegate(typeof(Action<object, object>));
#endif
        }


        public static object GetValueFromStr(string str, StackValueType vtype)
        {
            object value = null;
            switch (vtype)
            {
                case StackValueType.i8:
                    value = long.Parse(str);
                    break;
                case StackValueType.r8:
                    value = double.Parse(str);
                    break;
                case StackValueType.i4:
                    if (str.StartsWith("0x"))
                    {
                        value = System.Convert.ToInt32(str, 16);
                    }
                    else if (str.StartsWith("M") || str.StartsWith("m"))
                    {
                        value = (-int.Parse(str));
                    }
                    else
                    {
                        value = int.Parse(str);

                    }

                    break;
                case StackValueType.r4:
                    value = float.Parse(str);
                    break;
            }

            return value;
        }

        public static MethodInfo GetMethodInfo(this Type type, string name,Type [] types)
        {
           var mi = type.GetMethod(name, types);
            if (mi != null)
            {
                return mi;
            }
            mi = type.GetMethod(name.ToLower(), types);

            if (mi == null)
            {
                foreach (var methodInfo in type.GetMethods())
                {
                    if (methodInfo.Name.ToLower() == name.ToLower())
                    {
                        return methodInfo;
                    }
                }
            }
            return mi;
        }
        public static Type GetTypeByName(string name)
        {
            name = name.Replace("[mscorlib]", "");
            Type type = Type.GetType(name);

            if (type != null)
            {
                return type;
            }
            if (name.StartsWith("System"))
            {
                type = typeof(int).Assembly.GetType(name);

            }
            if (type == null && name.StartsWith("System"))
            {
                type = typeof(System.Diagnostics.Stopwatch).Assembly.GetType(name);
#if BRIDGE

                if (type == null)
                {

                    if ("System.Console" == name)
                    {
                        return typeof(System.Console);
                    }
                    type = typeof(System.Diagnostics.Stopwatch).Assembly.GetType(name.Replace("System.", "Bridge.")); 
                }
#endif
            }
            if (type != null)
            {
                return type;
            }
            switch (name)
            {
                case "string":
                    return typeof(string);
                case "int32":
                    return typeof(int);
                case "int64":
                    return typeof(long);
                case "float64":
                    return typeof(double);
                case "float32":
                    return typeof(float);
            }
  
            if (type == null)
            {
                throw new NotSupportedException("Type  Was  Not Fount :" + name);
            }
            return type;
        }
    }

}