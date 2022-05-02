using System;
using System.Collections.Generic;
using Grafted.Definitions;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;
using HorizontalAlignment = Myra.Graphics2D.UI.HorizontalAlignment;
using Label = Myra.Graphics2D.UI.Label;

namespace Grafted.Sim.Gui;

public class DialogueGui : BaseGui {
    private readonly VerticalStackPanel _panel;

    public DialogueGui(DialogueNode rootNode) {
        _panel = new VerticalStackPanel {
            HorizontalAlignment = HorizontalAlignment.Center, Padding = new Thickness(50),
            Margin = new Thickness(0, 50, 0, 0), Spacing = 40
        };

        SetNode(rootNode);
        Desktop = new Desktop { Root = _panel, HasExternalTextInput = true };
    }

    private void SetNode(DialogueNode node) {
        _panel.Widgets.Clear();
        _panel.AddChild(new Label(BaseContent.Styles.Label.Large) {
            Text = node.Text,
            HorizontalAlignment = HorizontalAlignment.Center
        });
        HorizontalStackPanel buttons = new() {
            Spacing = 10,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        foreach (DialogueNodeOption option in node.Options) {
            TextButton button = new(BaseContent.Styles.Button.Large) {
                Text = option.Text
            };
            button.Click += (_, _) => {
                if (option.Node != null) { SetNode(option.Node); }
                else {
                    option.ClickHandler?.Invoke();
                }
            };
            buttons.AddChild(button);
        }

        _panel.AddChild(buttons);
    }
}

public class DialogueNode {
    public string Text { get; init; } = null!;
    public List<DialogueNodeOption> Options { get; init; } = null!;
}

public class DialogueNodeOption {
    public string Text = "undefined";
    public DialogueNode? Node;
    public Action? ClickHandler;
}

public static class DialogueGenerator {
    private static int _nextDialogueId = 0;

    public static DialogueNode Generate() {
        _nextDialogueId++;
        if (_nextDialogueId == 1) {
            return new DialogueNode {
                Text = "You've been banished to The Village of the Damned,\nalong with fifteen others.\nThere is only a single claim available.",
                Options = new List<DialogueNodeOption> {
                    new() {
                        Text = "Continue",
                        Node = new DialogueNode {
                            Text = "Only one of you gets to stay.",
                            Options = new List<DialogueNodeOption> {
                                new() {
                                    Text = "Commence the Culling of the Fresh",
                                    ClickHandler = () => Core.Sim.ActivateCombatEvent(Core.Sim.World.NextCombat())
                                }
                            }
                        }
                    }
                }
            };
        }

        if (_nextDialogueId == 2) {
            return new DialogueNode {
                Text = "The other recusant have fallen.\n\nThe blood judge approaches,\nhe hands you a key and points\nto a building in the distance.",
                Options = new List<DialogueNodeOption> {
                    new() {
                        Text = "Head towards the building",
                        ClickHandler = () => Core.Sim.Gui = new DialogueGui(Core.Sim.World.NextDialogue())
                    }
                }
            };
        }


        if (_nextDialogueId == 3) {
            return new DialogueNode {
                Text = "This island is crawling with beasts,\ndo not travel unprepared.",
                Options = new List<DialogueNodeOption> {
                    new() {
                        Text = "Run",
                        ClickHandler = () => Core.Sim.Gui = new DialogueGui(Core.Sim.World.NextDialogue())
                    }
                }
            };
        }

        if (_nextDialogueId == 4) {
            return new DialogueNode {
                Text = "This ramshackle structure will be your home.",
                Options = new List<DialogueNodeOption> {
                    new() {
                        Text = "Continue",
                        Node = new DialogueNode {
                            Text = "There are many other recusant occupying the village,\nbe wary.",
                            Options = new List<DialogueNodeOption> {
                                new() {
                                    Text = "Enter your home",
                                    ClickHandler = () => {
                                        Core.Sim.World.MoveToZone(Defs.Zones.VillageOfTheDamned);
                                        Core.Sim.Gui = new TownGui(Core.Sim.World.CurrentZone.Town!);
                                    }
                                }
                            }
                        }
                    }
                }
            };
        }

        return null;
    }

    /*public static DialogueNode HammerHenryDialogue() {
        return new DialogueNode {
            Text = "You exit the house and start wandering the village streets.\nIn the distance you see a man working outside...",
            Options = new List<DialogueNodeOption> {
                new() {
                    Text = "Approach him",
                    Node = new DialogueNode {
                        Text = "You see him glaring at you as you approach.",
                        Options = new List<DialogueNodeOption> {
                            new() {
                                Text = "Ask him for a hammer",
                                Node = new DialogueNode {
                                    Text = "He smirks, and grumbles something that sounds like \"hand for a hammer\".",
                                    Options = new List<DialogueNodeOption> {
                                        new() {
                                            Text = "Offer your hand for the hammer",
                                            ClickHandler = () => Core.Sim.Gui = new TownGui()
                                        },
                                        new() {
                                            Text = "Take the hammer",
                                            ClickHandler = () => Core.Sim.Gui = new TownGui()
                                        }
                                    },
                                }
                            }
                        }
                    }
                }
            }
        };
    }*/
}