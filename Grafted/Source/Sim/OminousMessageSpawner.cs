using System.Collections.Generic;
using Grafted.Maths;
using Grafted.Sim.Gui;
using Grafted.Utils;
using Microsoft.Xna.Framework;

namespace Grafted.Sim;

public class OminousMessageSpawner {
    private Dictionary<int, ScreenMessageData> _messages = new();

    public OminousMessageSpawner() {
        List<ScreenMessageData> messages = new List<ScreenMessageData> {
            new() {
                Text = "all is calm, for now...",
                Color = Color.DarkSeaGreen
            },
            new() {
                Text = "the pages of the old books have powers undiscovered",
                Color = Color.DarkSeaGreen
            },
            new() {
                Text = "a strange pulse move over the land...",
                Color = Color.DarkOliveGreen
            },
            new() {
                Text = "legend says, elves live deep in the forests...",
                Color = Color.DarkOliveGreen
            },
            new() {
                Text = "crawley critters and creepy creatures,that's what the nights bring",
                Color = Color.DarkViolet
            },
            new() {
                Text = "the air feels heavy and tastes of metal",
                Color = new Color(79, 184, 140)
            },
            new() {
                Text = "%$$@$%% you've enraged the cow gods. herp, herp, ...",
                Color = new Color(82, 27, 0)
            },
            new() {
                Text = "Keep your eyes open for blood sippers, without proper care they can be quite troublesome",
                Color = new Color(82, 27, 0)
            },
            new() {
                Text = "there seems to be a strange hum in the air, curious...\nis it coming from the moons? do they speak to each other?",
                Font = BaseContent.Fonts.Fancy.Large,
                Color = Color.DarkOrange
            }
        };
        RangeInt daysRange = new(9, 12);
        int nextMessageDay = daysRange.RandomValue;
        foreach (ScreenMessageData message in messages.InRandomOrder()) {
            RegisterSpawn(message, nextMessageDay, nextMessageDay);
            nextMessageDay += daysRange.RandomValue;
        }
    }

    private void RegisterSpawn(ScreenMessageData messageData, int daysBeginRange, int daysEndRange) {
        _messages[Core.Random.Next(SimTime.TicksPerDay * daysBeginRange, SimTime.TicksPerDay * daysEndRange)] = messageData;
    }

    public void Tick() {
        if (_messages.ContainsKey(Core.Sim.Ticks)) {
            Core.Sim.Gui!.PushScreenMessage(_messages[Core.Sim.Ticks]);
        }
    }
}