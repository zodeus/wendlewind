namespace Grafted.Sim;

public class GameMessages : IExposable {
    private List<Message> _messages = new();
    public IEnumerable<Message> All => _messages;
    public event Action<Message>? MessagePushed;

    public void Push(Message message) {
        if (_messages.Count > 1000) {
            Log.Warning("Game messages reached 1000, truncating memory array");
            _messages.Clear();
        }

        _messages.Add(message);
        MessagePushed?.Invoke(message);
    }

    public void ExposeData() {
        ScribeCollections.Look(ref _messages!, "List", LookMode.Deep);
    }
}

public struct Message : IExposable {
    private string _message;
    public Color? TextColor;

    public Message(string message, Color? textColor = null) {
        _message = message;
        TextColor = textColor;
    }

    public override string ToString() {
        return _message;
    }

    public void ExposeData() {
        ScribeValues.Look(ref _message, "Message");
        ScribeValues.Look(ref TextColor, "TextColor");
    }
}