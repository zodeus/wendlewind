namespace Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.TrinketWidgets;

[UsedImplicitly]
public sealed class SlingshotPanel : EntityPanelBase
{
    private readonly SlingshotHandler _handler;
    private readonly VerticalStackPanel _ammoListPanel;
    private readonly HorizontalStackPanel _loadedAmmoPanel;
    private readonly Label _loadedAmmoLabel;
    private readonly VerticalStackPanel _upgradeSection;
    private readonly Image _headerIcon;

    public SlingshotPanel(BaseGui gui, Item item, EntityPanelProperties? properties = null) : base(gui, item, properties)
    {
        _handler = (SlingshotHandler)item.TrinketHandler!;
        Padding = new Thickness(16);
        Height = 600;

        // Header with icon, title, and active bonuses
        _headerIcon = new Image
        {
            Background = new TextureRegion(_handler.CurrentTexture),
            Width = 48, Height = 48
        };
        
        var headerInfo = new VerticalStackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            Spacing = 2,
            Widgets =
            {
                new Label(BaseContent.Styles.Label.Normal)
                {
                    Text = item.Def.Description,
                    TextColor = Color.LightGray,
                    Wrap = true,
                    MaxWidth = 380
                }
            }
        };
        
        var header = new HorizontalStackPanel
        {
            Spacing = 12,
            Margin = new Thickness(0, 0, 0, 8),
            Widgets = { _headerIcon, headerInfo }
        };
        Widgets.Add(header);

        // Two-column layout for Ammo (left) and Upgrades (right)
        var mainContent = new HorizontalStackPanel
        {
            Spacing = 16,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        // LEFT COLUMN: Ammo management
        var ammoColumn = new VerticalStackPanel
        {
            Spacing = 6,
            Width = 300
        };

        // Loaded ammo section
        ammoColumn.Widgets.Add(new Label(BaseContent.Styles.Label.Normal)
        {
            Text = "Loaded Ammo",
            TextColor = Color.DarkGoldenrod,
        });

        _loadedAmmoLabel = new Label(BaseContent.Styles.Label.Small)
        {
            Text = "None",
            TextColor = Color.Gray,
            VerticalAlignment = VerticalAlignment.Center
        };

        _loadedAmmoPanel = new HorizontalStackPanel
        {
            Spacing = 8,
            MinHeight = 40
        };
        RefreshLoadedAmmoDisplay();
        ammoColumn.Widgets.Add(_loadedAmmoPanel);

        // Available ammo section
        ammoColumn.Widgets.Add(new HorizontalSeparator { Margin = new Thickness(0, 4, 0, 4) });
        ammoColumn.Widgets.Add(new Label(BaseContent.Styles.Label.Normal)
        {
            Text = "Available Ammo",
            TextColor = Color.DarkGoldenrod,
        });

        _ammoListPanel = new VerticalStackPanel { Spacing = 4 };
        var scrollViewer = new ScrollViewer
        {
            Content = _ammoListPanel,
            MaxHeight = 150
        };
        ammoColumn.Widgets.Add(scrollViewer);
        RefreshAmmoList();

        // RIGHT COLUMN: Upgrades
        _upgradeSection = new VerticalStackPanel { Spacing = 6, Width = 210 };

        mainContent.Widgets.Add(ammoColumn);
        mainContent.Widgets.Add(new VerticalSeparator());
        mainContent.Widgets.Add(_upgradeSection);

        Widgets.Add(mainContent);
        RefreshUpgradeSection();
    }

    private void RefreshLoadedAmmoDisplay()
    {
        _loadedAmmoPanel.Widgets.Clear();

        if (_handler.Ammo != null)
        {
            var ammoProps = _handler.Ammo.ItemDef.AmmoProperties;

            _loadedAmmoPanel.Widgets.Add(new EntityIcon(_handler.Ammo, BaseContent.IconSizes.Small)
            {
                VerticalAlignment = VerticalAlignment.Center
            });
            
            _loadedAmmoPanel.Widgets.Add(new VerticalStackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                Spacing = 0,
                Widgets =
                {
                    new Label(BaseContent.Styles.Label.Small)
                    {
                        Text = _handler.Ammo.LabelWithStackSize,
                        TextColor = Color.White
                    },
                    new Label(BaseContent.Styles.Label.Small)
                    {
                        Text = ammoProps != null ? $"{ammoProps.DamageType} {ammoProps.DamageRange.Min:F0}-{ammoProps.DamageRange.Max:F0}" : "",
                        TextColor = GetDamageTypeColor(ammoProps?.DamageType ?? DamageType.Invalid)
                    }
                }
            });

            var unloadButton = new Button(BaseContent.Styles.Button.Normal)
            {
                Content = new Label(BaseContent.Styles.Label.Small) { Text = "Unload", TextColor = Color.IndianRed },
                VerticalAlignment = VerticalAlignment.Center
            };
            unloadButton.TouchDown += (_, _) => UnloadAmmo();
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
                Content = new Label(BaseContent.Styles.Label.Small) { Text = "Load" }
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

    private void RefreshUpgradeSection()
    {
        _upgradeSection.Widgets.Clear();
        
        // Update header icon to reflect current upgrade level
        _headerIcon.Background = new TextureRegion(_handler.CurrentTexture);
        
        // Section header
        _upgradeSection.Widgets.Add(new Label(BaseContent.Styles.Label.Normal)
        {
            Text = "Upgrades",
            TextColor = Color.DarkGoldenrod,
        });
        
        // Show current bonuses if upgraded
        if (_handler.UpgradeLevel != SlingshotUpgradeLevel.None)
        {
            var bonusRow = new HorizontalStackPanel { Spacing = 8 };
            
            if (_handler.UpgradeLevel >= SlingshotUpgradeLevel.Bone)
            {
                var damagePercent = (int)((SlingshotHandler.BoneDamageMultiplier - 1f) * 100);
                bonusRow.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
                {
                    Text = $"+{damagePercent}% Dmg",
                    TextColor = Color.LightGreen
                });
            }

            if (_handler.UpgradeLevel >= SlingshotUpgradeLevel.Gold)
            {
                var cooldownPercent = (int)((1f - SlingshotHandler.GoldCooldownMultiplier) * 100);
                bonusRow.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
                {
                    Text = $"-{cooldownPercent}% CD",
                    TextColor = Color.LightGreen
                });
            }
            
            _upgradeSection.Widgets.Add(bonusRow);
        }
        
