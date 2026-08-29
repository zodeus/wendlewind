using System.Xml;
using Wendlewind.Definitions.Loader;

namespace Wendlewind.Sim.Persistence;

public static class ScribeExtractor {
    public static T? ValueFromNode<T>(XmlNode? subNode, T defaultValue) {
        if (subNode == null) {
            return defaultValue;
        }

        var xmlAttribute = subNode.Attributes!["IsNull"];
        if (xmlAttribute != null && xmlAttribute.Value.ToLower() == "true") {
            return default;
        }

        try {
            try {
                return ParseHelper.FromString<T>(subNode.InnerText);
            }
            catch (Exception ex) {
                Log.Error(string.Concat("Exception parsing node ", subNode.OuterXml, " into a ", typeof(T), ":\n", ex.ToString()));
            }

            return default;
        }
        catch (Exception arg) {
            Log.Error("Exception loading XML: " + arg);
            return defaultValue;
        }
    }

    public static T? DefFromNode<T>(XmlNode? subNode) where T : Def, new() {
        if (subNode?.InnerText == null || subNode.InnerText == "null") {
            return null;
        }

        var text = subNode.InnerText;
        var namedSilentFail = DefRepository<T>.GetByMoniker(text);
        //T namedSilentFail = DefDatabase<T>.GetNamedSilentFail(text);
        if (namedSilentFail == null) {
            if (text == subNode.InnerText) {
                Log.Error(string.Concat("Could not load reference to ", typeof(T), " named ", subNode.InnerText));
            }
            else {
                Log.Error(string.Concat("Could not load reference to ", typeof(T), " named ", subNode.InnerText, " after compatibility-conversion to ", text));
            }
        }

        return namedSilentFail;
    }

    public static T DefFromNodeUnsafe<T>(XmlNode subNode) {
        return (T) GenericHelpers.InvokeStaticGenericMethod(typeof(ScribeExtractor), typeof(T), "DefFromNode", subNode);
    }

    public static T? SaveableFromNode<T>(XmlNode? subNode, object[] ctorArgs) {
        if (Scribe.State != ScribeState.LoadingObjects) {
            Log.Error("Called SaveableFromNode(), but mode is " + Scribe.State);
            return default;
        }

        if (subNode == null) {
            return default;
        }

        var xmlAttribute = subNode.Attributes?["IsNull"];
        if (xmlAttribute != null && xmlAttribute.Value.ToLower() == "true") {
            return default;
        }

        try {
            var xmlAttribute2 = subNode.Attributes?["Class"];
            var text = xmlAttribute2 != null ? xmlAttribute2.Value : typeof(T).FullName;
            var type = GenTypes.GetTypeInAnyAssembly(text);
            if (type == null) {
                //Type bestFallbackType = GetBestFallbackType<T>(subNode);
                Log.Error(string.Concat("Could not find class ", text, " while resolving node ", subNode.Name, ". Trying to use ", /*bestFallbackType,*/ " instead. Full node: ",
                    subNode.OuterXml));
                //type = bestFallbackType;
                throw new Exception();
            }

            if (type.IsAbstract) {
                throw new ArgumentException("Can't load abstract class " + type);
            }

            var exposable = (IExposable?) Activator.CreateInstance(type, ctorArgs);
            var flag = typeof(T).IsValueType /*|| typeof(Name).IsAssignableFrom(typeof(T))*/;
            if (!flag) {
                Scribe.Loader.CrossRefs.RegisterForCrossRefResolve(exposable);
            }

            var curXmlParent = Scribe.Loader.CurXmlParent;
            var curParent = Scribe.Loader.CurParent;
            var curPathRelToParent = Scribe.Loader.CurPathRelToParent;
            Scribe.Loader.CurXmlParent = subNode;
            Scribe.Loader.CurParent = exposable;
            Scribe.Loader.CurPathRelToParent = null;
            try {
                exposable!.ExposeData();
            }
            finally {
                Scribe.Loader.CurXmlParent = curXmlParent;
                Scribe.Loader.CurParent = curParent;
                Scribe.Loader.CurPathRelToParent = curPathRelToParent;
            }

            if (!flag) {
                Scribe.Loader.Initializer.RegisterForPostLoadInit(exposable);
            }
            
            return (T) exposable;
        }
        catch (Exception ex) {
            T result = default!;
            Log.Error(string.Concat("SaveableFromNode exception: ", ex, "\nSubnode:\n", subNode.OuterXml));
            return result;
        }
    }

