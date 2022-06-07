using Grafted.Definitions;
using Grafted.Sim.Entities.Items;
using Grafted.Utils;
using Myra.Graphics2D;
using Myra.Graphics2D.TextureAtlases;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;

namespace Grafted.Sim.Gui.Widgets.TownWidgets.HouseWidgets.FoodWidgets;

public class AlchemyPanel : VerticalStackPanel {
    private readonly TownStructureHouse _house;

    private RecipePanel? _recipePanel;
    private readonly ItemBrewingPanel _itemBrewingPanel;
    private readonly Widget _pickItemPanel;

    public AlchemyPanel(TownStructureHouse house) {
        _house = house;
        Spacing = 15;
        Padding = new Thickness(20);
        Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrame];
        base.Visible = false;
        AddChild(new Label(BaseContent.Styles.Label.Large) { Text = "Alchemy" });
        AddChild(new HorizontalSeparator());
        _pickItemPanel = GeneratePickItemPanel();
        if (_house.Barrel.ItemDef == null) {
            _pickItemPanel.Visible = true;
        }

        AddChild(_pickItemPanel);

        _itemBrewingPanel = new ItemBrewingPanel(_house.Barrel);
        _itemBrewingPanel.VisibleChanged += (sender, _) => {
            _pickItemPanel.Visible = !((ItemBrewingPanel) sender!).Visible;
        };
        AddChild(_itemBrewingPanel);
    }



    private Widget GeneratePickItemPanel() {
        VerticalStackPanel panel = new() { Spacing = 15, Visible = false };
        Panel itemToBrewPanel = new() { MinHeight = 380 };
        panel.AddChild(new Label { Text = "The brewing barrel is empty" });
        panel.AddChild(new RecipePicker(
            DefRepository<ItemDef>.Defs.FindAll(i => i == Defs.Items.BalmyOintment || i == Defs.Items.TheDreamingPowder|| i == Defs.Items.MendersMist),
            (sender, _) => {
                ListItem comboItem = ((ListBox) sender!).SelectedItem;
                itemToBrewPanel.Widgets.Clear();
                if (comboItem.Tag == null) { return; }

                _recipePanel = new RecipePanel(_house, (ItemDef) comboItem.Tag, "Start Brewing", (def, amount) => _house.TryStartBrewing(def));
                itemToBrewPanel.AddChild(_recipePanel);
            }
        ) {
            VerticalAlignment = VerticalAlignment.Bottom
        });

        panel.AddChild(new HorizontalSeparator());
        panel.AddChild(itemToBrewPanel);
        return panel;
    }

    public void Update() {
        if (Visible == false && _house.HasAlchemyBarrel) {
            Visible = true;
        }

        _recipePanel?.Update();
        _itemBrewingPanel.Update();
    }

    private class ItemBrewingPanel : VerticalStackPanel {
        private readonly AlchemyBarrel _barrel;
        private ItemDef? _cachedDef;
        private Label _timeLeft;
        private TextButton _button;

        public ItemBrewingPanel(AlchemyBarrel barrel) {
            _barrel = barrel;
            Spacing = 10;
            base.Visible = false;
            _timeLeft = new Label { HorizontalAlignment = HorizontalAlignment.Center };
        }

        public void Update() {
            if (_barrel.ItemDef == null && _barrel.Item == null) {
                Visible = false;
                return;
            }

            if (_cachedDef == null && _barrel.ItemDef != _cachedDef) {
                Visible = true;
                Widgets.Clear();
                AddChild(new Image() { Background = new TextureRegion(_barrel.ItemDef!.Icon), Width = 80, Height = 80, HorizontalAlignment = HorizontalAlignment.Center });
                AddChild(new Label(BaseContent.Styles.Label.Medium) { Text = _barrel.ItemDef!.Label, HorizontalAlignment = HorizontalAlignment.Center });
                AddChild(_timeLeft);
                _button = new TextButton(BaseContent.Styles.Button.Normal) {
                    Text = "Still Brewing",
                    Enabled = false, HorizontalAlignment = HorizontalAlignment.Center
                };
                _button.Click += (_, _) => {
                    _barrel.TransferToStorage();
                    Visible = false;
                };
                AddChild(_button);

                _cachedDef = _barrel.ItemDef;
            }

            if (_cachedDef != null) {
                if (_barrel.TimeLeft > 1) {
                    _timeLeft.Text = $"\\c[{UiTextColor.TextColorTime}]{_barrel.TimeLeft} \\c[{UiTextColor.TextColorDefault}]minutes left";
                }
                else {
                    _timeLeft.Text = $"The brew is ready!";
                    _button.Text = "Transfer to storage";
                }

            }

            if (_button.Enabled == false && _barrel.Item != null) {
                _button.Enabled = true;
            }
        }
    }
}