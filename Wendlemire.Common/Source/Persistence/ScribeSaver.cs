using System.IO;
using System.Xml;

namespace Wendlemire.Sim.Persistence;

public class ScribeSaver {
    private Stream? _saveStream;

    private XmlWriter? _writer;

    private string? _curPath;

    private bool _anyInternalException;

    public void InitSaving(string filePath, string documentElementName) {
        if (Scribe.State != 0) {
            Log.Error("Called InitSaving() but current mode is " + Scribe.State);
            Scribe.ForceStop();
        }

        if (_curPath != null) {
            Log.Error("Current path is not null in InitSaving");
            _curPath = null;
        }

        try {
            Scribe.State = ScribeState.Saving;
            _saveStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
            var xmlWriterSettings = new XmlWriterSettings
                { Indent = true, IndentChars = "\t" };
            _writer = XmlWriter.Create(_saveStream, xmlWriterSettings);
            _writer.WriteStartDocument();
            EnterNode(documentElementName);
        }
        catch (Exception ex) {
            Log.Error("Exception while init saving file: " + filePath + "\n" + ex);
            ForceStop();
            throw;
        }
    }

    public void FinalizeSaving() {
        if (Scribe.State != ScribeState.Saving) {
            Log.Error("Called FinalizeSaving() but current mode is " + Scribe.State);
            return;
        }

        if (_anyInternalException) {
            ForceStop();
            throw new Exception("Can't finalize saving due to internal exception. The whole file would be most likely corrupted anyway.");
        }

        try {
            if (_writer != null) {
                ExitNode();
                _writer.WriteEndDocument();
                _writer.Flush();
                _writer.Close();
                _writer = null;
            }

            if (_saveStream != null) {
                _saveStream.Flush();
                _saveStream.Close();
                _saveStream = null;
            }

            Scribe.State = ScribeState.Inactive;
            //loadIDsErrorsChecker.CheckForErrorsAndClear();
            _curPath = null;
            _anyInternalException = false;
        }
        catch (Exception arg) {
            Log.Error("Exception in FinalizeLoading(): " + arg);
            ForceStop();
            throw;
        }
    }

    public void WriteElement(string elementName, string? value) {
        if (_writer == null) {
            Log.Error("Called WriteElemenet(), but writer is null.");
        }
        else {
            try {
                _writer.WriteElementString(elementName, value);
            }
            catch (Exception) {
                _anyInternalException = true;
                throw;
            }
        }
    }

    public void WriteAttribute(string attributeName, string value) {
        if (_writer == null) {
            Log.Error("Called WriteAttribute(), but writer is null.");
        }
        else {
            try {
                _writer.WriteAttributeString(attributeName, value);
            }
            catch (Exception) {
                _anyInternalException = true;
                throw;
            }
        }
    }

    public bool EnterNode(string nodeName) {
        if (_writer == null) {
            return false;
        }

        try {
            _writer.WriteStartElement(nodeName);
        }
        catch (Exception) {
            _anyInternalException = true;
            throw;
        }

        return true;
    }

    public void ExitNode() {
        if (_writer != null) {
            try {
                _writer.WriteEndElement();
            }
            catch (Exception) {
                _anyInternalException = true;
                throw;
            }
        }
    }

    public void ForceStop() {
        if (_writer != null) {
            _writer.Close();
            _writer = null;
        }

        if (_saveStream != null) {
            _saveStream.Close();
            _saveStream = null;
        }

        //loadIDsErrorsChecker.Clear();
        _curPath = null;
        _anyInternalException = false;
        if (Scribe.State == ScribeState.Saving) {
            Scribe.State = ScribeState.Inactive;
        }
    }
}