    //private static Type GetBestFallbackType<T>(XmlNode node) {
        /*
        if (typeof(Entity).IsAssignableFrom(typeof(T))) {
            Def entityDef = TryFindDef<Def>(node, "def");
            if (entityDef != null) {
                return entityDef.entityClass;
            }
        }
        */

        /*else if (typeof(Hediff).IsAssignableFrom(typeof(T)))
        {
            HediffDef hediffDef = TryFindDef<HediffDef>(node, "def");
            if (hediffDef != null)
            {
                return hediffDef.hediffClass;
            }
        }
        else if (typeof(Ability).IsAssignableFrom(typeof(T)))
        {
            AbilityDef abilityDef = TryFindDef<AbilityDef>(node, "def");
            if (abilityDef != null)
            {
                return abilityDef.abilityClass;
            }
        }
        else if (typeof(Thought).IsAssignableFrom(typeof(T)))
        {
            ThoughtDef thoughtDef = TryFindDef<ThoughtDef>(node, "def");
            if (thoughtDef != null)
            {
                return thoughtDef.thoughtClass;
            }
        }*/
        //return typeof(T);
    //}

    /*private static TDef TryFindDef<TDef>(XmlNode node, string defNodeName) where TDef : Def, new() {
        var xmlElement = node[defNodeName];
        if (xmlElement == null) {
            return null;
        }

        //return DefDatabase<TDef>.GetNamedSilentFail(xmlElement.InnerText);
        return DefRepository<TDef>.GetByMoniker(xmlElement.InnerText);
    }*/

    /*    
    public static TargetInfo TargetInfoFromNode(XmlNode node, string label, TargetInfo defaultValue)
    {
        LoadIDsWantedBank loadIDs = Scribe.loader.crossRefs.loadIDs;
        if (node != null && Scribe.EnterNode(label))
        {
            try
            {
                string innerText = node.InnerText;
                if (innerText.Length != 0 && innerText[0] == '(')
                {
                    ExtractCellAndMapPairFromTargetInfo(innerText, out string cell, out string map);
                    loadIDs.RegisterLoadIDReadFromXml(null, typeof(Thing), "thing");
                    loadIDs.RegisterLoadIDReadFromXml(map, typeof(Map), "map");
                    return new TargetInfo(IntVec3.FromString(cell), null, allowNullMap: true);
                }
                loadIDs.RegisterLoadIDReadFromXml(innerText, typeof(Thing), "thing");
                loadIDs.RegisterLoadIDReadFromXml(null, typeof(Map), "map");
                return TargetInfo.Invalid;
            }
            finally
            {
                Scribe.ExitNode();
            }
        }
        loadIDs.RegisterLoadIDReadFromXml(null, typeof(Thing), label + "/thing");
        loadIDs.RegisterLoadIDReadFromXml(null, typeof(Map), label + "/map");
        return defaultValue;
    }
    */

    /*public static TargetInfo ResolveTargetInfo(TargetInfo loaded, string label) {
        if (Scribe.EnterNode(label)) {
            try {
                Entity entity = Scribe.Loader.CrossRefs.TakeResolvedRef<Entity>("entity");
                //ap map = Scribe.loader.crossRefs.TakeResolvedRef<Map>("map");
                Vector2Int cell = loaded.Cell;
                if (entity != null) {
                    return new TargetInfo(entity);
                }

                //if (cell.IsValid && map != null) {
                return new TargetInfo(cell /*, map#1#);
                //}

                return TargetInfo.Invalid;
            }
            finally {
                Scribe.ExitNode();
            }
        }

        return loaded;
    }*/
}