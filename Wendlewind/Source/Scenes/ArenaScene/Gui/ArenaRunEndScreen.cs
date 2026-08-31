using Image = Myra.Graphics2D.UI.Image;

namespace Wendlewind.Scenes.ArenaScene.Gui;

public sealed class ArenaRunEndScreen : VerticalStackPanel
{
    private static readonly Color Muted = new(160, 155, 145);
    private static readonly Color SpineWon = new(80, 140, 80);
    private static readonly Color SpineEmpty = new(50, 50, 55);
    private static readonly Color LifeLost = new(90, 40, 40);

    public ArenaRunEndScreen(GameContext context, Action onMenu)
    {
        Spacing = 16;
        Padding = new Thickness(28, 20);
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;

        var run = context.ArenaRun ?? throw new InvalidOperationException("Run end requires an ArenaRun.");
        var pawn = context.PlayerPawn;
        var victory = run.IsVictory;
        var titleColor = victory ? Color.Goldenrod : Color.IndianRed;

        Widgets.Add(BuildHeader(run, pawn, titleColor, victory));
        Widgets.Add(BuildStatRow(run));

        var columns = new Grid
        {
            ColumnSpacing = 16,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        columns.ColumnsProportions.Add(new Proportion(ProportionType.Part, 1));
        columns.ColumnsProportions.Add(new Proportion(ProportionType.Part, 1.25f));
        columns.RowsProportions.Add(new Proportion(ProportionType.Fill));

        var recap = BuildRecapCard(run);
        var kit = BuildKitCard(pawn);
        columns.Widgets.Add(recap);
        Grid.SetColumn(kit, 1);
        columns.Widgets.Add(kit);
        Widgets.Add(columns);
        SetProportionType(columns, ProportionType.Fill);

        var menu = new CursorButton(BaseContent.Styles.Button.LargeGold)
        {
            Content = new Label { Text = "Main Menu", HorizontalAlignment = HorizontalAlignment.Center },
            HorizontalAlignment = HorizontalAlignment.Center,
            MinWidth = 220,
            Margin = new Thickness(0, 4, 0, 0)
        };
        menu.Click += (_, _) => onMenu();
        Widgets.Add(menu);
    }

    private static Widget BuildHeader(ArenaRun run, Pawn pawn, Color titleColor, bool victory)
    {
        var header = new Grid
        {
            ColumnSpacing = 20,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        header.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        header.ColumnsProportions.Add(new Proportion(ProportionType.Fill));

        var portrait = new PawnRenderWidget(pawn, 160)
        {
            Width = 160,
            Height = 160,
            VerticalAlignment = VerticalAlignment.Center
        };
        header.Widgets.Add(portrait);

        var titleBlock = new VerticalStackPanel
        {
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Center,
            Widgets =
            {
                new Label(BaseContent.Styles.Label.Huge)
                {
                    Text = victory ? "Arena Champion" : "Run Over",
                    TextColor = titleColor
                },
                new Label(BaseContent.Styles.Label.Medium)
                {
                    Text = Subtitle(run, victory),
                    TextColor = Muted,
                    Wrap = true
                },
                new Label(BaseContent.Styles.Label.Small)
                {
                    Text = string.IsNullOrWhiteSpace(run.PlayerName)
                        ? $"{run.Wins}-{run.Losses}  ·  {run.FightsPlayed} fights"
                        : $"{run.PlayerName}  ·  {run.Wins}-{run.Losses}  ·  {run.FightsPlayed} fights",
                    TextColor = Color.Goldenrod
                },
                BuildFightSpine(run),
                BuildLivesRow(run)
            }
        };
        header.Widgets.Add(titleBlock);
        Grid.SetColumn(titleBlock, 1);
        return header;
    }

    private static string Subtitle(ArenaRun run, bool victory)
    {
        if (victory)
        {
            return "Ten victories. The arena yields.";
        }

        return run.Wins switch
        {
            0 => "Cut down before a single victory.",
            <= 2 => "A brief run through the dust.",
            <= 4 => "You made them work for it.",
            <= 6 => "A hard-fought showing.",
            <= 8 => "One more life would have changed everything.",
            _ => "The crown was nearly in reach."
        };
    }

    private static Widget BuildFightSpine(ArenaRun run)
    {
        var row = new HorizontalStackPanel
        {
            Spacing = 6,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        for (var i = 1; i <= ArenaRun.WinsToFinish; i++)
        {
            var won = i <= run.Wins;
            row.Widgets.Add(new Panel
            {
                Width = 28,
                Height = 28,
                Background = new ColoredRegion(
                    Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.RoundWhite64],
                    won ? SpineWon : SpineEmpty),
                Widgets =
                {
                    new Label(BaseContent.Styles.Label.Small)
                    {
                        Text = i.ToString(),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        TextColor = won ? Color.White : Muted
                    }
                }
            });
        }

        return row;
    }

    private static Widget BuildLivesRow(ArenaRun run)
    {
        var row = new HorizontalStackPanel
        {
            Spacing = 8,
            VerticalAlignment = VerticalAlignment.Center
        };
        row.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
        {
            Text = "Lives",
            TextColor = Muted,
            VerticalAlignment = VerticalAlignment.Center
        });

        for (var i = 0; i < ArenaRun.LossesToFinish; i++)
        {
            var remaining = i < run.LivesRemaining;
            row.Widgets.Add(new Panel
            {
                Width = 18,
                Height = 18,
                Background = new ColoredRegion(
                    Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.RoundWhite64],
                    remaining ? Color.IndianRed : LifeLost)
            });
        }

        return row;
    }

