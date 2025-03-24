using Grafted.Graphics.Textures;
using Grafted.Scenes.MainGameScene.Gui.CombatGui;
using Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets;
using Grafted.Sim.Entities.Items.Trinkets;
using Image = Myra.Graphics2D.UI.Image;

namespace Grafted.Scenes.MainGameScene.Gui.Widgets.CombatWidgets;

internal sealed class PawnCombatPanel : HorizontalStackPanel
{
    public readonly Pawn Pawn;
    private readonly Encounter _encounter;
    private HorizontalProgressBar _bloodBar = null!;
    private readonly Dictionary<string, Image> _bodyPartImages = new();
    private readonly ZoneGui _gui;
    private readonly List<IUpdatable> _updatables = new();

    public PawnCombatPanel(ZoneGui gui, Pawn pawn, Encounter encounter)
    {
        MinWidth = 1000;
        MinHeight = 410;
        //Border = new SolidBrush(Color.Red);
        //BorderThickness = new Thickness(1);
        Pawn = pawn;
        _encounter = encounter;
        _gui = gui;

        var isPlayer = pawn.PawnType == PawnType.Player;
        if (isPlayer)
        {
            GeneratePlayerControls(pawn);
        }

        Widgets.Add(GeneratePawnPanel());

        if (isPlayer == false)
        {
            var bodySummary = new PawnBodySummary(_gui, Pawn.Body)
            {
                VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(0, 10, 0, 0)
            };
            _updatables.Add(bodySummary);
            Widgets.Add(bodySummary);
        }

        Update();
    }

    private void GeneratePlayerControls(Pawn pawn)
    {
        var trinketBar = new TrinketBar(pawn.Inventory.Entities, TrinketType.Combat, HandleTrinketClick, true)
        {
            Height = BaseContent.IconSizes.Large + 30,
            DefaultProportion = Proportion.Auto,
            VerticalAlignment = VerticalAlignment.Bottom, HorizontalAlignment = HorizontalAlignment.Right
        };
        var pawnEffectsPanel = new PawnBodyEffectsPanel(_gui, Pawn)
        {
            DefaultProportion = Proportion.Fill,
            HorizontalAlignment = HorizontalAlignment.Right,
            Padding = new Thickness(15),
            MinHeight = 90
        };
        var potionBar = new PotionBar(pawn, item => _encounter.CombatHandler?.QueueItemForPawn(item, Pawn))
        {
            HorizontalAlignment = HorizontalAlignment.Right
        };
        var weaponBar = new WeaponBar(pawn) { HorizontalAlignment = HorizontalAlignment.Right };
        var stanceBar = new BodyStanceBar(pawn) { HorizontalAlignment = HorizontalAlignment.Right };

        _updatables.Add(potionBar);
        _updatables.Add(weaponBar);
        _updatables.Add(trinketBar);
        _updatables.Add(pawnEffectsPanel);

        SetProportionType(pawnEffectsPanel, ProportionType.Fill);
        SetProportionType(potionBar, ProportionType.Auto);
        SetProportionType(weaponBar, ProportionType.Auto);
        SetProportionType(stanceBar, ProportionType.Auto);
        SetProportionType(trinketBar, ProportionType.Auto);
        Widgets.Add(new VerticalStackPanel
        {
            Width = 600,
            //ShowGridLines = true,
            //GridLinesColor = new Color(0.7f, 0.7f, 0.7f),
            Widgets =
            {
                pawnEffectsPanel,
                potionBar,
                weaponBar,
                stanceBar,
                trinketBar,
            }
        });
    }

    private void HandleTrinketClick(Item item)
    {
        if (item.TrinketHandler?.IsActive == true)
        {
            _encounter.CombatHandler?.DeActivateTrinketForPawn(item, Pawn);
        }
        else
        {
            _encounter.CombatHandler?.ActivateTrinketForPawn(item, Pawn);
        }
    }

    private Widget GenerateEquipmentPanel()
    {
        // _pawnEquipmentPanel = new PawnEquipmentPanel(_gui, Pawn, (part, type) =>
        // {
        //     if (part.Equipment[type] is { } item)
        //     {
        //         if (Pawn.PawnType == PawnType.Player && Input.RightMouseButtonReleased && item.ItemDef.ItemType == ItemType.Potion)
        //         {

        //             return;
        //         }
        //
        //         _gui.ViewEntity(item);
        //     }
        // });
        return new Widget();
    }

