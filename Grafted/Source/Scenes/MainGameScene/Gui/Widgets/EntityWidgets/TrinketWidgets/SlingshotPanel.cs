using Grafted.Sim.Entities.Items;
using Grafted.Sim.Entities.Items.Trinkets;

namespace Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.TrinketWidgets;

[UsedImplicitly]
public sealed class SlingshotPanel : EntityPanelBase
{
    private readonly SlingshotHandler _handler;

    private readonly VerticalStackPanel _ammoListPanel;
    private readonly HorizontalStackPanel _loadedAmmoPanel;
    private readonly Label _loadedAmmoLabel;

    public SlingshotPanel(BaseGui gui, Item item, EntityPanelProperties? properties = null) : base(gui, item, properties)
    {
        _handler = (SlingshotHandler)item.TrinketHandler!;
        Padding = new Thickness(20);
        Width = 400;
        Height = 500;

        // Header with icon and title
        var header = new HorizontalStackPanel
        {
            Spacing = 15,
            Margin = new Thickness(0, 0, 0, 10),
            Widgets =
            {
                new Image
                {
                    Background = new TextureRegion(item.Icon),
                    Width = 64, Height = 64
                },
                new VerticalStackPanel
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    Widgets =
                    {
                        new Label(BaseContent.Styles.Label.Normal)
                        {
                            Text = item.Def.Description,
                            TextColor = Color.LightGray,
                            Wrap = true,
                            MaxWidth = 280
                        }
                    }
                }
            }
        };
        Widgets.Add(header);

        // Separator
        Widgets.Add(new HorizontalSeparator { Margin = new Thickness(0, 5, 0, 15) });

        // Loaded ammo section
        Widgets.Add(new Label(BaseContent.Styles.Label.Normal)
        {
            Text = "Loaded Ammo",
            TextColor = Color.DarkGoldenrod,
            Margin = new Thickness(0, 0, 0, 5),
        });

        _loadedAmmoLabel = new Label(BaseContent.Styles.Label.Normal)
        {
            Text = "None",
            TextColor = Color.Gray,
            VerticalAlignment = VerticalAlignment.Center
        };

        _loadedAmmoPanel = new HorizontalStackPanel
        {
            Spacing = 10,
            Margin = new Thickness(0, 0, 0, 15),
            MinHeight = 60
        };
        RefreshLoadedAmmoDisplay();
        Widgets.Add(_loadedAmmoPanel);

        // Available ammo section
        Widgets.Add(new HorizontalSeparator { Margin = new Thickness(0, 5, 0, 10) });
        Widgets.Add(new Label(BaseContent.Styles.Label.Normal)
        {
            Text = "Available Ammo",
            TextColor = Color.DarkGoldenrod,
            Margin = new Thickness(0, 0, 0, 5)
        });

        _ammoListPanel = new VerticalStackPanel { Spacing = 5 };
        Widgets.Add(new ScrollViewer
        {
            Content = _ammoListPanel,
            MaxHeight = 200,
            VerticalAlignment = VerticalAlignment.Stretch
        });

