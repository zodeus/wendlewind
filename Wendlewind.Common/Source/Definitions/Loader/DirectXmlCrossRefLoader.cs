#nullable disable
using System.Reflection;
using System.Xml;

namespace Wendlewind.Definitions.Loader;

public static class DirectXmlCrossRefLoader {
    public enum FailMode : byte {
        Silent,
        LogErrors
    }

    private abstract class WantedRef {
        public object wanter;

        public abstract bool TryResolve(FailMode failReportMode);

        public virtual void Apply() { }
    }

    private class WantedRefForObject : WantedRef {
        public FieldInfo fi;

        public string defName;

        public Def resolvedDef;

        public string mayRequireMod;

        public Type overrideFieldType;

        private bool BadCrossRefAllowed {
            get {
                /*if (!mayRequireMod.NullOrEmpty() && !ModsConfig.IsActive(mayRequireMod))
                {
                    return true;
                }*/
                return false;
            }
        }

        public WantedRefForObject(object wanter, FieldInfo fi, string targetDefName, string mayRequireMod = null, Type overrideFieldType = null) {
            this.wanter = wanter;
            this.fi = fi;
            defName = targetDefName;
            this.mayRequireMod = mayRequireMod;
            this.overrideFieldType = overrideFieldType;
        }

        public override bool TryResolve(FailMode failReportMode) {
            if (fi == null) {
                Log.Error("Trying to resolve null field for def named " + defName);
                return false;
            }

            Type type = overrideFieldType ?? fi.FieldType;
            resolvedDef = DefRepository.GetDef(type, defName);
            if (resolvedDef == null) {
                if (failReportMode == FailMode.LogErrors && !BadCrossRefAllowed) {
                    Log.Error(string.Concat("Could not resolve cross-reference: No ", type, " named ", defName, " found to give to ", wanter.GetType(), " ", wanter));
                }

                return false;
            }

            /*SoundDef soundDef = resolvedDef as SoundDef;
            if (soundDef != null && soundDef.isUndefined)
            {
                Log.Warning(string.Concat("Could not resolve cross-reference: No ", type, " named ", defName.ToString(), " found to give to ", wanter.GetType(), " ", wanter.ToString(), " (using undefined sound instead)"));
            }*/
            fi.SetValue(wanter, resolvedDef);
            return true;
        }
    }

    private class WantedRefForList<T> : WantedRef {
        private List<string> defNames = new();

        private List<string> mayRequireMods;

        private object debugWanterInfo;

        public WantedRefForList(object wanter, object debugWanterInfo) {
            this.wanter = wanter;
            this.debugWanterInfo = debugWanterInfo;
        }

        public void AddWantedListEntry(string newTargetDefName, string mayRequireMod = null) {
            if (!mayRequireMod.NullOrEmpty() && mayRequireMods == null) {
                mayRequireMods = new List<string>();
                for (int i = 0; i < defNames.Count; i++) {
                    mayRequireMods.Add(null);
                }
            }

            defNames.Add(newTargetDefName);
            if (mayRequireMods != null) {
                mayRequireMods.Add(mayRequireMod);
            }
        }

        public override bool TryResolve(FailMode failReportMode) {
            bool flag = false;
            for (int i = 0; i < defNames.Count; i++) {
                bool flag2 = mayRequireMods != null && i < mayRequireMods.Count && !mayRequireMods[i].NullOrEmpty() /*&& !ModsConfig.IsActive(mayRequireMods[i])*/;
                T val = TryResolveDef<T>(defNames[i], (!flag2) ? failReportMode : FailMode.Silent, debugWanterInfo);
                if (val != null) {
                    ((List<T>) wanter).Add(val);
                    defNames.RemoveAt(i);
                    i--;
                }
                else {
                    flag = true;
                }
            }

            return !flag;
        }
    }

    private class WantedRefForDictionary<K, V> : WantedRef {
        private List<XmlNode> wantedDictRefs = new();

        private object debugWanterInfo;

        private List<Tuple<object, object>> makingData = new();

        public WantedRefForDictionary(object wanter, object debugWanterInfo) {
            this.wanter = wanter;
            this.debugWanterInfo = debugWanterInfo;
        }

        public void AddWantedDictEntry(XmlNode entryNode) {
            wantedDictRefs.Add(entryNode);
        }

        public override bool TryResolve(FailMode failReportMode) {
            failReportMode = FailMode.LogErrors;
            bool flag = typeof(Def).IsAssignableFrom(typeof(K));
            bool flag2 = typeof(Def).IsAssignableFrom(typeof(V));
            foreach (XmlNode wantedDictRef in wantedDictRefs) {
                XmlNode xmlNode = wantedDictRef[DirectXmlToObject.DictionaryKeyName];
                XmlNode xmlNode2 = wantedDictRef[DirectXmlToObject.DictionaryValueName];
                object first = (!flag) ? xmlNode : TryResolveDef<K>(xmlNode.InnerText, failReportMode, debugWanterInfo);
                object second = (!flag2) ? xmlNode2 : TryResolveDef<V>(xmlNode2.InnerText, failReportMode, debugWanterInfo);
                makingData.Add(new Tuple<object, object>(first, second));
            }

            return true;
        }

