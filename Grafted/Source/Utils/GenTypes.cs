using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Grafted.Sim;
using JetBrains.Annotations;

namespace Grafted.Utils;

public static class GenTypes {
    private readonly struct TypeCacheKey : IEquatable<TypeCacheKey> {
        public readonly string TypeName;

        public readonly string NamespaceIfAmbiguous;

        public override int GetHashCode() {
            if (NamespaceIfAmbiguous == null) {
                return TypeName.GetHashCode();
            }

            return (17 * 31 + TypeName.GetHashCode()) * 31 + NamespaceIfAmbiguous.GetHashCode();
        }

        public bool Equals(TypeCacheKey other) {
            if (string.Equals(TypeName, other.TypeName)) {
                return string.Equals(NamespaceIfAmbiguous, other.NamespaceIfAmbiguous);
            }

            return false;
        }

        public override bool Equals(object obj) {
            if (obj is TypeCacheKey key) {
                return Equals(key);
            }

            return false;
        }

        public TypeCacheKey(string typeName, string namespaceIfAmbiguous = null) {
            this.TypeName = typeName;
            NamespaceIfAmbiguous = namespaceIfAmbiguous;
        }
    }

    public static List<string> ImpliedNamespaceNames {
        get { return _impliedNamespaceNames ??= typeof(Simulation).Assembly.GetTypes().Where(t => t.Namespace?.StartsWith("Grafted") == true).Select(t => t.Namespace).Distinct().ToList()!; }
    }

    private static readonly Dictionary<TypeCacheKey, Type> TypeCache = new(EqualityComparer<TypeCacheKey>.Default);
    private static List<string>? _impliedNamespaceNames;

    private static IEnumerable<Assembly> AllActiveAssemblies {
        get { yield return Assembly.GetExecutingAssembly(); }
    }

    public static IEnumerable<Type> AllTypes {
        get {
            foreach (Assembly allActiveAssembly in AllActiveAssemblies) {
                Type[] array = null;
                try {
                    array = allActiveAssembly.GetTypes();
                }
                catch (ReflectionTypeLoadException) {
                    Log.Error("Exception getting types in assembly " + allActiveAssembly);
                }

                if (array != null) {
                    Type[] array2 = array;
                    for (int i = 0; i < array2.Length; i++) {
                        yield return array2[i];
                    }
                }
            }
        }
    }

    public static IEnumerable<Type> AllTypesWithAttribute<TAttr>() where TAttr : Attribute {
        return AllTypes.Where(x => AttributeExtensions.HasAttribute<TAttr>(x));
    }

    public static IEnumerable<Type> Subclasses(this Type baseType) {
        return AllTypes.Where(x => x.IsSubclassOf(baseType));
    }

    public static IEnumerable<Type> AllSubclassesNonAbstract(this Type baseType) {
        return AllTypes.Where(x => x.IsSubclassOf(baseType) && !x.IsAbstract);
    }

    public static IEnumerable<Type> AllLeafSubclasses(this Type baseType) {
        return from type in baseType.Subclasses()
            where !type.Subclasses().Any()
            select type;
    }

    public static IEnumerable<Type> InstantiableDescendantsAndSelf(this Type baseType) {
        if (!baseType.IsAbstract) {
            yield return baseType;
        }

        foreach (Type item in baseType.Subclasses()) {
            if (!item.IsAbstract) {
                yield return item;
            }
        }
    }

    public static Type GetTypeInAnyAssembly(string typeName, string namespaceIfAmbiguous = null) {
        TypeCacheKey key = new(typeName, namespaceIfAmbiguous);
        if (!TypeCache.TryGetValue(key, out Type value)) {
            value = GetTypeInAnyAssemblyInt(typeName, namespaceIfAmbiguous);
            TypeCache.Add(key, value);
        }

        return value;
    }

    private static Type GetTypeInAnyAssemblyInt(string typeName, string namespaceIfAmbiguous = null) {
        Type typeInAnyAssemblyRaw = GetTypeInAnyAssemblyRaw(typeName);
        if (typeInAnyAssemblyRaw != null) {
            return typeInAnyAssemblyRaw;
        }

        if (namespaceIfAmbiguous != null && !namespaceIfAmbiguous.NullOrEmpty() && ImpliedNamespaceNames.Contains(namespaceIfAmbiguous)) {
            typeInAnyAssemblyRaw = GetTypeInAnyAssemblyRaw(namespaceIfAmbiguous + "." + typeName);
            if (typeInAnyAssemblyRaw != null) {
                return typeInAnyAssemblyRaw;
            }
        }

        for (int i = 0; i < ImpliedNamespaceNames.Count; i++) {
            typeInAnyAssemblyRaw = GetTypeInAnyAssemblyRaw(ImpliedNamespaceNames[i] + "." + typeName);
            if (typeInAnyAssemblyRaw != null) {
                return typeInAnyAssemblyRaw;
            }
        }

        return null;
    }

    private static Type GetTypeInAnyAssemblyRaw(string typeName) {
        switch (typeName) {
            case "int":
                return typeof(int);
            case "uint":
                return typeof(uint);
            case "short":
                return typeof(short);
            case "ushort":
                return typeof(ushort);
            case "float":
                return typeof(float);
            case "double":
                return typeof(double);
            case "long":
                return typeof(long);
            case "ulong":
                return typeof(ulong);
            case "byte":
                return typeof(byte);
            case "sbyte":
                return typeof(sbyte);
            case "char":
                return typeof(char);
            case "bool":
                return typeof(bool);
            case "decimal":
                return typeof(decimal);
            case "string":
                return typeof(string);
            case "int?":
                return typeof(int?);
            case "uint?":
                return typeof(uint?);
            case "short?":
                return typeof(short?);
            case "ushort?":
                return typeof(ushort?);
            case "float?":
                return typeof(float?);
            case "double?":
                return typeof(double?);
            case "long?":
                return typeof(long?);
            case "ulong?":
                return typeof(ulong?);
            case "byte?":
                return typeof(byte?);
            case "sbyte?":
                return typeof(sbyte?);
            case "char?":
                return typeof(char?);
            case "bool?":
                return typeof(bool?);
            case "decimal?":
                return typeof(decimal?);
            default: {
                foreach (Assembly allActiveAssembly in AllActiveAssemblies) {
                    Type type = allActiveAssembly.GetType(typeName, throwOnError: false, ignoreCase: true);
                    if (type != null) {
                        return type;
                    }
                }

                Type type2 = Type.GetType(typeName, throwOnError: false, ignoreCase: true);
                if (type2 != null) {
                    return type2;
                }

                return null;
            }
        }
    }

    public static string GetTypeNameWithoutIgnoredNamespaces(Type type) {
        if (type.IsGenericType) {
            return type.ToString();
        }

        for (int i = 0; i < ImpliedNamespaceNames.Count; i++) {
            if (type.Namespace == ImpliedNamespaceNames[i]) {
                return type.Name;
            }
        }

        return type.FullName;
    }
}