using Grafted.Graphics.Textures;
using Grafted.Scenes.MainGameScene.Gui.CombatGui;
using Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets;
using Grafted.Sim.Entities.Items.Trinkets;
using Myra.Graphics2D.Brushes;
using Image = Myra.Graphics2D.UI.Image;

namespace Grafted.Scenes.MainGameScene.Gui.Widgets.CombatWidgets;

internal sealed class PawnCombatPanel : HorizontalStackPanel
{
    public readonly Pawn Pawn;
    private readonly Encounter _encounter;
    private HorizontalProgressBar _bloodBar = null!;
    private PawnBodyRenderWidget? _bodyWidget;
    private readonly ZoneGui _gui;
    private readonly List<IUpdatable> _updatables = new();

    /// <summary>
    /// Gets the body render widget for this pawn, if available.
    /// </summary>
    public PawnBodyRenderWidget? BodyWidget => _bodyWidget;

    public PawnCombatPanel(ZoneGui gui, Pawn pawn, Encounter encounter)
    {
        Pawn = pawn;
        _encounter = encounter;
        _gui = gui;

        var isPlayer = pawn.PawnType == PawnType.Player;
        if (isPlayer)
        {
            GeneratePlayerControls(pawn);
        }

        Widgets.Add(GeneratePawnPanel());


        Update(0f);
    }

    private void GeneratePlayerControls(Pawn pawn)
    {
        var trinketBar = new TrinketBar(pawn.Inventory.Entities, TrinketType.Combat, HandleTrinketClick, true)
        {
            DefaultProportion = Proportion.Auto,
            VerticalAlignment = VerticalAlignment.Bottom, 
            HorizontalAlignment = HorizontalAlignment.Right,
            Height = BaseContent.IconSizes.Large + 25,
        };
        var pawnEffectsPanel = new PawnBodyEffectsPanel(_gui, Pawn)
        {
            DefaultProportion = Proportion.Fill,
            HorizontalAlignment = HorizontalAlignment.Right,
        };
        var potionBar = new PotionBar(pawn, item => _encounter.CombatHandler?.QueueItemForPawn(item, Pawn))
        {
            HorizontalAlignment = HorizontalAlignment.Right
        };
        var weaponBar = new WeaponBar(pawn) { HorizontalAlignment = HorizontalAlignment.Right };
        var stanceBar = new BodyStanceBar(pawn) {HorizontalAlignment = HorizontalAlignment.Right };

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

    private PawnBodyRenderWidget CreateBodyWidget(int size)
    {
        _bodyWidget = new PawnBodyRenderWidget(Pawn)
        {
            Width = size,
            Height = size,
            BorderThickness = new Thickness(2)
        };
        if (Pawn.PawnType == PawnType.Player)
        {
            _bodyWidget.HorizontalAlignment = HorizontalAlignment.Right;
        }
        
        // _bodyWidget.Clicked += (_, _) =>
        // {
        //     if (_gui.MouseAttachment == null)
        //     {
        //         _gui.ViewEntity(Pawn);
        //     }
        // };

        return _bodyWidget;
    }

    private Widget GeneratePawnPanel()
    {
        VerticalStackPanel panel = new()
        {
            VerticalAlignment = VerticalAlignment.Bottom,
            DefaultProportion = Proportion.Auto,
        };
        var panelWidth = 400;
        // Use body widget for all pawns - it will fall back to icon if no layout available
        var bodyWidget = CreateBodyWidget(BaseContent.IconSizes.Portrait);
        
        panel.Widgets.Add(bodyWidget);

        _bloodBar = new BloodBar(Pawn) { Width = panelWidth, Height = 25 };
        panel.Widgets.Add(_bloodBar);

        Label namePlate = new()
        {
            Text = Pawn.LabelShort,
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.IconFrame],
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Padding = new Thickness(12)
        };
        var attackSpeed = new AttackSpeedIcon(Pawn);
        _updatables.Add(attackSpeed);
        SetProportionType(namePlate, ProportionType.Fill);
        SetProportionType(attackSpeed, ProportionType.Auto);
        panel.Widgets.Add(new HorizontalStackPanel { Widgets = { namePlate, attackSpeed } });

        return panel;
    }

    public void Update(float deltaTime)
    {
        _bloodBar.Value = Pawn.Body.BloodPercent * 100;
        _bodyWidget?.Update(deltaTime);
        foreach (var u in _updatables)
        {
            u.Update();
        }
    }
}

internal sealed class BodyStanceBar : HorizontalStackPanel
{
    public BodyStanceBar(Pawn pawn)
    {
        var buttons = new List<Button>();
        var defaultColor = new Color(80, 80, 80, 100);
        foreach (var stance in DefRepository<BodyStanceDef>.Defs)
        {
            var button =new Button(BaseContent.Styles.Button.Icon)
            {
                Content = new Image
                {
                    Background = new ColoredRegion(new TextureRegion(stance.Texture), defaultColor),
                    Width = BaseContent.IconSizes.Medium, Height = BaseContent.IconSizes.Medium
                }
            };
            button.TouchDown += (_, _) =>
            {
                buttons.ForEach(b => ((ColoredRegion)b.Content.Background).Color = defaultColor);
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