        RefreshAmmoList();
    }

    private void RefreshLoadedAmmoDisplay()
    {
        _loadedAmmoPanel.Widgets.Clear();

        if (_handler.Ammo != null)
        {
            var ammoProps = _handler.Ammo.ItemDef.AmmoProperties;

            _loadedAmmoPanel.Widgets.Add(new EntityIcon(_handler.Ammo, BaseContent.IconSizes.Default){
            VerticalAlignment = VerticalAlignment.Center});
            _loadedAmmoPanel.Widgets.Add(new VerticalStackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                Widgets =
                {
                    new Label(BaseContent.Styles.Label.Normal)
                    {
                        Text = _handler.Ammo.LabelWithStackSize,
                        TextColor = Color.White
                    },
                    new HorizontalStackPanel
                    {
                        Spacing = 10,
                        Widgets =
                        {
                            new Label(BaseContent.Styles.Label.Small)
                            {
                                Text = ammoProps?.DamageType.ToString() ?? "None",
                                TextColor = GetDamageTypeColor(ammoProps?.DamageType ?? DamageType.Invalid)
                            },
                            new Label(BaseContent.Styles.Label.Small)
                            {
                                Text = ammoProps != null ? $"{ammoProps.DamageRange.Min:F0}-{ammoProps.DamageRange.Max:F0} dmg" : "",
                                TextColor = Color.LightGray
                            }
                        }
                    }
                }
            });

            var unloadButton = new Button(BaseContent.Styles.Button.Normal)
            {
                Content = new Label { Text = "Unload", TextColor = Color.White },
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 0, 0, 0)
            };
            unloadButton.TouchDown += (_, _) =>
            {
                UnloadAmmo();
            };
            _loadedAmmoPanel.Widgets.Add(unloadButton);
        }
        else
        {
            _loadedAmmoPanel.Widgets.Add(_loadedAmmoLabel);
        }
    }

    private void RefreshAmmoList()
    {
        _ammoListPanel.Widgets.Clear();

        foreach (var item in Core.Context.PlayerPawn.Inventory)
        {
            // Only show stackable items that aren't trinkets as potential ammo
            if (item.ItemDef.AmmoProperties == null) continue;
            if (item.ItemDef.StackLimit <= 1) continue;
            if (item == _handler.Ammo) continue; // Don't show already loaded ammo

            var ammoRow = new HorizontalStackPanel
            {
                Spacing = 10,
                Margin = new Thickness(0, 2, 0, 2)
            };

            var loadButton = new Button(BaseContent.Styles.Button.Normal)
            {
                VerticalAlignment = VerticalAlignment.Center,
                Content = new Image
                {
                    Width = BaseContent.IconSizes.Small,
                    Height = BaseContent.IconSizes.Small,
                    Background = new ColoredRegion(
                        Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.ArrowPositive],
                        Color.White
                    )
                }
            };

            var capturedItem = item;
            loadButton.TouchDown += (_, _) =>
            {
                LoadAmmo(capturedItem);
            };

            var ammoProps = item.ItemDef.AmmoProperties;

            ammoRow.Widgets.Add(loadButton);
            ammoRow.Widgets.Add(new EntityIcon(item, BaseContent.IconSizes.Default));
            ammoRow.Widgets.Add(new VerticalStackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                Widgets =
                {
                    new Label(BaseContent.Styles.Label.Normal)
                    {
                        Text = item.LabelWithStackSize,
                        TextColor = Color.White
                    },
                    new HorizontalStackPanel
                    {
                        Spacing = 10,
                        Widgets =
                        {
                            new Label(BaseContent.Styles.Label.Small)
                            {
                                Text = ammoProps?.DamageType.ToString() ?? "None",
                                TextColor = GetDamageTypeColor(ammoProps?.DamageType ?? DamageType.Invalid)
                            },
                            new Label(BaseContent.Styles.Label.Small)
                            {
                                Text = ammoProps != null ? $"{ammoProps.DamageRange.Min:F0}-{ammoProps.DamageRange.Max:F0} dmg" : "",
                                TextColor = Color.LightGray
                            }
                        }
                    }
                }
            });

            _ammoListPanel.Widgets.Add(ammoRow);
        }

        if (_ammoListPanel.Widgets.Count == 0)
        {
            _ammoListPanel.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = "No suitable ammo in inventory",
                TextColor = Color.Gray
            });
        }
    }

    private void LoadAmmo(Item ammo)
    {
        // Remove from inventory
        Core.Context.PlayerPawn.Inventory.Entities.Remove(ammo);

        // Load into slingshot, get any previously loaded ammo back
        var oldAmmo = _handler.LoadAmmo(ammo);

        // Return old ammo to inventory
        if (oldAmmo != null)
        {
            Core.Context.PlayerPawn.Inventory.TryAdd(oldAmmo);
        }

        RefreshLoadedAmmoDisplay();
        RefreshAmmoList();
    }

    private void UnloadAmmo()
    {
        var oldAmmo = _handler.LoadAmmo(null);
        if (oldAmmo != null)
        {
            Core.Context.PlayerPawn.Inventory.TryAdd(oldAmmo);
        }

        RefreshLoadedAmmoDisplay();
        RefreshAmmoList();
    }

    private static HorizontalStackPanel MakeStatRow(string label, int width, Widget valueWidget)
    {
        return new HorizontalStackPanel
        {
            Spacing = 10,
            Margin = new Thickness(0, 5, 0, 0),
            Widgets =
            {
                new Label(BaseContent.Styles.Label.Normal)
                {
                    Text = $"{label}:",
                    TextColor = Color.Gray,
                    Width = width
                },
                valueWidget
            }
        };
    }

    public override void Update()
    {
        // Refresh displays in case inventory changed externally
        RefreshLoadedAmmoDisplay();
        RefreshAmmoList();
    }

    private static Color GetDamageTypeColor(DamageType damageType)
    {
        return damageType switch
        {
            DamageType.Sharp => Color.LightSteelBlue,
            DamageType.Blunt => Color.SandyBrown,
            DamageType.Piercing => Color.Silver,
            DamageType.Flesh => Color.IndianRed,
            DamageType.Fire => Color.OrangeRed,
            DamageType.Ice => Color.LightCyan,
            DamageType.Acid => Color.LimeGreen,
            DamageType.Magic => Color.MediumPurple,
            _ => Color.Gray
        };
    }
}
