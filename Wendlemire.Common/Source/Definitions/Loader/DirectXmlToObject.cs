using System.Reflection;
using System.Xml;

namespace Wendlemire.Definitions.Loader;

public static class DirectXmlToObject
{
    private readonly struct FieldAliasCache : IEquatable<FieldAliasCache>
    {
        private readonly Type _type;

        private readonly string _fieldName;

        public FieldAliasCache(Type type, string fieldName)
        {
            _type = type;
            _fieldName = fieldName.ToLower();
        }

        public bool Equals(FieldAliasCache other)
        {
            if (_type == other._type)
            {
                return string.Equals(_fieldName, other._fieldName);
            }

            return false;
        }
    }

    public const string DictionaryKeyName = "Key";

    public const string DictionaryValueName = "Value";

    public const string LoadDataFromXmlCustomMethodName = "LoadDataFromXmlCustom";

    public const string DefInitializeMethodName = "Initialize";

    public const string ObjectFromXmlMethodName = "ObjectFromXmlReflection";

    public const string ListItemNodeName = "ListItem";
    public const string ListFromXmlMethodName = "ListFromXmlReflection";

    public const string DictionaryFromXmlMethodName = "DictionaryFromXmlReflection";

    private static readonly Dictionary<Type, Func<XmlNode, object>> ListFromXmlMethods = new();

    private static readonly Dictionary<Type, Func<XmlNode, object>> DictionaryFromXmlMethods = new();

    private static readonly Type[] TmpOneTypeArray = new Type[1];

    private static readonly Dictionary<Type, Func<XmlNode, bool, object>> ObjectFromXmlMethods = new();

    private static readonly Dictionary<FieldAliasCache, FieldInfo> FieldAliases = new(EqualityComparer<FieldAliasCache>.Default);

    public static readonly Dictionary<Type, Dictionary<string, FieldInfo>> FieldInfoLookup = new();