    private static Widget BuildStatRow(ArenaRun run)
    {
        return new Grid
        {
            ColumnSpacing = 12,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            ColumnsProportions =
            {
                new Proportion(ProportionType.Part, 1),
                new Proportion(ProportionType.Part, 1),
                new Proportion(ProportionType.Part, 1),
                new Proportion(ProportionType.Part, 1)
            },
            Widgets =
            {
                Place(StatTile("Wins", $"{run.Wins} / {ArenaRun.WinsToFinish}", Color.Goldenrod,
                    BaseContent.Styles.Atlas.Icon.Checkmark), 0),
                Place(StatTile("Losses", $"{run.Losses} / {ArenaRun.LossesToFinish}", Color.IndianRed,
                    BaseContent.Styles.Atlas.Icon.Skull), 1),
                Place(StatTile("Gold left", $"{run.Gold}g", Color.Goldenrod,
                    BaseContent.Styles.Atlas.Icon.Coin), 2),
                Place(StatTile("Fights", run.FightsPlayed.ToString(), Color.White,
                    BaseContent.Styles.Atlas.Icon.Combat), 3)
            }
        };
    }

    private static Widget Place(Widget widget, int column)
    {
        Grid.SetColumn(widget, column);
        return widget;
    }

    private static Widget StatTile(string label, string value, Color valueColor, string icon)
    {
        return new HorizontalStackPanel
        {
            Spacing = 10,
            Padding = new Thickness(12, 10),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrameBright],
            Widgets =
            {
                new Image
                {
                    Background = Stylesheet.Current.Atlas[icon],
                    Width = 36,
                    Height = 36,
                    VerticalAlignment = VerticalAlignment.Center
                },
                new VerticalStackPanel
                {
                    Spacing = 2,
                    VerticalAlignment = VerticalAlignment.Center,
                    Widgets =
                    {
                        new Label(BaseContent.Styles.Label.Small)
                        {
                            Text = label,
                            TextColor = Muted
                        },
                        new Label(BaseContent.Styles.Label.Medium)
                        {
                            Text = value,
                            TextColor = valueColor
                        }
                    }
                }
            }
        };
    }

    private static Widget BuildRecapCard(ArenaRun run)
    {
        var fightPurse = run.Wins * ArenaRun.WinGold + run.Losses * ArenaRun.LoseGold;
        var shopNet = run.Gold - ArenaRun.StartingGold - fightPurse;
        var lastFight = run.FightsPlayed == 0
            ? "No fights recorded"
            : run.LastFightWon
                ? "Last fight won"
                : "Last fight lost";

        var body = new VerticalStackPanel
        {
            Spacing = 6,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Widgets =
            {
                LedgerRow("Started with", $"{ArenaRun.StartingGold}g", Color.White),
                LedgerRow("Fight purse", $"+{fightPurse}g", Color.LightGreen),
                LedgerRow(
                    shopNet >= 0 ? "Sold in shops" : "Spent in shops",
                    shopNet >= 0 ? $"+{shopNet}g" : $"{shopNet}g",
                    shopNet >= 0 ? Color.LightGreen : Color.Salmon),
                new HorizontalSeparator { Margin = new Thickness(0, 4, 0, 4) },
                LedgerRow("Remaining", $"{run.Gold}g", Color.Goldenrod),
                new HorizontalSeparator { Margin = new Thickness(0, 8, 0, 8) },
                LedgerRow("Record", $"{run.Wins}–{run.Losses}", Color.White),
                LedgerRow("Opponents faced", run.FoughtPlayerIds.Count.ToString(), Color.White),
                LedgerRow("Lives left", run.LivesRemaining.ToString(), Color.White),
                new Label(BaseContent.Styles.Label.Small)
                {
                    Text = lastFight,
                    TextColor = run.LastFightWon ? Color.LightGreen : Color.Salmon,
                    Margin = new Thickness(0, 8, 0, 0)
                }
            }
        };

        return Section("Run recap", body);
    }

    private static Widget LedgerRow(string label, string value, Color valueColor)
    {
        var row = new Grid
        {
            ColumnSpacing = 12,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        row.ColumnsProportions.Add(new Proportion(ProportionType.Fill));
        row.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        var name = new Label(BaseContent.Styles.Label.Small)
        {
            Text = label,
            TextColor = Muted
        };
        var amount = new Label(BaseContent.Styles.Label.Small)
        {
            Text = value,
            TextColor = valueColor,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        row.Widgets.Add(name);
        Grid.SetColumn(amount, 1);
        row.Widgets.Add(amount);
        return row;
    }

    private static Widget BuildKitCard(Pawn pawn)
    {
        var body = new VerticalStackPanel
        {
            Spacing = 10,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        AddKitRow(body, "Gear", GearItems(pawn));
        AddKitRow(body, "Potions", pawn.Equipment.Potions.Where(Alive));
        AddKitRow(body, "Food", pawn.MealPlan.Items.Where(Alive));
        AddKitRow(body, "Medical", MedicalItems(pawn));
        AddKitRow(body, "Incense", IncenseItems(pawn));
        var meal = pawn.MealPlan.Items.ToHashSet();
        AddKitRow(body, "Pack", pawn.Inventory.Where(item => item is { IsDestroyed: false } && !meal.Contains(item)));

        if (body.Widgets.Count == 0)
        {
            body.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = "No kit remains.",
                TextColor = Muted
            });
        }

        var scroll = new ScrollViewer
        {
            Content = body,
            ShowHorizontalScrollBar = false,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        return Section("Final kit", scroll);
    }

    private static void AddKitRow(VerticalStackPanel body, string title, IEnumerable<KitIcon> items)
    {
        var cells = items.Select(IconCell).ToList();
        if (cells.Count == 0)
        {
            return;
        }

        body.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
        {
            Text = title,
            TextColor = Muted
        });
        body.Widgets.Add(WrapIcons(cells));
    }

    private static void AddKitRow(VerticalStackPanel body, string title, IEnumerable<Item> items)
    {
        AddKitRow(body, title, items.Select(item => new KitIcon(
            item.GetIconImage(),
            item.Label,
            item.Def.Description,
            item.StackSize > 1 ? $"x{item.StackSize}" : null)));
    }

    private static IEnumerable<KitIcon> GearItems(Pawn pawn)
    {
        foreach (var item in pawn.Equipment)
        {
            if (item == null || item.IsDestroyed || IsBuiltin(item) || item.ItemDef.ItemType == ItemType.Potion)
            {
                continue;
            }

            yield return new KitIcon(item.GetIconImage(), item.Label, item.Def.Description, null);
        }
    }

    private static IEnumerable<KitIcon> MedicalItems(Pawn pawn)
    {
        foreach (var slot in pawn.MedicalChest.Slots)
        {
            if (slot.Def == null)
            {
                continue;
            }

            yield return new KitIcon(
                slot.Def.GetIconImage(),
                slot.Def.Label,
                slot.Trigger.Describe(),
                slot.IsInfinite ? "∞" : slot.Charges.ToString());
        }
    }

    private static IEnumerable<KitIcon> IncenseItems(Pawn pawn)
    {
        foreach (var incense in pawn.ActiveIncense)
        {
            var itemDef = incense.SourceMoniker != null
                ? DefRepository<ItemDef>.GetByMoniker(incense.SourceMoniker, raiseError: false)
                : null;
            var name = incense.Def?.Label ?? itemDef?.Label ?? incense.SourceMoniker ?? "Incense";
            var left = incense.EncountersRemaining;
            IImage? icon = itemDef != null
                ? itemDef.GetIconImage()
                : incense.Def != null
                    ? new TextureRegion(incense.Def.GetTexture())
                    : null;
            yield return new KitIcon(icon, name, left == 1 ? "1 battle left" : $"{left} battles left", left.ToString());
        }
    }

    private static Widget WrapIcons(List<Widget> cells)
    {
        const int perRow = 8;
        var wrap = new VerticalStackPanel { Spacing = 6 };
        HorizontalStackPanel? line = null;
        for (var i = 0; i < cells.Count; i++)
        {
            if (i % perRow == 0)
            {
                line = new HorizontalStackPanel { Spacing = 6 };
                wrap.Widgets.Add(line);
            }

            line!.Widgets.Add(cells[i]);
        }

        return wrap;
    }

    private static Widget IconCell(KitIcon item)
    {
        var content = new Panel
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        content.Widgets.Add(new Image
        {
            Background = item.Icon,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        });
        if (!string.IsNullOrEmpty(item.Overlay))
        {
            content.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = item.Overlay,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
                TextColor = new Color(220, 180, 140)
            });
        }

        return new Panel
        {
            Width = 48,
            Height = 48,
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.IconFrame],
            Padding = new Thickness(4),
            Widgets = { content }
        }.WithTooltip(item.Title, item.Description);
    }

    private static Widget Section(string title, Widget content)
    {
        var panel = new VerticalStackPanel
        {
            Spacing = 8,
            Padding = new Thickness(14, 12),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrame],
            Widgets =
            {
                new Label(BaseContent.Styles.Label.Normal)
                {
                    Text = title,
                    TextColor = Color.Goldenrod,
                    HorizontalAlignment = HorizontalAlignment.Center
                },
                new HorizontalSeparator(),
                content
            }
        };
        VerticalStackPanel.SetProportionType(content, ProportionType.Fill);
        return panel;
    }

    private static bool Alive(Item? item) => item is { IsDestroyed: false };

    private static bool IsBuiltin(Item item) =>
        item.ItemDef.EquipmentProperties?.SlotUsedToEquip == EquipmentSlotType.BuiltIn;

    private readonly record struct KitIcon(IImage? Icon, string Title, string? Description, string? Overlay);
}
