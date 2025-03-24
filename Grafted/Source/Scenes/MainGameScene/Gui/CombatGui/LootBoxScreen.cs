using Grafted.Sim.Entities;
using Grafted.Sim.LootBoxes;

namespace Grafted.Scenes.MainGameScene.Gui.CombatGui;

public sealed class LootBoxScreen : VerticalStackPanel
{
    private readonly VerticalStackPanel _openPanel;
    private readonly VerticalStackPanel _viewPanel;

    public LootBoxScreen(CombatResultsScreen resultsScreen, GameContext context, LootBoxDef box)
    {
        HorizontalAlignment = HorizontalAlignment.Center;
        Margin = new Thickness(0, 300, 0, 0);
        var openButton = new Button(BaseContent.Styles.Button.Large)
        {
            Margin = new Thickness(0, 10, 0, 0),
            Content = new Label(BaseContent.Styles.Label.Large) { Text = "Open" },
            HorizontalAlignment = HorizontalAlignment.Center
        };
        openButton.Click += (_, _) => { OpenBox(context, resultsScreen, box); };

        _openPanel = new VerticalStackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Widgets =
            {
                new Label(BaseContent.Styles.Label.Large) { Text = box.Label, HorizontalAlignment = HorizontalAlignment.Center },
                new Image { Background = new TextureRegion(box.Icon), Width = 256, Height = 256, HorizontalAlignment = HorizontalAlignment.Center },
                openButton
            }
        };

        _viewPanel = new VerticalStackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
        };

        Widgets.Add(_openPanel);
        Widgets.Add(_viewPanel);
    }

    private void OpenBox(GameContext context, CombatResultsScreen resultsScreen, LootBoxDef box)
    {
        List<Item> items = [];
        var playerPawn = context.PlayerPawn;
        var limit = Mathf.Clamp(box.CollectionLimit.RandomValue, 1, 10);
        if (limit == 1)
        {
            var boxItem = box.Items
                .Where(potentialItem => context.Player.HasTrinkets(potentialItem.ItemDef) == false)
                .RandomElementByWeight(i => i.Weight);
            if (boxItem != null)
            {
                items.Add(EntityGenerator.CreateEntity<Item>(boxItem.ItemDef, boxItem.Amount.RandomValue));
                if (boxItem.ItemDef.ItemType == ItemType.Trinket)
                {
                    context.Player.TrinketsFound.Add(boxItem.ItemDef);
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

        _openPanel.Visible = false;
        _viewPanel.Visible = true;

        var itemRow = new HorizontalStackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Spacing = 20
        };
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

            resultsScreen.ShowResultsScreen();
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

        _viewPanel.Widgets.Add(itemRow);
        _viewPanel.Widgets.Add(continueButton);
    }
}