        var nextUpgrade = _handler.NextUpgrade;
        if (nextUpgrade == null)
        {
            _upgradeSection.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = "Fully upgraded!",
                TextColor = Color.Gold
            });
            return;
        }

        var inventory = Core.Context.PlayerPawn.Inventory;
        var upgradeCost = _handler.GetUpgradeCost(nextUpgrade.Value);
        var canUpgrade = _handler.CanUpgrade(inventory);

        _upgradeSection.Widgets.Add(new HorizontalSeparator { Margin = new Thickness(0, 4, 0, 4) });

        // Next upgrade header with bonus
        var bonusText = nextUpgrade.Value switch
        {
            SlingshotUpgradeLevel.Bone => $"+{(int)((SlingshotHandler.BoneDamageMultiplier - 1f) * 100)}% Dmg",
            SlingshotUpgradeLevel.Gold => $"-{(int)((1f - SlingshotHandler.GoldCooldownMultiplier) * 100)}% CD",
            _ => ""
        };
        
        _upgradeSection.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
        {
            Text = $"{nextUpgrade.Value}: {bonusText}",
            TextColor = Color.Gold,
        });

        // Required trinkets (compact inline)
        foreach (var trinketDef in SlingshotHandler.RequiredTrinkets)
        {
            var hasTrinket = inventory.Trinkets.Any(t => t.Def == trinketDef);
            var trinketRow = new HorizontalStackPanel { Spacing = 6 };

            trinketRow.Widgets.Add(new Image
            {
                Background = new TextureRegion(trinketDef.Texture),
                Width = 20, Height = 20,
                Opacity = hasTrinket ? 1.0f : 0.4f,
                VerticalAlignment = VerticalAlignment.Center
            });

            trinketRow.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = trinketDef.Label,
                TextColor = hasTrinket ? Color.LightGreen : Color.IndianRed,
                VerticalAlignment = VerticalAlignment.Center
            });

            _upgradeSection.Widgets.Add(trinketRow);
        }

        // Resource costs (compact)
        foreach (var cost in upgradeCost)
        {
            var hasEnough = inventory.AmountOf(cost.Item) >= cost.Count;
            var currentAmount = inventory.AmountOf(cost.Item);

            var costRow = new HorizontalStackPanel { Spacing = 6 };

            costRow.Widgets.Add(new Image
            {
                Background = new TextureRegion(cost.Item.Texture),
                Width = 20, Height = 20,
                Opacity = hasEnough ? 1.0f : 0.5f,
                VerticalAlignment = VerticalAlignment.Center
            });

            costRow.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = $"{cost.Item.Label} {currentAmount}/{cost.Count}",
                TextColor = hasEnough ? Color.LightGreen : Color.IndianRed,
                VerticalAlignment = VerticalAlignment.Center
            });

            _upgradeSection.Widgets.Add(costRow);
        }

        // Upgrade button
        var upgradeButton = new Button(BaseContent.Styles.Button.Normal)
        {
            Content = new Label
            {
                Text = canUpgrade ? "Upgrade" : "Missing",
                TextColor = canUpgrade ? Color.Gold : Color.Gray
            },
            Enabled = canUpgrade,
            Margin = new Thickness(0, 6, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        upgradeButton.TouchDown += (_, _) =>
        {
            if (_handler.TryUpgrade(inventory))
            {
                RefreshUpgradeSection();
            }
        };

        _upgradeSection.Widgets.Add(upgradeButton);
    }

    private void LoadAmmo(Item ammo)
    {
        // Remove from inventory
        Core.Context.PlayerPawn.Inventory.Remove(ammo);

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
        RefreshUpgradeSection();
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
