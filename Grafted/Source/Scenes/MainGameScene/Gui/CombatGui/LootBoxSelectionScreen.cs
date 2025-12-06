using Grafted.Sim.Entities;
using Grafted.Sim.LootBoxes;

namespace Grafted.Scenes.MainGameScene.Gui.CombatGui;

/// <summary>
/// Displays all potential loot boxes for the player to choose from after combat.
/// Clicking a chest immediately opens it and shows the loot.
/// </summary>
public sealed class LootBoxSelectionScreen : VerticalStackPanel
{
    private readonly CombatResultsScreen _resultsScreen;
    private readonly GameContext _context;
    private readonly Widget _selectionPanel;
    private readonly VerticalStackPanel _lootPanel;

    public LootBoxSelectionScreen(CombatResultsScreen resultsScreen, GameContext context, IReadOnlyList<LootBoxDef> lootBoxes)
    {
        _resultsScreen = resultsScreen;
        _context = context;

        HorizontalAlignment = HorizontalAlignment.Center;
        VerticalAlignment = VerticalAlignment.Center;
        Spacing = 30;

        // Selection panel - shows all chests
        _selectionPanel = BuildSelectionPanel(lootBoxes);
        
        // Loot panel - shows items after opening (initially hidden)
        _lootPanel = new VerticalStackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Visible = false
        };

        Widgets.Add(_selectionPanel);
        Widgets.Add(_lootPanel);
    }

    private Widget BuildSelectionPanel(IReadOnlyList<LootBoxDef> lootBoxes)
    {
;

        var chestContainer = new HorizontalStackPanel
        {
            Margin = new Thickness(0, 100, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Center,
            Spacing = 80
        };

        foreach (var box in lootBoxes)
        {
            var chestPanel = CreateChestPanel(box);
            chestContainer.Widgets.Add(chestPanel);
        }

        return chestContainer;
    }

    private Widget CreateChestPanel(LootBoxDef box)
    {
        var panel = new VerticalStackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Spacing = 12
        };

        // Use Button with LargeGold style for proper hover/pressed states
        var chestButton = new Button(BaseContent.Styles.Button.Dark)
        {
            Padding = new Thickness(16),
            Content = new Image
            {
                Background = new TextureRegion(box.Icon),
                Width = 256,
                Height = 256
            }
        };
        chestButton.Click += (_, _) => OpenChest(box);

        var label = new Label(BaseContent.Styles.Label.Medium)
        {
            Text = box.Label,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextColor = BaseContent.Colors.Text.Golden,
            MaxWidth = 256,
            Wrap = true

        };

        panel.Widgets.Add(chestButton);
        panel.Widgets.Add(label);

        return panel;
    }

    private void OpenChest(LootBoxDef box)
    {
        var playerPawn = _context.PlayerPawn;
        List<Item> items = [];
        
        var limit = Mathf.Clamp(box.CollectionLimit.RandomValue, 1, 10);
        if (limit == 1)
        {
            var boxItem = box.Items
                .Where(potentialItem => _context.Player.HasTrinkets(potentialItem.ItemDef) == false)
                .RandomElementByWeight(i => i.Weight);
            if (boxItem != null)
            {
                items.Add(EntityGenerator.CreateEntity<Item>(boxItem.ItemDef, boxItem.Amount.RandomValue));
                if (boxItem.ItemDef.ItemType == ItemType.Trinket)
                {
                    _context.Player.TrinketsFound.Add(boxItem.ItemDef);
                }
            }
        }
        else
        {
            foreach (var boxItem in box.Items.InRandomOrder())
            {
                if (boxItem.ItemDef.EquipmentProperties?.EquipmentType == EquipmentType.Armor)
                {
                    var existingArmorPieces = playerPawn.Equipment.Armor.Count(a => a.ItemDef == boxItem.ItemDef);
                    existingArmorPieces += playerPawn.Inventory.Count(a => a.ItemDef == boxItem.ItemDef);
                    existingArmorPieces += items.Count(a => a.ItemDef == boxItem.ItemDef);
                    if (existingArmorPieces >= playerPawn.Equipment.SlotCountFor(boxItem.ItemDef))
                    {
                        continue;
                    }
                }

                if (Core.Random.Chance(boxItem.ChanceToDrop))
                {
                    items.Add(EntityGenerator.CreateEntity<Item>(boxItem.ItemDef, boxItem.Amount.RandomValue));
                }

                if (items.Count >= limit)
                {
                    break;
                }
            }
        }

        // Hide selection, show loot
        _selectionPanel.Visible = false;
        _lootPanel.Visible = true;
        _lootPanel.Widgets.Clear();

        // Chest name
        _lootPanel.Widgets.Add(new Label(BaseContent.Styles.Label.Large)
        {
            Text = box.Label,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 20)
        });

        // Items row
        var itemRow = new HorizontalStackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Spacing = 20
        };

        if (items.Count == 0)
        {
            itemRow.Widgets.Add(new Label(BaseContent.Styles.Label.Medium) { Text = "Womp, womp, the loot box is empty." });
        }

        foreach (var item in items)
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
                        Background = new TextureRegion(item.ItemDef.Icon),
                        Width = 128, Height = 128, HorizontalAlignment = HorizontalAlignment.Center
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

        _lootPanel.Widgets.Add(itemRow);

        // Continue button
        var continueButton = new Button(BaseContent.Styles.Button.Large)
        {
            Content = new Label { Text = "Continue" },
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 20, 0, 0)
        };
        continueButton.Click += (_, _) =>
        {
            RemoveFromParent();
            foreach (var item in items)
            {
                playerPawn.Inventory.TryAdd(item);
            }
            _resultsScreen.ShowResultsScreen();
        };

        _lootPanel.Widgets.Add(continueButton);
    }
}
