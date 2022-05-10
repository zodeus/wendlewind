using System;
using System.Collections.Generic;
using System.Text;
using Grafted.Utils;

namespace Grafted.Sim.Persistence;

public class LoadIDsWantedBank {
    private struct IdRecord {
        public string TargetLoadId;

        public Type TargetType;

        public string PathRelToParent;

        public IExposable Parent;

        public IdRecord(string targetLoadId, Type targetType, string pathRelToParent, IExposable parent) {
            TargetLoadId = targetLoadId;
            TargetType = targetType;
            PathRelToParent = pathRelToParent;
            Parent = parent;
        }
    }

    private struct IdListRecord {
        public List<string>? TargetLoadIDs;

        public string PathRelToParent;

        public IExposable Parent;

        public IdListRecord(List<string>? targetLoadIDs, string pathRelToParent, IExposable parent) {
            TargetLoadIDs = targetLoadIDs;
            PathRelToParent = pathRelToParent;
            Parent = parent;
        }
    }

    private List<IdRecord> _idsRead = new();

    private List<IdListRecord> _idListsRead = new();

    public void ConfirmClear() {
        if (_idsRead.Count > 0 || _idListsRead.Count > 0) {
            StringBuilder stringBuilder = new();
            stringBuilder.AppendLine("Not all loadIDs which were read were consumed.");
            if (_idsRead.Count > 0) {
                stringBuilder.AppendLine("Singles:");
                for (int i = 0; i < _idsRead.Count; i++) {
                    stringBuilder.AppendLine(string.Concat("  ", _idsRead[i].TargetLoadId, " of type ", _idsRead[i].TargetType, ". pathRelToParent=", _idsRead[i].PathRelToParent,
                        ", parent=", _idsRead[i].Parent.ToString()));
                }
            }

            if (_idListsRead.Count > 0) {
                stringBuilder.AppendLine("Lists:");
                for (int j = 0; j < _idListsRead.Count; j++) {
                    stringBuilder.AppendLine("  List with " + ((_idListsRead[j].TargetLoadIDs != null) ? _idListsRead[j].TargetLoadIDs!.Count : 0) + " elements. pathRelToParent=" +
                                             _idListsRead[j].PathRelToParent + ", parent=" + _idListsRead[j].Parent);
                }
            }

            Log.Warning(stringBuilder.ToString());
        }

        Clear();
    }

    public void Clear() {
        _idsRead.Clear();
        _idListsRead.Clear();
    }

    public void RegisterLoadIdReadFromXml(string targetLoadId, Type targetType, string pathRelToParent, IExposable parent) {
        for (int i = 0; i < _idsRead.Count; i++) {
            if (_idsRead[i].Parent == parent && _idsRead[i].PathRelToParent == pathRelToParent) {
                Log.Error("Tried to register the same load ID twice: " + targetLoadId + ", pathRelToParent=" + pathRelToParent + ", parent=" + parent);
                return;
            }
        }

        _idsRead.Add(new IdRecord(targetLoadId, targetType, pathRelToParent, parent));
    }

    public void RegisterLoadIdReadFromXml(string targetLoadId, Type targetType, string toAppendToPathRelToParent) {
        string text = Scribe.Loader.CurPathRelToParent!;
        if (!toAppendToPathRelToParent.NullOrEmpty()) {
            text = text + "/" + toAppendToPathRelToParent;
        }

        RegisterLoadIdReadFromXml(targetLoadId, targetType, text, Scribe.Loader.CurParent!);
    }

    public void RegisterLoadIdListReadFromXml(List<string>? targetLoadIdList, string pathRelToParent, IExposable parent) {
        for (int i = 0; i < _idListsRead.Count; i++) {
            if (_idListsRead[i].Parent == parent && _idListsRead[i].PathRelToParent == pathRelToParent) {
                Log.Error("Tried to register the same list of load IDs twice. pathRelToParent=" + pathRelToParent + ", parent=" + parent);
                return;
            }
        }

        _idListsRead.Add(new IdListRecord(targetLoadIdList, pathRelToParent, parent));
    }

    public void RegisterLoadIdListReadFromXml(List<string>? targetLoadIdList, string? toAppendToPathRelToParent) {
        string text = Scribe.Loader.CurPathRelToParent!;
        if (!toAppendToPathRelToParent?.NullOrEmpty() ?? true) {
            text = text + "/" + toAppendToPathRelToParent;
        }

        RegisterLoadIdListReadFromXml(targetLoadIdList, text, Scribe.Loader.CurParent!);
    }

    public string? Take<T>(string pathRelToParent, IExposable parent) {
        for (int i = 0; i < _idsRead.Count; i++) {
            if (_idsRead[i].Parent == parent && _idsRead[i].PathRelToParent == pathRelToParent) {
                string targetLoadId = _idsRead[i].TargetLoadId;
                if (typeof(T) != _idsRead[i].TargetType) {
                    Log.Error(string.Concat("Trying to get load ID of object of type ", typeof(T), ", but it was registered as ", _idsRead[i].TargetType, ". pathRelToParent=", pathRelToParent,
                        ", parent=", parent.ToString()));
                }

                _idsRead.RemoveAt(i);
                return targetLoadId;
            }
        }

        Log.Error("Could not get load ID. We're asking for something which was never added during LoadingVars. pathRelToParent=" + pathRelToParent + ", parent=" + parent);
        return null;
    }

    public List<string> TakeList(string pathRelToParent, IExposable parent) {
        for (int i = 0; i < _idListsRead.Count; i++) {
            if (_idListsRead[i].Parent == parent && _idListsRead[i].PathRelToParent == pathRelToParent) {
                List<string> targetLoadIDs = _idListsRead[i].TargetLoadIDs!;
                _idListsRead.RemoveAt(i);
                return targetLoadIDs;
            }
        }

        Log.Error("Could not get load IDs list. We're asking for something which was never added during LoadingVars. pathRelToParent=" + pathRelToParent + ", parent=" + parent);
        return new List<string>();
    }
}