namespace Wendlewind.Scenes.MainGameScene.Gui.CombatGui;

internal sealed class MysteryScreen : VerticalStackPanel
{
    private readonly WheelOfParts? _wheel;

    public MysteryScreen(ZoneGui gui, Pawn playerPawn, MysteryProperties shrine)
    {
        HorizontalAlignment = HorizontalAlignment.Center;

        // Check if there are any missing parts that this mystery can actually restore
        var restorablePartTypes = playerPawn.Body.AllExternalParts
            .SelectMany(p => p.Sockets.Where(s => s.AttachedPart == null))
            .SelectMany(s => s.Def.AllowedBodyPartTypes)
            .Where(t => shrine.RestorablePartTypes.Contains(t))
            .Distinct()
            .ToList();

        if (restorablePartTypes.Count == 0)
        {
            ShowOptionalRewards(gui, playerPawn, shrine);
            return;
        }

        _wheel = new WheelOfParts(playerPawn, shrine);
        _wheel.OnSkipped += () => gui.LeaveMystery();
        Widgets.Add(_wheel);
    }

    public void Update(float deltaTime)
    {
        _wheel?.Update(deltaTime);
    }

    private void ShowOptionalRewards(ZoneGui gui, Pawn playerPawn, MysteryProperties shrine)
    {
        if (shrine.OptionalRewards.Count == 0 || shrine.OptionalRewardsCount <= 0)
        {
            ShowNoRewardsMessage(gui);
            return;
        }

        // Select random rewards
        var selectedRewards = shrine.OptionalRewards
            .InRandomOrder(Core.Context.Rng)
            .Take(shrine.OptionalRewardsCount)
            .Select(itemDef => Core.Context.Factory.CreateEntity<Item>(itemDef))
            .ToList();

        if (selectedRewards.Count == 0)
        {
            ShowNoRewardsMessage(gui);
            return;
        }

        // Title
        Widgets.Add(new Label(BaseContent.Styles.Label.Huge)
        {
            Margin = new Thickness(0, 50, 0, 0),
            Text = "Your Body is Whole",
            HorizontalAlignment = HorizontalAlignment.Center,
            TextColor = BaseContent.Colors.Text.Golden
        });

        // Subtitle
        Widgets.Add(new Label(BaseContent.Styles.Label.Medium)
        {
            Margin = new Thickness(0, 10, 0, 30),
            Text = "The shrine offers you a gift instead...",
            HorizontalAlignment = HorizontalAlignment.Center,
            TextColor = new Color(180, 180, 180)
        });

        // Items row
        var itemRow = new HorizontalStackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Spacing = 20
        };

        foreach (var item in selectedRewards)
        {
            itemRow.Widgets.Add(new VerticalStackPanel
            {
                Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.DeepGold],
                Padding = new Thickness(15),
                Spacing = 5,
                Widgets =
                {
                    new Image
                    {
                        Background = item.ItemDef.GetIconImage(),
                        Width = 128,
                        Height = 128,
                        HorizontalAlignment = HorizontalAlignment.Center
                    },
                    new HorizontalSeparator { Margin = new Thickness(0, 0, 0, 10) },
                    new Label(BaseContent.Styles.Label.Large)
                    {
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Text = item.ItemDef.Label,
                    },
                    new Label(BaseContent.Styles.Label.Large)
                    {
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 0, 0, 10),
                        Visible = item.ItemDef.StackLimit > 1,
                        Text = $"x{item.StackSize}"
                    }
                }
            });
        }

        Widgets.Add(itemRow);

        // Continue button
        var continueButton = new CursorButton(BaseContent.Styles.Button.LargeGold)
        {
            Content = new Label(BaseContent.Styles.Label.Large) { Text = "Accept Gift" },
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 30, 0, 0)
        };
        continueButton.Click += (_, _) =>
        {
            foreach (var item in selectedRewards)
            {
                playerPawn.Inventory.TryAdd(item);
            }
            gui.LeaveMystery();
        };

        Widgets.Add(continueButton);
    }

    private void ShowNoRewardsMessage(ZoneGui gui)
    {
        Widgets.Add(new Label(BaseContent.Styles.Label.Huge)
        {
            Margin = new Thickness(0, 50, 0, 0),
            Text = "Your Body is Whole",
            HorizontalAlignment = HorizontalAlignment.Center,
            TextColor = BaseContent.Colors.Text.Golden
        });

        Widgets.Add(new Label(BaseContent.Styles.Label.Medium)
        {
            Margin = new Thickness(0, 10, 0, 30),
            Text = "The shrine has nothing more to offer...",
            HorizontalAlignment = HorizontalAlignment.Center,
            TextColor = new Color(140, 140, 140)
        });

        var button = new CursorButton(BaseContent.Styles.Button.Large)
        {
            Content = new Label(BaseContent.Styles.Label.Large) { Text = "Leave" },
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 20, 0, 0)
        };
        button.Click += (_, _) => gui.LeaveMystery();
        Widgets.Add(button);
    }
}