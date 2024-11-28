using System.Xml;
using Grafted.Definitions.Loader;

namespace Grafted.Sim.Persistence;

public static class ScribeCollections
{
    public static void Look<T>(ref List<T>? list, string label, LookMode lookMode = LookMode.Undefined, params object[] ctorArgs)
    {
        if (lookMode == LookMode.Undefined && !TryResolveLookMode(typeof(T), out lookMode))
        {
            Log.Error(string.Concat("LookList call with a list of ", typeof(T), " must have lookMode set explicitly."));
        }
        else if (Scribe.EnterNode(label))
        {
            try
            {
                if (Scribe.State == ScribeState.Saving)
                {
                    if (list == null)
                    {
                        Scribe.Saver.WriteAttribute("IsNull", "True");
                    }
                    else
                    {
                        foreach (var item in list)
                        {
                            switch (lookMode)
                            {
                                case LookMode.Value: {
                                    var value5 = item;
                                    ScribeValues.Look(ref value5!, "li", forceSave: true);
                                    break;
                                }
                                /*case LookMode.TargetInfo: {
                                    TargetInfo? value3 = (TargetInfo) (object) item!;
                                    Scribe_TargetInfo.Look(ref value3, "li");
                                    break;
                                }*/
                                case LookMode.Def: {
                                    var value = (Def)(object)item!;
                                    ScribeDefs.Look(ref value!, "li");
                                    break;
                                }
                                case LookMode.Deep: {
                                    var target = item;
                                    ScribeDeep.Look(ref target, "li", ctorArgs);
                                    break;
                                }
                                case LookMode.Reference: {
                                    var refee = (IIdentityProvider)item!;
                                    ScribeReferences.Look(ref refee!, "li");
                                    break;
                                }
                            }
                        }
                    }
                }
                else if (Scribe.State == ScribeState.LoadingObjects)
                {
                    var curXmlParent = Scribe.Loader.CurXmlParent!;
                    var xmlAttribute = curXmlParent.Attributes?["IsNull"];
                    if (xmlAttribute != null && xmlAttribute.Value.ToLower() == "true")
                    {
                        if (lookMode == LookMode.Reference)
                        {
                            Scribe.Loader.CrossRefs.LoadIDs.RegisterLoadIdListReadFromXml(null, null);
                        }

                        list = null;
                    }
                    else
                    {
                        switch (lookMode)
                        {
                            case LookMode.Value:
                                list = new List<T>(curXmlParent.ChildNodes.Count);
                                foreach (XmlNode childNode in curXmlParent.ChildNodes)
                                {
                                    var item = ScribeExtractor.ValueFromNode(childNode, default(T))!;
                                    list.Add(item);
                                }

                                break;
                            case LookMode.Deep:
                                list = new List<T>(curXmlParent.ChildNodes.Count);
                                foreach (XmlNode childNode2 in curXmlParent.ChildNodes)
                                {
                                    var item7 = ScribeExtractor.SaveableFromNode<T>(childNode2, ctorArgs)!;
                                    list.Add(item7);
                                }

                                break;
                            case LookMode.Def:
                                list = new List<T>(curXmlParent.ChildNodes.Count);
                                foreach (XmlNode childNode3 in curXmlParent.ChildNodes)
                                {
                                    var item6 = ScribeExtractor.DefFromNodeUnsafe<T>(childNode3);
                                    list.Add(item6);
                                }

                                break;
                            /*case LookMode.TargetInfo: {
                                throw new NotImplementedException();
                                /*list = new List<T>(curXmlParent.ChildNodes.Count);
                                int num2 = 0;
                                foreach (XmlNode childNode6 in curXmlParent.ChildNodes)
                                {
                                    T item3 = (T)(object)ScribeExtractor.TargetInfoFromNode(childNode6, num2.ToString(), TargetInfo.Invalid);
                                    list.Add(item3);
                                    num2++;
                                }#1#
                                break;
                            }*/
                            case LookMode.Reference: {
                                List<string> list2 = new List<string>(curXmlParent.ChildNodes.Count);
                                foreach (XmlNode childNode8 in curXmlParent.ChildNodes)
                                {
                                    list2.Add(childNode8.InnerText);
                                }

                                Scribe.Loader.CrossRefs.LoadIDs.RegisterLoadIdListReadFromXml(list2, "");
                                break;
                            }
                        }
                    }
                }
                else if (Scribe.State == ScribeState.ResolvingCrossReferences)
                {
                    switch (lookMode)
                    {
                        case LookMode.Reference:
                            list = Scribe.Loader.CrossRefs.TakeResolvedRefList<T>("");
                            break;
                        /*case LookMode.TargetInfo:
                            if (list != null) {
                                for (int k = 0; k < list.Count; k++) {
                                    list[k] = (T) (object) ScribeExtractor.ResolveTargetInfo((TargetInfo) (object) list[k], k.ToString());
                                }
                            }

                            break;*/
                    }
                }
            }
            finally
            {
                Scribe.ExitNode();
            }
        }
        else if (Scribe.State == ScribeState.LoadingObjects)
        {
            if (lookMode == LookMode.Reference)
            {
                Scribe.Loader.CrossRefs.LoadIDs.RegisterLoadIdListReadFromXml(null, label);
            }

            list = null;
        }
    }