        public override void Apply() {
            Dictionary<K, V> dictionary = (Dictionary<K, V>) wanter;
            dictionary.Clear();
            foreach (Tuple<object, object> makingDatum in makingData) {
                try {
                    object obj = makingDatum.Item1;
                    object obj2 = makingDatum.Item2;
                    if (obj is XmlNode) {
                        obj = DirectXmlToObject.ObjectFromXml<K>(obj as XmlNode, doPostLoad: true);
                    }

                    if (obj2 is XmlNode) {
                        obj2 = DirectXmlToObject.ObjectFromXml<V>(obj2 as XmlNode, doPostLoad: true);
                    }

                    dictionary.Add((K) obj, (V) obj2);
                }
                catch {
                    Log.Error(string.Concat("Failed to load key/value pair: ", makingDatum.Item1, ", ", makingDatum.Item2));
                }
            }
        }
    }

    private static List<WantedRef> wantedRefs = new();

    private static Dictionary<object, WantedRef> wantedListDictRefs = new();

    public static bool LoadingInProgress => wantedRefs.Count > 0;

    public static void RegisterObjectWantsCrossRef(object wanter, FieldInfo fi, string targetDefName, string mayRequireMod = null, Type assumeFieldType = null) {
        WantedRefForObject item = new(wanter, fi, targetDefName, mayRequireMod, assumeFieldType);
        wantedRefs.Add(item);
    }

    public static void RegisterObjectWantsCrossRef(object wanter, string fieldName, string targetDefName, string mayRequireMod = null, Type overrideFieldType = null) {
        WantedRefForObject item = new(wanter, wanter.GetType().GetField(fieldName), targetDefName, mayRequireMod, overrideFieldType);
        wantedRefs.Add(item);
    }

    public static void RegisterListWantsCrossRef<T>(List<T> wanterList, string targetDefName, object debugWanterInfo = null, string mayRequireMod = null) {
        WantedRefForList<T> wantedRefForList = null;
        if (!wantedListDictRefs.TryGetValue(wanterList, out WantedRef value)) {
            wantedRefForList = new WantedRefForList<T>(wanterList, debugWanterInfo);
            wantedListDictRefs.Add(wanterList, wantedRefForList);
            wantedRefs.Add(wantedRefForList);
        }
        else {
            wantedRefForList = (WantedRefForList<T>) value;
        }

        wantedRefForList.AddWantedListEntry(targetDefName, mayRequireMod);
    }

    public static void RegisterDictionaryWantsCrossRef<K, V>(Dictionary<K, V> wanterDict, XmlNode entryNode, object debugWanterInfo = null) {
        WantedRefForDictionary<K, V> wantedRefForDictionary = null;
        if (!wantedListDictRefs.TryGetValue(wanterDict, out WantedRef value)) {
            wantedRefForDictionary = new WantedRefForDictionary<K, V>(wanterDict, debugWanterInfo);
            wantedRefs.Add(wantedRefForDictionary);
            wantedListDictRefs.Add(wanterDict, wantedRefForDictionary);
        }
        else {
            wantedRefForDictionary = (WantedRefForDictionary<K, V>) value;
        }

        wantedRefForDictionary.AddWantedDictEntry(entryNode);
    }

    public static T TryResolveDef<T>(string defName, FailMode failReportMode, object debugWanterInfo = null) {
        T val = (T) (object) DefRepository.GetDef(typeof(T), defName);
        if (val != null) {
            return val;
        }

        if (failReportMode == FailMode.LogErrors) {
            string text = string.Concat("Could not resolve cross-reference to ", typeof(T), " named ", defName);
            if (debugWanterInfo != null) {
                text = text + " (wanter=" + debugWanterInfo + ")";
            }

            Log.Error(text);
        }

        return default(T);
    }

    public static void Clear() {
        wantedRefs.Clear();
        wantedListDictRefs.Clear();
    }

    public static void ResolveAllWantedCrossReferences(FailMode failReportMode) {
        HashSet<WantedRef> resolvedRefs = new();
        foreach (WantedRef wantedRef in wantedRefs) {
            if (wantedRef.TryResolve(failReportMode)) {
                resolvedRefs.Add(wantedRef);
            }
        }

        /*GenThreading.ParallelForEach(wantedRefs, delegate(WantedRef wantedRef) {
            if (wantedRef.TryResolve(failReportMode)) {
                lock (resolvedRefsLock) {
                    resolvedRefs.Add(wantedRef);
                }
            }
        });*/
        foreach (WantedRef item in resolvedRefs) {
            item.Apply();
        }

        wantedRefs.RemoveAll(x => resolvedRefs.Contains(x));
    }
}