    private Panel InitializeBodyPartImages(int panelWidth)
    {
        Panel panel = new();

        for (var i = 0; i < Pawn.Body.AllExternalParts.Count; i++)
        {
            if (Pawn.Body.AllExternalParts[i].Image is null) continue;

            var bodyPart = Pawn.Body.AllExternalParts[i];
            var icon = bodyPart.Image!;
            Image image = new() { Background = new TextureRegion(icon), Width = panelWidth, Height = panelWidth, BorderThickness = new Thickness(2) };
            image.TouchDown += (_, _) =>
            {
                if (_gui.MouseAttachment == null)
                {
                    _gui.ViewEntity(Pawn);
                }
            };
            _bodyPartImages.Add(bodyPart.Label, image);
            panel.Widgets.Add(image);
        }

        return panel;
    }

    private void RenderBodyParts()
    {
        foreach (var bodyPartImage in _bodyPartImages)
        {
            bodyPartImage.Value.Visible = false;
        }

        foreach (var bodyPart in Pawn.Body.AllExternalParts)
        {
            if (_bodyPartImages.TryGetValue(bodyPart.Label, out var image))
            {
                image.Visible = true;
            }
        }
    }

    private Widget GeneratePawnPanel()
    {
        VerticalStackPanel panel = new()
        {
            VerticalAlignment = VerticalAlignment.Bottom,
            DefaultProportion = Proportion.Auto,
            ShowGridLines = false,
            GridLinesColor = new Color(255, 0, 0, 255),
        };
        var panelWidth = 400;
        if (Pawn.PawnType == PawnType.Enemy || Pawn.Race != Defs.Races.Journeyman)
        {
            var icon = Pawn.Icon.Flip(false, true);
            Image image = new() { Background = new TextureRegion(icon), Width = BaseContent.IconSizes.Portrait, Height = BaseContent.IconSizes.Portrait, BorderThickness = new Thickness(2) };
            Pawn.Died += _ => { image.Background = new ColoredRegion(new TextureRegion(icon), Color.Red); };
            image.TouchDown += (_, _) =>
            {
                if (_gui.MouseAttachment == null)
                {
                    _gui.ViewEntity(Pawn);
                }
            };
            panel.Widgets.Add(image);
        }
        else
        {
            panel.Widgets.Add(InitializeBodyPartImages(BaseContent.IconSizes.Portrait));
        }

        _bloodBar = new BloodBar(Pawn) { Width = panelWidth, Height = 30 };
        panel.Widgets.Add(_bloodBar);

        Label namePlate = new()
        {
            Text = Pawn.LabelShort,
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrame],
            Padding = new Thickness(12),
            Width = panelWidth - 24 - 32 - 5
        };
        var attackSpeed = new AttackSpeedIcon(Pawn);
        _updatables.Add(attackSpeed);
        panel.Widgets.Add(new HorizontalStackPanel { Spacing = 5, Widgets = { namePlate, attackSpeed } });

        return panel;
    }

    public void Update()
    {
        _bloodBar.Value = Pawn.Body.BloodPercent * 100;
        foreach (var u in _updatables)
        {
            u.Update();
        }

        RenderBodyParts();
    }
}

internal sealed class BodyStanceBar : HorizontalStackPanel
{
    public BodyStanceBar(Pawn pawn)
    {
        var buttons = new List<Button>();
        foreach (var stance in DefRepository<BodyStanceDef>.Defs)
        {
            var button = new Button
            {
                Content = new Image
                {
                    Background = new ColoredRegion(new TextureRegion(stance.Texture), Color.White),
                    Width = BaseContent.IconSizes.Medium, Height = BaseContent.IconSizes.Medium
                }
            };
            button.TouchDown += (_, _) =>
            {
                buttons.ForEach(b => ((ColoredRegion)b.Content.Background).Color = Color.White);
                ((ColoredRegion)button.Content.Background).Color = Color.Goldenrod;
                pawn.Body.Stance = stance;
            };
            buttons.Add(button);

            if (pawn.Body.Stance == stance)
            {
                ((ColoredRegion)button.Content.Background).Color = Color.Goldenrod;
            }

            Widgets.Add(button);
        }
    }
}