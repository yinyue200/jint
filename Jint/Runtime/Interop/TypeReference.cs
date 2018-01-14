﻿using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Jint.Native;
using Jint.Native.Function;
using Jint.Native.Object;
using Jint.Runtime.Descriptors;
using Jint.Runtime.Descriptors.Specialized;

namespace Jint.Runtime.Interop
{
    public class TypeReference : FunctionInstance, IConstructor, IObjectWrapper
    {
        private TypeReference(Engine engine)
            : base(engine, null, null, false)
        {
        }

        public Type ReferenceType { get; set; }

        public static TypeReference CreateTypeReference(Engine engine, Type type)
        {
            var obj = new TypeReference(engine);
            obj.Extensible = false;
            obj.ReferenceType = type;

            // The value of the [[Prototype]] internal property of the TypeReference constructor is the Function prototype object
            obj.Prototype = engine.Function.PrototypeObject;

            obj.SetOwnProperty("length", new AllForbiddenPropertyDescriptor(0));

            // The initial value of Boolean.prototype is the Boolean prototype object
            obj.SetOwnProperty("prototype", new AllForbiddenPropertyDescriptor(engine.Object.PrototypeObject));

            return obj;
        }

        public override JsValue Call(JsValue thisObject, JsValue[] arguments)
        {
            // direct calls on a TypeReference constructor object is equivalent to the new operator
            return Construct(arguments);
        }

        public ObjectInstance Construct(JsValue[] arguments)
        {
            if (arguments.Length == 0 && ReferenceType.IsValueType())
            {
                var instance = Activator.CreateInstance(ReferenceType);
                var result = TypeConverter.ToObject(Engine, JsValue.FromObject(Engine, instance));

                return result;
            }

            var constructors = ReferenceType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);

            var methods = TypeConverter.FindBestMatch(Engine, constructors, arguments).ToList();

            foreach (var method in methods)
            {
                var parameters = new object[arguments.Length];
                try
                {
                    for (var i = 0; i < arguments.Length; i++)
                    {
                        var parameterType = method.GetParameters()[i].ParameterType;

                        if (typeof(JsValue).IsAssignableFrom(parameterType))
                        {
                            parameters[i] = arguments[i];
                        }
                        else
                        {
                            parameters[i] = Engine.ClrTypeConverter.Convert(
                                arguments[i].ToObject(),
                                parameterType,
                                CultureInfo.InvariantCulture);
                        }
                    }

                    var constructor = (ConstructorInfo)method;
                    var instance = constructor.Invoke(parameters.ToArray());
                    var result = TypeConverter.ToObject(Engine, JsValue.FromObject(Engine, instance));

                    // todo: cache method info

                    return result;
                }
                catch
                {
                    // ignore method
                }
            }

            throw new JavaScriptException(Engine.TypeError, "No public methods with the specified arguments were found.");

        }

        public override bool HasInstance(JsValue v)
        {
            ObjectWrapper wrapper = v.As<ObjectWrapper>();

            if (wrapper == null)
            {
                return base.HasInstance(v);
            }

            return wrapper.Target.GetType() == ReferenceType;
        }

        public override bool DefineOwnProperty(string propertyName, IPropertyDescriptor desc, bool throwOnError)
        {
            if (throwOnError)
            {
                throw new JavaScriptException(Engine.TypeError, "Can't define a property of a TypeReference");
            }

            return false;
        }

        public override bool Delete(string propertyName, bool throwOnError)
        {
            if (throwOnError)
            {
                throw new JavaScriptException(Engine.TypeError, "Can't delete a property of a TypeReference");
            }

            return false;
        }

        public override void Put(string propertyName, JsValue value, bool throwOnError)
        {
            if (!CanPut(propertyName))
            {
                if (throwOnError)
                {
                    throw new JavaScriptException(Engine.TypeError);
                }

                return;
            }

            var ownDesc = GetOwnProperty(propertyName);

            if (ownDesc == null)
            {
                if (throwOnError)
                {
                    throw new JavaScriptException(Engine.TypeError, "Unknown member: " + propertyName);
                }
                else
                {
                    return;
                }
            }

            ownDesc.Value = value;
        }

        public override IPropertyDescriptor GetOwnProperty(string propertyName)
        {
            // todo: cache members locally

            if (ReferenceType.IsEnum())
            {
                Array enumValues = Enum.GetValues(ReferenceType);
                Array enumNames = Enum.GetNames(ReferenceType);
                var enumNameStringComparer = Engine.Options._EnumNameStringComparer;

                for (int i = 0; i < enumValues.Length; i++)
                {
                    
                    if (enumNameStringComparer.Equals(enumNames.GetValue(i) as string, propertyName))
                    {
                        return new AllForbiddenPropertyDescriptor((int) enumValues.GetValue(i));
                    }
                }
                return PropertyDescriptor.Undefined;
            }

            var _CamelCasedProperties = Engine.Options._StaticMemberCamelCasedProperties;

            var propertiesStringComparer = _CamelCasedProperties.PropertiesStringComparer;
            var propertyInfo = ReferenceType.GetProperties(BindingFlags.Public | BindingFlags.Static).Where(p => propertiesStringComparer.Equals(p.Name, propertyName)).FirstOrDefault();
            if (propertyInfo != null)
            {
                return new PropertyInfoDescriptor(Engine, propertyInfo, Type);
            }

            var fieldsStringComparer = _CamelCasedProperties.FieldsStringComparer;
            var fieldInfo = ReferenceType.GetFields(BindingFlags.Public | BindingFlags.Static).Where(fi=> fieldsStringComparer.Equals(fi.Name, propertyName)).FirstOrDefault();
            if (fieldInfo != null)
            {
                return new FieldInfoDescriptor(Engine, fieldInfo, Type);
            }

            var methodsStringComparer = _CamelCasedProperties.MethodsStringComparer;
            var methodInfo = ReferenceType
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(mi => methodsStringComparer.Equals( mi.Name,propertyName))
                .ToArray();

            if (methodInfo.Length == 0)
            {
                return PropertyDescriptor.Undefined;
            }

            return new AllForbiddenPropertyDescriptor(new MethodInfoFunctionInstance(Engine, methodInfo));
        }

        public object Target => ReferenceType;

        public override string Class => "TypeReference";
    }
}