    public static void Look<TKeyType, TValueType>(ref Dictionary<TKeyType, TValueType>? dict,
        string label,
        LookMode keyLookMode = LookMode.Undefined,
        LookMode valueLookMode = LookMode.Undefined)
        where TKeyType : notnull
    {
        if (Scribe.State == ScribeState.LoadingObjects)
        {
            var num = keyLookMode == LookMode.Reference;
            var flag = valueLookMode == LookMode.Reference;
            if (num != flag)
            {
                Log.Error("You need to provide working lists for the keys and values in order to be able to load such dictionary. label=" + label);
            }
        }

        List<TKeyType?>? keysWorkingList = null;
        List<TValueType>? valuesWorkingList = null;
        Look(ref dict, label, keyLookMode, valueLookMode, ref keysWorkingList, ref valuesWorkingList);
    }

    public static void Look<TKeyTpe, TValueType>(ref Dictionary<TKeyTpe, TValueType>? dict,
        string label,
        LookMode keyLookMode,
        LookMode valueLookMode,
        ref List<TKeyTpe?>? keysWorkingList,
        ref List<TValueType>? valuesWorkingList) where TKeyTpe : notnull
    {
        if (Scribe.EnterNode(label))
        {
            try
            {
                if (Scribe.State == ScribeState.Saving && dict == null)
                {
                    Scribe.Saver.WriteAttribute("IsNull", "True");
                }
                else
                {
                    if (Scribe.State == ScribeState.LoadingObjects)
                    {
                        var xmlAttribute = Scribe.Loader.CurXmlParent?.Attributes?["IsNull"];
                        if (xmlAttribute != null && xmlAttribute.Value.ToLower() == "true")
                        {
                            dict = null;
                        }
                        else
                        {
                            dict = new Dictionary<TKeyTpe, TValueType>();
                        }
                    }

                    if (Scribe.State == ScribeState.Saving || Scribe.State == ScribeState.LoadingObjects)
                    {
                        keysWorkingList = new List<TKeyTpe?>();
                        valuesWorkingList = new List<TValueType>();
                        if (Scribe.State == ScribeState.Saving && dict != null)
                        {
                            foreach (KeyValuePair<TKeyTpe, TValueType> item in dict)
                            {
                                keysWorkingList.Add(item.Key);
                                valuesWorkingList.Add(item.Value);
                            }
                        }
                    }

                    if (Scribe.State == ScribeState.Saving || dict != null)
                    {
                        Look(ref keysWorkingList, "keys", keyLookMode);
                        Look(ref valuesWorkingList, "values", valueLookMode);
                    }

                    if (Scribe.State == ScribeState.Saving)
                    {
                        if (keysWorkingList != null)
                        {
                            keysWorkingList.Clear();
                            keysWorkingList = null;
                        }

                        if (valuesWorkingList != null)
                        {
                            valuesWorkingList.Clear();
                            valuesWorkingList = null;
                        }
                    }

                    var flag = keyLookMode == LookMode.Reference || valueLookMode == LookMode.Reference;
                    if (((flag && Scribe.State == ScribeState.ResolvingCrossReferences) || (!flag && Scribe.State == ScribeState.LoadingObjects)) && dict != null)
                    {
                        if (keysWorkingList == null)
                        {
                            Log.Error("Cannot fill dictionary because there are no keys. label=" + label);
                        }
                        else if (valuesWorkingList == null)
                        {
                            Log.Error("Cannot fill dictionary because there are no values. label=" + label);
                        }
                        else
                        {
                            if (keysWorkingList.Count != valuesWorkingList.Count)
                            {
                                Log.Error(
                                    "Keys count does not match the values count while loading a dictionary (maybe keys and values were resolved during different passes?). Some elements will be skipped. keys=" +
                                    keysWorkingList.Count + ", values=" + valuesWorkingList.Count + ", label=" + label);
                            }

                            var num = Math.Min(keysWorkingList.Count, valuesWorkingList.Count);
                            for (var i = 0; i < num; i++)
                            {
                                if (keysWorkingList[i] == null)
                                {
                                    Log.Error(string.Concat("Null key while loading dictionary of ", typeof(TKeyTpe), " and ", typeof(TValueType), ". label=", label));
                                }
                                else
                                {
                                    try
                                    {
                                        dict.Add(keysWorkingList[i]!, valuesWorkingList[i]);
                                    }
                                    catch (OutOfMemoryException)
                                    {
                                        throw;
                                    }
                                    catch (Exception ex2)
                                    {
                                        Log.Error("Exception in LookDictionary(label=" + label + "): " + ex2);
                                    }
                                }
                            }
                        }
                    }

                    if (Scribe.State == ScribeState.PostLoadInitialization)
                    {
                        if (keysWorkingList != null)
                        {
                            keysWorkingList.Clear();
                            keysWorkingList = null;
                        }

                        if (valuesWorkingList != null)
                        {
                            valuesWorkingList.Clear();
                            valuesWorkingList = null;
                        }
                    }
                }
            }
            finally
            {
                Scribe.ExitNode();
            }
        }
        else if (Scribe.State == ScribeState.LoadingObjects)
        {
            dict = null;
        }
    }

