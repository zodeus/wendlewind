using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Grafted.Sim;

public class SimulationMessages {
    private readonly List<Message> _messages = new();
    public IEnumerable<Message> All => _messages;
    public event Action<Message>? MessagePushed;

    public void Push(Message message) {
        if (_messages.Count > 1000) {
            Log.Warning("Simulation messages reached 1000, truncating memory array");
            _messages.Clear();
        }

        _messages.Add(message);
        MessagePushed?.Invoke(message);
    }
}

public readonly struct Message {
    private readonly string _message;
    public readonly Color? TextColor;

    public Message(string message, Color? textColor = null) {
        _message = message;
        TextColor = textColor;
    }

    public override string ToString() {
        return _message;
    }
}