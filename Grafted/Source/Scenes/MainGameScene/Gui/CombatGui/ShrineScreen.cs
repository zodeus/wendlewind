using Grafted.Sim.Entities.Pawns.Bodies;

namespace Grafted.Scenes.MainGameScene.Gui.CombatGui;

internal sealed class ShrineScreen : VerticalStackPanel
{
    private float _timeToDiscoverPart = 1;
    private int _partsRestored;
    private bool _praying;
    private readonly ZoneGui _gui;
    private readonly Pawn _pawn;
    private readonly ShrineProperties _shrine;
    private readonly int _maxNumberOfPartsToRestore;
    private readonly VerticalStackPanel _panel;

    public ShrineScreen(ZoneGui gui, Pawn playerPawn, ShrineProperties shrine)
    {
        _gui = gui;
        _pawn = playerPawn;
        _shrine = shrine;
        _maxNumberOfPartsToRestore = _shrine.PartsToRestore.RandomValue;
        HorizontalAlignment = HorizontalAlignment.Center;

        _panel = new VerticalStackPanel()
        {
            Spacing = 25,
            Width = 1600
        };
        Widgets.Add(new Label(BaseContent.Styles.Label.Huge)
        {
            Margin = new Thickness(0, 50, 0, 0),
            Text = _shrine.GodLabel
        });
        Widgets.Add(new Label(BaseContent.Styles.Label.Medium)
        {
            Margin = new Thickness(0, 0, 0, 20),
            Text = "It's shrine time, let's pray"
        });
        Widgets.Add(new HorizontalStackPanel
        {
            Spacing = 10,
            Widgets =
            {
                new Panel
                {
                    Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrame],
                    Padding = new Thickness(10),
                    Widgets =
                    {
                        new Image()
                        {
                            Background = new TextureRegion(shrine.Texture),
                            Width = 600, Height = 800
                        }
                    }
                },
                _panel
            }
        });


        var prayButton = new Button(BaseContent.Styles.Button.LargeGold)
        {
            Content = new Label(BaseContent.Styles.Label.Huge) { Text = "Pray" }
        };
        prayButton.Click += (_, _) =>
        {
            prayButton.Visible = false;
            Pray();
        };
        _panel.Widgets.Add(prayButton);
    }

    private void Pray()
    {
        _praying = true;
        _panel.Widgets.Add(new Label(BaseContent.Styles.Label.Medium) { Text = "You begin praying" });
    }

    public void Update(float deltaTime)
    {
        if (_praying == false) return;

        _timeToDiscoverPart -= deltaTime;
        if (_timeToDiscoverPart > 0) return;

        _timeToDiscoverPart = new RangeFloat(1.7f, 3).RandomValue;
        var potentialParts = _pawn.Body.AllExternalParts
            .SelectMany(p => p.Sockets.Where(s => s.AttachedPart == null));
        var part = potentialParts.InRandomOrder().FirstOrNull();
        TryToRestoreSeveredPart(part);
    }

    private void TryToRestoreSeveredPart(BodyPartSocket? part)
    {
        if (part == null)
        {
            EndMystery();
            return;
        }

        _partsRestored++;
        var socketFor = part.Def.AllowedBodyPartTypes;
        if (socketFor.Contains(BodyPartType.Finger) && _shrine.RestorablePartTypes.Contains(BodyPartType.Finger))
        {
            HumanBodyGenerator.MakeFingerForSocket(part, Defs.BodyParts.HumanFinger);
        }

        if (socketFor.Contains(BodyPartType.Thumb) && _shrine.RestorablePartTypes.Contains(BodyPartType.Thumb))
        {
            HumanBodyGenerator.MakeFingerForSocket(part, Defs.BodyParts.HumanThumb);
        }

        if (socketFor.Contains(BodyPartType.Hand) && _shrine.RestorablePartTypes.Contains(BodyPartType.Hand))
        {
            HumanBodyGenerator.MakeHandForSocket(part);
        }

        if (socketFor.Contains(BodyPartType.Foot) && _shrine.RestorablePartTypes.Contains(BodyPartType.Foot))
        {
            HumanBodyGenerator.MakeFootForSocket(part);
        }

        PrintResults(part);

        if (_partsRestored >= _maxNumberOfPartsToRestore)
        {
            EndMystery();
        }
    }

    private void PrintResults(BodyPartSocket part)
    {
        if (part.AttachedPart != null)
        {
            _panel.Widgets.Add(new Label(BaseContent.Styles.Label.Medium)
            {
                Text = $"Gaia smiles, you are gifted a {part.AttachedPart?.Label ?? "error_bug"}"
            });
        }
        else
        {
            _panel.Widgets.Add(new Label(BaseContent.Styles.Label.Medium)
            {
                TextColor = Color.Crimson,
                Text = "Gaia gazes upon the wound, she frowns...\nYour favor is too low"
            });
        }
    }

    private void EndMystery()
    {
        _praying = false;
        _panel.Widgets.Add(new Label(BaseContent.Styles.Label.Medium)
        {
            Text = $"The mystery has completed"
        });
        var leave = new Button(BaseContent.Styles.Button.Large)
        {
            Content = new Label(BaseContent.Styles.Label.Large) { Text = "Leave the shrine" }
        };
        leave.Click += (_, _) => _gui.LeaveShrine();
        _panel.Widgets.Add(leave);
    }
}