namespace Wendlemire.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.TrinketWidgets;

[UsedImplicitly]
public sealed class SlingshotPanel : EntityPanelBase
{
    private readonly SlingshotHandler _handler;
    private readonly VerticalStackPanel _ammoListPanel;
    private readonly HorizontalStackPanel _loadedAmmoPanel;
    private readonly Label _loadedAmmoLabel;
    private readonly ItemUpgradePanel _upgradePanel;
    private readonly Image _headerIcon;
    private readonly PawnInventory _inventory;
    private readonly CursorButton _autoFireToggle;
    private readonly Label _autoFireLabel;

    public SlingshotPanel(BaseGui gui, Item item, EntityPanelProperties? properties = null) : base(gui, item, properties)
    {
        _handler = (SlingshotHandler)item.TrinketHandler!;
        
        _inventory = Core.Context.PlayerPawn.Inventory;
        _inventory.ItemAdded += OnInventoryChanged;
        _inventory.ItemRemoved += OnInventoryChanged;
        _inventory.ItemStackSizeChanged += OnInventoryChanged;
        Padding = new Thickness(16);
        Height = 600;

        // Header with icon, title, and active bonuses
        _headerIcon = new Image
        {
            Background = new TextureRegion(EntityVisuals.LoadPremultiplied(_handler.CurrentTexturePath)),
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
        
        // Auto-fire toggle row
        _autoFireLabel = new Label(BaseContent.Styles.Label.Small)
        {
            Text = _handler.IsAutomatic ? "AUTO" : "MANUAL",
            TextColor = _handler.IsAutomatic ? Color.LimeGreen : Color.Gray,
            VerticalAlignment = VerticalAlignment.Center
        };
        
        _autoFireToggle = new CursorButton(BaseContent.Styles.Button.Normal)
        {
            Content = new HorizontalStackPanel
            {
                Spacing = 6,
                Widgets =
                {
                    new Label(BaseContent.Styles.Label.Small)
                    {
                        Text = "Full-Auto:",
                        TextColor = Color.White,
                        VerticalAlignment = VerticalAlignment.Center
                    },
                    _autoFireLabel
                }
            },
            Margin = new Thickness(0, 0, 0, 8),
            Visible = _handler.UpgradeLevel >= 3
        };
        _autoFireToggle.TouchDown += (_, _) => ToggleAutoFire();
        Widgets.Add(_autoFireToggle);

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
            Width = 340
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
            Height = 350
        };
        ammoColumn.Widgets.Add(scrollViewer);
        RefreshAmmoList();

        // RIGHT COLUMN: Upgrades using the generic ItemUpgradePanel
        _upgradePanel = new ItemUpgradePanel(item, _handler, RefreshHeaderIcon)
        {
            Width = 210
        };

        mainContent.Widgets.Add(ammoColumn);
        mainContent.Widgets.Add(new VerticalSeparator());
        mainContent.Widgets.Add(_upgradePanel);

        Widgets.Add(mainContent);
    }
    
    private void RefreshHeaderIcon()
    {
        _headerIcon.Background = new TextureRegion(EntityVisuals.LoadPremultiplied(_handler.CurrentTexturePath));
        _autoFireToggle.Visible = _handler.UpgradeLevel >= 3;
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

            var unloadButton = new CursorButton(BaseContent.Styles.Button.Normal)
            {
                Content = new Label(BaseContent.Styles.Label.Small) { Text = "Unload", TextColor = Color.IndianRed },
                VerticalAlignment = VerticalAlignment.Center
            };
            unloadButton.TouchDown += (_, _) =>
            {
                UnloadAmmo();
                _upgradePanel.Refresh();
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

            var loadButton = new CursorButton(BaseContent.Styles.Button.Normal)
            {
                VerticalAlignment = VerticalAlignment.Center,
                Content = new Label(BaseContent.Styles.Label.Small) { Text = "Load" }
            };

            var capturedItem = item;
            loadButton.TouchDown += (_, _) =>
            {
                LoadAmmo(capturedItem);
                _upgradePanel.Refresh();
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
        // Check if we should stack with existing loaded ammo of the same type
        if (_handler.Ammo != null && _handler.Ammo.Def == ammo.Def)
        {
            // Stack the ammo - add to existing loaded ammo
            var currentAmmo = _handler.Ammo;
            var stackLimit = currentAmmo.ItemDef.StackLimit;
            var spaceAvailable = stackLimit - currentAmmo.StackSize;
            
            if (spaceAvailable > 0)
            {
                var amountToTransfer = Math.Min(ammo.StackSize, spaceAvailable);
                currentAmmo.StackSize += amountToTransfer;
                ammo.StackSize -= amountToTransfer;
                
                // If the inventory ammo is depleted, remove it
                if (ammo.StackSize <= 0)
                {
                    Core.Context.PlayerPawn.Inventory.Remove(ammo);
                    ammo.Destroy();
                }
            }
            
            RefreshLoadedAmmoDisplay();
            RefreshAmmoList();
            return;
        }
        
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

    private void OnInventoryChanged(Item _)
    {
        Log.Debug($"Inventory changed: {_}");
        RefreshLoadedAmmoDisplay();
        RefreshAmmoList();
    }

    public override void Update() { }

    private void ToggleAutoFire()
    {
        _handler.IsAutomatic = !_handler.IsAutomatic;
        _autoFireLabel.Text = _handler.IsAutomatic ? "AUTO" : "MANUAL";
        _autoFireLabel.TextColor = _handler.IsAutomatic ? Color.LimeGreen : Color.Gray;
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