    public static void Look<T>(ref HashSet<T>? valueHashSet, string label, LookMode lookMode = LookMode.Undefined)
    {
        List<T>? list = null;
        if (Scribe.State == ScribeState.Saving && valueHashSet != null)
        {
            list = new List<T>();
            foreach (var item in valueHashSet)
            {
                list.Add(item);
            }
        }

        Look(ref list, label, lookMode);
        if ((lookMode != LookMode.Reference || Scribe.State != ScribeState.ResolvingCrossReferences) && (lookMode == LookMode.Reference || Scribe.State != ScribeState.LoadingObjects))
        {
            return;
        }

        if (list == null)
        {
            valueHashSet = null;
            return;
        }

        valueHashSet = new HashSet<T>();
        for (var i = 0; i < list.Count; i++)
        {
            valueHashSet.Add(list[i]);
        }
    }

    public static void Look<T>(ref Stack<T>? valueStack, string label, LookMode lookMode = LookMode.Undefined)
    {
        List<T>? list = null;
        if (Scribe.State == ScribeState.Saving && valueStack != null)
        {
            list = new List<T>();
            foreach (var item in valueStack)
            {
                list.Add(item);
            }
        }

        Look(ref list, label, lookMode);
        if ((lookMode != LookMode.Reference || Scribe.State != ScribeState.ResolvingCrossReferences) && (lookMode == LookMode.Reference || Scribe.State != ScribeState.LoadingObjects))
        {
            return;
        }

        if (list == null)
        {
            valueStack = null;
            return;
        }

        valueStack = new Stack<T>();
        for (var i = 0; i < list.Count; i++)
        {
            valueStack.Push(list[i]);
        }
    }

    public static bool TryResolveLookMode(Type? type, out LookMode lookMode, bool desperate = false, bool preferDeepIfDesperateAndAmbiguous = false)
    {
        if (type == null)
        {
            if (desperate)
            {
                lookMode = LookMode.Value;
                return true;
            }

            lookMode = LookMode.Undefined;
            return false;
        }

        if (type == typeof(object) && desperate)
        {
            lookMode = LookMode.Value;
            return true;
        }

        if (ParseHelper.HandlesType(type))
        {
            lookMode = LookMode.Value;
            return true;
        }

        /*
        if (type == typeof(TargetInfo)) {
            lookMode = LookMode.TargetInfo;
            return true;
        }*/

        if (typeof(Def).IsAssignableFrom(type))
        {
            lookMode = LookMode.Def;
            return true;
        }

        if (typeof(IExposable).IsAssignableFrom(type) && !typeof(IIdentityProvider).IsAssignableFrom(type))
        {
            lookMode = LookMode.Deep;
            return true;
        }

        if (desperate && typeof(IIdentityProvider).IsAssignableFrom(type))
        {
            lookMode = preferDeepIfDesperateAndAmbiguous ? LookMode.Deep : LookMode.Reference;

            return true;
        }

        lookMode = LookMode.Undefined;
        return false;
    }
}