    public static Func<XmlNode, bool, object> GetObjectFromXmlMethod(Type type)
    {
        if (ObjectFromXmlMethods.TryGetValue(type, out var func)) return func;

        var method = typeof(DirectXmlToObject).GetMethod(ObjectFromXmlMethodName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!;
        TmpOneTypeArray[0] = type;
        func = (Func<XmlNode, bool, object>)Delegate.CreateDelegate(typeof(Func<XmlNode, bool, object>), method.MakeGenericMethod(TmpOneTypeArray));
        ObjectFromXmlMethods.Add(type, func);

        return func;
    }

    [UsedImplicitly]
    private static object? ObjectFromXmlReflection<T>(XmlNode xmlRoot, bool doPostLoad = true)
    {
        return ObjectFromXml<T>(xmlRoot, doPostLoad);
    }

    public static T? ObjectFromXml<T>(XmlNode xmlRoot, bool doPostLoad)
    {
        var methodInfo = CustomDataLoadMethodOf(typeof(T));
        if (methodInfo != null)
        {
            return ObjectFromXmlUsingCustomLoader<T>(xmlRoot, doPostLoad, methodInfo);
        }

        if (xmlRoot.FirstChild != null && xmlRoot.ChildNodes.Count == 1 && xmlRoot.FirstChild.NodeType == XmlNodeType.CDATA)
        {
            if (typeof(T) != typeof(string))
            {
                Log.Error("CDATA can only be used for strings. Bad xml: " + xmlRoot.OuterXml);
                return default;
            }

            return (T?)(object?)xmlRoot.FirstChild.Value;
        }

        if (xmlRoot.ChildNodes.Count == 1 && xmlRoot.FirstChild?.NodeType == XmlNodeType.Text)
        {
            try
            {
                return ParseHelper.FromString<T>(xmlRoot.InnerText);
            }
            catch (Exception e)
            {
                Log.Error(string.Concat("Exception parsing ", xmlRoot.OuterXml, " to type ", typeof(T), ": ", e.ToString()));
            }

            return default;
        }

        if (Attribute.IsDefined(typeof(T), typeof(FlagsAttribute)))
        {
            var list = ListFromXml<T>(xmlRoot);
            var num = 0;
            foreach (var item in list)
            {
                var num2 = (int?)(object?)item ?? 0;
                num |= num2;
            }

            return (T)(object)num;
        }

        if (typeof(T).HasGenericDefinition(typeof(List<>)))
        {
            if (!ListFromXmlMethods.TryGetValue(typeof(T), out Func<XmlNode, object>? value))
            {
                var method = typeof(DirectXmlToObject).GetMethod(ListFromXmlMethodName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!;
                Type[] genericArguments = typeof(T).GetGenericArguments();
                value = (Func<XmlNode, object>)Delegate.CreateDelegate(typeof(Func<XmlNode, object>), method.MakeGenericMethod(genericArguments));
                ListFromXmlMethods.Add(typeof(T), value);
            }

            return (T)value(xmlRoot);
        }

        if (typeof(T).HasGenericDefinition(typeof(Dictionary<,>)))
        {
            if (!DictionaryFromXmlMethods.TryGetValue(typeof(T), out Func<XmlNode, object>? value2))
            {
                var method2 = typeof(DirectXmlToObject).GetMethod(DictionaryFromXmlMethodName,
                    BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!;
                Type[] genericArguments2 = typeof(T).GetGenericArguments();
                value2 = (Func<XmlNode, object>)Delegate.CreateDelegate(typeof(Func<XmlNode, object>), method2.MakeGenericMethod(genericArguments2));
                DictionaryFromXmlMethods.Add(typeof(T), value2);
            }

            return (T)value2(xmlRoot);
        }

        if (!xmlRoot.HasChildNodes)
        {
            if (typeof(T) == typeof(string))
            {
                return (T)(object)"";
            }

            var xmlAttribute = xmlRoot.Attributes!["IsNull"];
            if (xmlAttribute != null && xmlAttribute.Value.ToUpperInvariant() == "TRUE")
            {
                return default(T);
            }

            if (typeof(T).IsGenericType)
            {
                var genericTypeDefinition = typeof(T).GetGenericTypeDefinition();
                if (genericTypeDefinition == typeof(List<>) || genericTypeDefinition == typeof(HashSet<>) || genericTypeDefinition == typeof(Dictionary<,>))
                {
                    return Activator.CreateInstance<T>();
                }
            }
        }
        xmlRoot = XmlInheritance.GetResolvedNodeFor(xmlRoot)!;
        var type = ClassTypeOf<T>(xmlRoot);
        var typeOrUnderlyingType = Nullable.GetUnderlyingType(type) ?? type;
        var dataObject = (T?)Activator.CreateInstance(typeOrUnderlyingType);

        HashSet<string>? hashSet = null;
        if (xmlRoot.ChildNodes.Count > 1)
        {
            hashSet = new HashSet<string>();
        }

        for (var i = 0; i < xmlRoot.ChildNodes.Count; i++)
        {
            var xmlNode = xmlRoot.ChildNodes[i];
            if (xmlNode is XmlComment)
            {
                continue;
            }

            if (xmlRoot.ChildNodes.Count > 1)
            {
                if (xmlNode != null && (hashSet?.Contains(xmlNode.Name) ?? false))
                {
                    Log.Error(
                        string.Concat("XML ", typeof(T), " defines the same field twice: ", xmlNode.Name, ".\n\nField contents: ", xmlNode.InnerText, ".\n\nWhole XML:\n\n", xmlRoot.OuterXml));
                }
                else
                {
                    hashSet!.Add(xmlNode!.Name);
                }
            }

            var fieldInfo = GetFieldInfoForType(dataObject?.GetType(), xmlNode!.Name);

            if (fieldInfo == null)
            {
                Log.Warning("Oh interesting we have attempted to use FieldAliasCache for " + xmlNode.OuterXml + " " + dataObject?.GetType().Name);
                var key = new FieldAliasCache(dataObject!.GetType(), xmlNode.Name);
                if (!FieldAliases.TryGetValue(key, out fieldInfo))
                {
                    //todo this looks like a bug/junk
                    FieldInfo[] fields = dataObject.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    foreach (var _ in fields)
                    {
                        if (fieldInfo != null)
                        {
                            break;
                        }
                    }

                    FieldAliases.Add(key, fieldInfo!);
                }
            }

            /*
            if (value3 != null && value3.TryGetAttribute<UnsavedAttribute>() != null && !value3.TryGetAttribute<UnsavedAttribute>().allowLoading) {
                Log.Error("XML error: " + xmlNode.OuterXml + " corresponds to a field in type " + val2.GetType().Name + " which has an Unsaved attribute. Context: " + xmlRoot.OuterXml);
                continue;
            }
            */

            if (fieldInfo == null)
            {
                var flag = false;
                var xmlAttribute2 = xmlNode.Attributes?["IgnoreIfNoMatchingField"];
                if (xmlAttribute2 != null && xmlAttribute2.Value.ToUpperInvariant() == "TRUE")
                {
                    flag = true;
                }
                /*else {
                    //object[] customAttributes = val2.GetType().GetCustomAttributes(typeof(IgnoreSavedElementAttribute), inherit: true);
                    /*for (int j = 0; j < customAttributes.Length; j++) {
                        if (string.Equals(((IgnoreSavedElementAttribute) customAttributes[j]).elementToIgnore, xmlNode.Name, StringComparison.OrdinalIgnoreCase)) {
                            flag = true;
                            break;
                        }
                    }#1#
                }
                */

                if (!flag)
                {
                    Log.Error("XML error: " + xmlNode.OuterXml + " doesn't correspond to any field in type " + (dataObject?.GetType().Name ?? "null") + ". Context: " + xmlRoot.OuterXml);
                }


                continue;
            }

            if (typeof(Def).IsAssignableFrom(fieldInfo.FieldType))
            {
                if (xmlNode.InnerText.NullOrEmpty())
                {
                    fieldInfo.SetValue(dataObject, null);
                    continue;
                }

                var xmlAttribute3 = xmlNode.Attributes?["MayRequire"];
                DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(dataObject, fieldInfo, xmlNode.InnerText, xmlAttribute3?.Value.ToLower());
                continue;
            }

            object? obj;
            try
            {
                obj = GetObjectFromXmlMethod(fieldInfo.FieldType)(xmlNode, doPostLoad);
            }
            catch (Exception e)
            {
                Log.Error("Exception loading from " + xmlNode + ": " + e);
                continue;
            }

            if (!typeof(T).IsValueType)
            {
                fieldInfo.SetValue(dataObject, obj);
                continue;
            }

            object? obj2 = dataObject;
            fieldInfo.SetValue(obj2, obj);
            dataObject = (T)obj2!;
        }

        if (doPostLoad)
        {
            TryDoPostLoad(dataObject);
        }

        return dataObject;
    }

    private static T? ObjectFromXmlUsingCustomLoader<T>(XmlNode xmlRoot, bool doPostLoad, MethodInfo methodInfo)
    {
        xmlRoot = XmlInheritance.GetResolvedNodeFor(xmlRoot)!;
        var type = ClassTypeOf<T>(xmlRoot);
        var instance = (T)Activator.CreateInstance(type)!;

        try
        {
            methodInfo.Invoke(instance, [xmlRoot]);
        }
        catch (Exception ex)
        {
            Log.Error(string.Concat("Exception in custom XML loader for ", typeof(T), ". Node is:\n ", xmlRoot.OuterXml, "\n\nException is:\n ", ex.ToString()));
            instance = default(T);
        }

        if (doPostLoad)
        {
            TryDoPostLoad(instance);
        }

        return instance;
    }

    private static Type ClassTypeOf<T>(XmlNode xmlRoot)
    {
        var xmlAttribute = xmlRoot.Attributes?["Class"];
        if (xmlAttribute == null) return typeof(T);

        var typeInAnyAssembly = GenTypes.GetTypeInAnyAssembly(xmlAttribute.Value, typeof(T).Namespace);
        if (typeInAnyAssembly != null) return typeInAnyAssembly;

        Log.Error("Could not find type named " + xmlAttribute.Value + " from node " + xmlRoot.OuterXml);
        return typeof(T);
    }

    private static void TryDoPostLoad(object? obj)
    {
        var method = obj?.GetType().GetMethod(DefInitializeMethodName);
        if (method != null)
        {
            method.Invoke(obj, null);
        }
    }

    [UsedImplicitly]
    private static object ListFromXmlReflection<T>(XmlNode listRootNode)
    {
        return ListFromXml<T>(listRootNode);
    }

    private static List<T> ListFromXml<T>(XmlNode listRootNode)
    {
        var list = new List<T>();
        try
        {
            var flag = typeof(Def).IsAssignableFrom(typeof(T));
            foreach (XmlNode childNode in listRootNode.ChildNodes)
            {
                if (ValidateListNode(childNode, listRootNode, typeof(T)))
                {
                    if (flag)
                    {
                        var xmlAttribute = childNode.Attributes?["MayRequire"];
                        DirectXmlCrossRefLoader.RegisterListWantsCrossRef(list, childNode.InnerText, listRootNode.Name, xmlAttribute?.Value);
                    }
                    else
                    {
                        try
                        {
                            list.Add(ObjectFromXml<T>(childNode, doPostLoad: true)!);
                        }
                        catch (Exception ex)
                        {
                            Log.Error(string.Concat("Exception loading list element from XML: ", ex, "\nXML:\n", listRootNode.OuterXml));
                        }
                    }
                }
            }

            return list;
        }
        catch (Exception ex2)
        {
            Log.Error(string.Concat("Exception loading list from XML: ", ex2, "\nXML:\n", listRootNode.OuterXml));
            return list;
        }
    }

    [UsedImplicitly]
    private static object DictionaryFromXmlReflection<TKeyTpe, TValueType>(XmlNode dictRootNode) where TKeyTpe : notnull
    {
        return DictionaryFromXml<TKeyTpe, TValueType>(dictRootNode);
    }

    private static Dictionary<TKeyTpe, TValueType> DictionaryFromXml<TKeyTpe, TValueType>(XmlNode dictRootNode) where TKeyTpe : notnull
    {
        var dictionary = new Dictionary<TKeyTpe, TValueType>();
        try
        {
            var num = typeof(Def).IsAssignableFrom(typeof(TKeyTpe));
            var flag = typeof(Def).IsAssignableFrom(typeof(TValueType));
            if (!num && !flag)
            {
                foreach (XmlNode childNode in dictRootNode.ChildNodes)
                {
                    if (ValidateListNode(childNode, dictRootNode, typeof(KeyValuePair<TKeyTpe, TValueType>)))
                    {
                        var key = ObjectFromXml<TKeyTpe>(childNode[DictionaryKeyName]!, true)!;
                        var value = ObjectFromXml<TValueType>(childNode[DictionaryValueName]!, true)!;
                        dictionary.Add(key, value);
                    }
                }

                return dictionary;
            }

            foreach (XmlNode childNode2 in dictRootNode.ChildNodes)
            {
                if (ValidateListNode(childNode2, dictRootNode, typeof(KeyValuePair<TKeyTpe, TValueType>)))
                {
                    DirectXmlCrossRefLoader.RegisterDictionaryWantsCrossRef(dictionary, childNode2, dictRootNode.Name);
                }
            }

            return dictionary;
        }
        catch (Exception ex)
        {
            Log.Error("Malformed dictionary XML. Node: " + dictRootNode.OuterXml + ".\n\nException: " + ex);
            return dictionary;
        }
    }

    private static MethodInfo? CustomDataLoadMethodOf(Type type)
    {
        return type.GetMethod(LoadDataFromXmlCustomMethodName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
    }

    private static bool ValidateListNode(XmlNode listEntryNode, XmlNode listRootNode, Type listItemType)
    {
        if (listEntryNode is XmlComment)
        {
            return false;
        }

        if (listEntryNode is XmlText)
        {
            Log.Error("XML format error: Raw text found inside a list element. Did you mean to surround it with list item <ListItem> tags? " + listRootNode.OuterXml);
            return false;
        }

        if (listEntryNode.Name != ListItemNodeName && CustomDataLoadMethodOf(listItemType) == null)
        {
            Log.Error("XML format error: List item found with name that is not <ListItem>, and which does not have a custom XML loader method, in " + listRootNode.OuterXml);
            return false;
        }

        return true;
    }

    private static FieldInfo? GetFieldInfoForType(Type? type, string token)
    {
        if (type == null)
        {
            return null;
        }

        var dictionary = FieldInfoLookup!.TryGetValue(type);
        if (dictionary == null)
        {
            dictionary = new Dictionary<string, FieldInfo>();
            FieldInfoLookup[type] = dictionary;
        }

        var fieldInfo = dictionary!.TryGetValue(token);
        if (fieldInfo == null && !dictionary.ContainsKey(token))
        {
            fieldInfo = SearchTypeHierarchy(type, token, BindingFlags.Default);
            if (fieldInfo == null)
            {
                var text = $"Failed to get field info using token {token} to refer to field {fieldInfo?.Name} in type {type}";
                Log.Error(text);
                return null;
            }

            dictionary[token] = fieldInfo;
        }

        return fieldInfo;
    }

    private static FieldInfo? SearchTypeHierarchy(Type type, string token, BindingFlags extraFlags)
    {
        FieldInfo? fieldInfo;
        while (true)
        {
            fieldInfo = type.GetField(token, extraFlags | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (!(fieldInfo == null) || !(type.BaseType != typeof(object)))
            {
                break;
            }

            type = type.BaseType!;
        }

        return fieldInfo;
    }
}