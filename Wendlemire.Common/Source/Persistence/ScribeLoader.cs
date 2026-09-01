using System.IO;
using System.Xml;

namespace Wendlemire.Sim.Persistence;

public class ScribeLoader {
    public readonly CrossRefHandler CrossRefs = new CrossRefHandler();

    public readonly PostLoadInitializer Initializer = new PostLoadInitializer();

    public IExposable? CurParent;

    public XmlNode? CurXmlParent;

    public string? CurPathRelToParent;

    public void InitLoading(string filePath) {
        if (Scribe.State != 0) {
            Log.Error("Called InitLoading() but current mode is " + Scribe.State);
            Scribe.ForceStop();
        }

        if (CurParent != null) {
            Log.Error("Current parent is not null in InitLoading");
            CurParent = null;
        }

        if (CurPathRelToParent != null) {
            Log.Error("Current path relative to parent is not null in InitLoading");
            CurPathRelToParent = null;
        }

        try {
            using (var input = new StreamReader(filePath)) {
                using (var reader = new XmlTextReader(input)) {
                    var xmlDocument = new XmlDocument();
                    xmlDocument.Load(reader);
                    CurXmlParent = xmlDocument.DocumentElement;
                }
            }

            Scribe.State = ScribeState.LoadingObjects;
        }
        catch (Exception ex) {
            Log.Error("Exception while init loading file: " + filePath + "\n" + ex);
            ForceStop();
            throw;
        }
    }

    public void FinalizeLoading() {
        if (Scribe.State != ScribeState.LoadingObjects) {
            Log.Error("Called FinalizeLoading() but current mode is " + Scribe.State);
        }
        else {
            try {
                Scribe.ExitNode();
                CurXmlParent = null;
                CurParent = null;
                CurPathRelToParent = null;
                Scribe.State = ScribeState.Inactive;
                CrossRefs.ResolveAllCrossReferences();
                Initializer.DoAllPostLoadInits();
            }
            catch (Exception arg) {
                Log.Error("Exception in FinalizeLoading(): " + arg);
                ForceStop();
                throw;
            }
        }
    }

    public bool EnterNode(string nodeName) {
        if (CurXmlParent != null) {
            XmlNode? xmlNode = CurXmlParent[nodeName];
            if (xmlNode == null && char.IsDigit(nodeName[0])) {
                xmlNode = CurXmlParent.ChildNodes[int.Parse(nodeName)];
            }

            if (xmlNode == null) {
                return false;
            }

            CurXmlParent = xmlNode;
        }

        CurPathRelToParent = CurPathRelToParent + "/" + nodeName;
        return true;
    }

    public void ExitNode() {
        CurXmlParent = CurXmlParent?.ParentNode;

        if (CurPathRelToParent == null) return;

        var num = CurPathRelToParent.LastIndexOf('/');
        CurPathRelToParent = num > 0 ? CurPathRelToParent.Substring(0, num) : null;
    }

    public void ForceStop() {
        CurXmlParent = null;
        CurParent = null;
        CurPathRelToParent = null;
        CrossRefs.Clear(errorIfNotEmpty: false);
        Initializer.Clear();
        if (Scribe.State == ScribeState.LoadingObjects || Scribe.State == ScribeState.ResolvingCrossReferences || Scribe.State == ScribeState.PostLoadInitialization) {
            Scribe.State = ScribeState.Inactive;
        }
    }
}