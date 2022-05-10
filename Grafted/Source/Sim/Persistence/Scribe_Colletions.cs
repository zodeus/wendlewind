using System;
using System.Collections.Generic;
using System.Xml;
using Grafted.Definitions;
using Grafted.Definitions.Loader;

namespace Grafted.Sim.Persistence;

public static class Scribe_Collections {
    public static void Look<T>(ref List<T>? list, string label, LookMode lookMode = LookMode.Undefined, params object[] ctorArgs) {
        if (lookMode == LookMode.Undefined && !TryResolveLookMode(typeof(T), out lookMode)) {
            Log.Error(string.Concat("LookList call with a list of ", typeof(T), " must have lookMode set explicitly."));
        }
        else if (Scribe.EnterNode(label)) {
            try {
                if (Scribe.State == ScribeState.Saving) {
                    if (list == null) {
                        Scribe.Saver.WriteAttribute("IsNull", "True");
                    }
                    else {
                        foreach (T item in list) {
                            switch (lookMode) {
                                case LookMode.Value: {
                                    T value5 = item;
                                    Scribe_Values.Look(ref value5!, "li", default, forceSave: true);
                                    break;
                                }
                                /*case LookMode.TargetInfo: {
                                    TargetInfo? value3 = (TargetInfo) (object) item!;
                                    Scribe_TargetInfo.Look(ref value3, "li");
                                    break;
                                }*/
                                case LookMode.Def: {
                                    Def value = (Def) (object) item!;
                                    Scribe_Defs.Look(ref value!, "li");
                                    break;
                                }
                                case LookMode.Deep: {
                                    T target = item;
                                    Scribe_Deep.Look(ref target, "li", ctorArgs);
                                    break;
                                }
                                case LookMode.Reference: {
                                    IIdentityProvider refee = (IIdentityProvider) item!;
                                    Scribe_References.Look(ref refee!, "li");
                                    break;
                                }
                            }
                        }
                    }
                }
                else if (Scribe.State == ScribeState.LoadingObjects) {
                    XmlNode curXmlParent = Scribe.Loader.CurXmlParent!;
                    XmlAttribute? xmlAttribute = curXmlParent.Attributes?["IsNull"];
                    if (xmlAttribute != null && xmlAttribute.Value.ToLower() == "true") {
                        if (lookMode == LookMode.Reference) {
                            Scribe.Loader.CrossRefs.LoadIDs.RegisterLoadIdListReadFromXml(null, null);
                        }

                        list = null;
                    }
                    else {
                        switch (lookMode) {
                            case LookMode.Value:
                                list = new List<T>(curXmlParent.ChildNodes.Count);
                                foreach (XmlNode childNode in curXmlParent.ChildNodes) {
                                    T item = ScribeExtractor.ValueFromNode(childNode, default(T));
                                    list.Add(item);
                                }

                                break;
                            case LookMode.Deep:
                                list = new List<T>(curXmlParent.ChildNodes.Count);
                                foreach (XmlNode childNode2 in curXmlParent.ChildNodes) {
                                    T item7 = ScribeExtractor.SaveableFromNode<T>(childNode2, ctorArgs);
                                    list.Add(item7);
                                }

                                break;
                            case LookMode.Def:
                                list = new List<T>(curXmlParent.ChildNodes.Count);
                                foreach (XmlNode childNode3 in curXmlParent.ChildNodes) {
                                    T item6 = ScribeExtractor.DefFromNodeUnsafe<T>(childNode3);
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
                                List<string> list2 = new(curXmlParent.ChildNodes.Count);
                                foreach (XmlNode childNode8 in curXmlParent.ChildNodes) {
                                    list2.Add(childNode8.InnerText);
                                }

                                Scribe.Loader.CrossRefs.LoadIDs.RegisterLoadIdListReadFromXml(list2, "");
                                break;
                            }
                        }
                    }
                }
                else if (Scribe.State == ScribeState.ResolvingCrossReferences) {
                    switch (lookMode) {
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
            finally {
                Scribe.ExitNode();
            }
        }
        else if (Scribe.State == ScribeState.LoadingObjects) {
            if (lookMode == LookMode.Reference) {
                Scribe.Loader.CrossRefs.LoadIDs.RegisterLoadIdListReadFromXml(null, label);
            }

            list = null;
        }
    }

    public static void Look<K, V>(ref Dictionary<K?, V?> dict, string label, LookMode keyLookMode = LookMode.Undefined, LookMode valueLookMode = LookMode.Undefined) {
        if (Scribe.State == ScribeState.LoadingObjects) {
            bool num = keyLookMode == LookMode.Reference;
            bool flag = valueLookMode == LookMode.Reference;
            if (num != flag) {
                Log.Error("You need to provide working lists for the keys and values in order to be able to load such dictionary. label=" + label);
            }
        }

        List<K>? keysWorkingList = null;
        List<V>? valuesWorkingList = null;
        Look(ref dict, label, keyLookMode, valueLookMode, ref keysWorkingList, ref valuesWorkingList);
    }

    public static void Look<K, V>(ref Dictionary<K, V> dict, string label, LookMode keyLookMode, LookMode valueLookMode, ref List<K> keysWorkingList, ref List<V> valuesWorkingList) {
        if (Scribe.EnterNode(label)) {
            try {
                if (Scribe.State == ScribeState.Saving && dict == null) {
                    Scribe.Saver.WriteAttribute("IsNull", "True");
                }
                else {
                    if (Scribe.State == ScribeState.LoadingObjects) {
                        XmlAttribute xmlAttribute = Scribe.Loader.CurXmlParent.Attributes["IsNull"];
                        if (xmlAttribute != null && xmlAttribute.Value.ToLower() == "true") {
                            dict = null;
                        }
                        else {
                            dict = new Dictionary<K, V>();
                        }
                    }

                    if (Scribe.State == ScribeState.Saving || Scribe.State == ScribeState.LoadingObjects) {
                        keysWorkingList = new List<K>();
                        valuesWorkingList = new List<V>();
                        if (Scribe.State == ScribeState.Saving && dict != null) {
                            foreach (KeyValuePair<K, V> item in dict) {
                                keysWorkingList.Add(item.Key);
                                valuesWorkingList.Add(item.Value);
                            }
                        }
                    }

                    if (Scribe.State == ScribeState.Saving || dict != null) {
                        Look(ref keysWorkingList, "keys", keyLookMode);
                        Look(ref valuesWorkingList, "values", valueLookMode);
                    }

                    if (Scribe.State == ScribeState.Saving) {
                        if (keysWorkingList != null) {
                            keysWorkingList.Clear();
                            keysWorkingList = null;
                        }

                        if (valuesWorkingList != null) {
                            valuesWorkingList.Clear();
                            valuesWorkingList = null;
                        }
                    }

                    bool flag = keyLookMode == LookMode.Reference || valueLookMode == LookMode.Reference;
                    if (((flag && Scribe.State == ScribeState.ResolvingCrossReferences) || (!flag && Scribe.State == ScribeState.LoadingObjects)) && dict != null) {
                        if (keysWorkingList == null) {
                            Log.Error("Cannot fill dictionary because there are no keys. label=" + label);
                        }
                        else if (valuesWorkingList == null) {
                            Log.Error("Cannot fill dictionary because there are no values. label=" + label);
                        }
                        else {
                            if (keysWorkingList.Count != valuesWorkingList.Count) {
                                Log.Error(
                                    "Keys count does not match the values count while loading a dictionary (maybe keys and values were resolved during different passes?). Some elements will be skipped. keys=" +
                                    keysWorkingList.Count + ", values=" + valuesWorkingList.Count + ", label=" + label);
                            }

                            int num = Math.Min(keysWorkingList.Count, valuesWorkingList.Count);
                            for (int i = 0; i < num; i++) {
                                if (keysWorkingList[i] == null) {
                                    Log.Error(string.Concat("Null key while loading dictionary of ", typeof(K), " and ", typeof(V), ". label=", label));
                                }
                                else {
                                    try {
                                        dict.Add(keysWorkingList[i], valuesWorkingList[i]);
                                    }
                                    catch (OutOfMemoryException) {
                                        throw;
                                    }
                                    catch (Exception ex2) {
                                        Log.Error("Exception in LookDictionary(label=" + label + "): " + ex2);
                                    }
                                }
                            }
                        }
                    }

                    if (Scribe.State == ScribeState.PostLoadInitialization) {
                        if (keysWorkingList != null) {
                            keysWorkingList.Clear();
                            keysWorkingList = null;
                        }

                        if (valuesWorkingList != null) {
                            valuesWorkingList.Clear();
                            valuesWorkingList = null;
                        }
                    }
                }
            }
            finally {
                Scribe.ExitNode();
            }
        }
        else if (Scribe.State == ScribeState.LoadingObjects) {
            dict = null;
        }
    }

    public static void Look<T>(ref HashSet<T> valueHashSet, string label, LookMode lookMode = LookMode.Undefined) {
        List<T> list = null;
        if (Scribe.State == ScribeState.Saving && valueHashSet != null) {
            list = new List<T>();
            foreach (T item in valueHashSet) {
                list.Add(item);
            }
        }

        Look(ref list, label, lookMode);
        if ((lookMode != LookMode.Reference || Scribe.State != ScribeState.ResolvingCrossReferences) && (lookMode == LookMode.Reference || Scribe.State != ScribeState.LoadingObjects)) {
            return;
        }

        if (list == null) {
            valueHashSet = null;
            return;
        }

        valueHashSet = new HashSet<T>();
        for (int i = 0; i < list.Count; i++) {
            valueHashSet.Add(list[i]);
        }
    }

    public static void Look<T>(ref Stack<T> valueStack, string label, LookMode lookMode = LookMode.Undefined) {
        List<T> list = null;
        if (Scribe.State == ScribeState.Saving && valueStack != null) {
            list = new List<T>();
            foreach (T item in valueStack) {
                list.Add(item);
            }
        }

        Look(ref list, label, lookMode);
        if ((lookMode != LookMode.Reference || Scribe.State != ScribeState.ResolvingCrossReferences) && (lookMode == LookMode.Reference || Scribe.State != ScribeState.LoadingObjects)) {
            return;
        }

        if (list == null) {
            valueStack = null;
            return;
        }

        valueStack = new Stack<T>();
        for (int i = 0; i < list.Count; i++) {
            valueStack.Push(list[i]);
        }
    }

    public static bool TryResolveLookMode(Type type, out LookMode lookMode, bool desperate = false, bool preferDeepIfDesperateAndAmbiguous = false) {
        if (type == null) {
            if (desperate) {
                lookMode = LookMode.Value;
                return true;
            }

            lookMode = LookMode.Undefined;
            return false;
        }

        if (type == typeof(object) && desperate) {
            lookMode = LookMode.Value;
            return true;
        }

        if (ParseHelper.HandlesType(type)) {
            lookMode = LookMode.Value;
            return true;
        }

        /*
        if (type == typeof(TargetInfo)) {
            lookMode = LookMode.TargetInfo;
            return true;
        }*/

        if (typeof(Def).IsAssignableFrom(type)) {
            lookMode = LookMode.Def;
            return true;
        }

        if (typeof(IExposable).IsAssignableFrom(type) && !typeof(IIdentityProvider).IsAssignableFrom(type)) {
            lookMode = LookMode.Deep;
            return true;
        }

        if (desperate && typeof(IIdentityProvider).IsAssignableFrom(type)) {
            lookMode = preferDeepIfDesperateAndAmbiguous ? LookMode.Deep : LookMode.Reference;

            return true;
        }

        lookMode = LookMode.Undefined;
        return false;
    }
}