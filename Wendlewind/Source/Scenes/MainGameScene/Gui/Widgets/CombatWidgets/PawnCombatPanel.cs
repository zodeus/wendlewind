using Wendlewind.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets;
using Wendlewind.Scenes.MainGameScene.Gui.Widgets.PawnRenderer;

namespace Wendlewind.Scenes.MainGameScene.Gui.Widgets.CombatWidgets;

internal sealed class PawnCombatPanel : HorizontalStackPanel
{
    public readonly Pawn Pawn;
    private readonly Encounter _encounter;
    private VerticalProgressBar _bloodBar = null!;
    private PawnRenderWidget? _bodyWidget;
    private readonly ZoneGui _gui;
    private readonly List<IUpdatable> _updatables = new();
    private PotionBar? _potionBar;

    /// <summary>
    /// Gets the body render widget for this pawn, if available.
    /// </summary>
    public PawnRenderWidget? BodyWidget => _bodyWidget;

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
        var trinketBar = new TrinketBar(pawn.Inventory, TrinketType.Combat, item => _gui.ViewEntity(item))
        {
            DefaultProportion = Proportion.Auto,
            VerticalAlignment = VerticalAlignment.Bottom,
            HorizontalAlignment = HorizontalAlignment.Right,
        };
        _potionBar = new PotionBar(pawn, item => _gui.ViewEntity(item))
        {
            HorizontalAlignment = HorizontalAlignment.Right
        };
        var weaponBar = new WeaponBar(pawn, readOnly: true, inspectHandler: item => _gui.ViewEntity(item))
        {
            HorizontalAlignment = HorizontalAlignment.Right,
        };
        var stanceBar = new BodyStanceBar(pawn, readOnly: true) {HorizontalAlignment = HorizontalAlignment.Right };

        _updatables.Add(_potionBar);
        _updatables.Add(weaponBar);

        SetProportionType(_potionBar, ProportionType.Auto);
        SetProportionType(weaponBar, ProportionType.Auto);
        SetProportionType(stanceBar, ProportionType.Auto);
        SetProportionType(trinketBar, ProportionType.Auto);

        var loadout = new VerticalStackPanel
        {
            VerticalAlignment = VerticalAlignment.Bottom,
            HorizontalAlignment = HorizontalAlignment.Right,
            MinWidth = 300,
            Spacing = 6,
            Padding = new Thickness(10),
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrame],
            Widgets =
            {
                new Label(BaseContent.Styles.Label.Small)
                {
                    Text = "Loadout",
                    TextColor = Color.Goldenrod,
                    HorizontalAlignment = HorizontalAlignment.Right
                },
                _potionBar,
                weaponBar,
                stanceBar,
                trinketBar,
            }
        };
        Widgets.Add(loadout);
    }

    public void NotifyPotionUsed(string? itemMoniker)
    {
        _potionBar?.NotifyUsed(itemMoniker);
    }

    private PawnRenderWidget CreateBodyWidget(int size)
    {
        _bodyWidget = new PawnRenderWidget(Pawn)
        {
            Width = size,
            Height = size
        };

        if (Pawn.PawnType == PawnType.Player)
        {
            _bodyWidget.HorizontalAlignment = HorizontalAlignment.Right;
        }
        
        // Set weather from encounter if available
        if (_encounter.Weather != null)
        {
            _bodyWidget.SetWeather(_encounter.Weather);
        }
        return _bodyWidget;
    }

    private Widget GeneratePawnPanel()
    {
        VerticalStackPanel panel = new()
        {
            VerticalAlignment = VerticalAlignment.Bottom,
            DefaultProportion = Proportion.Auto
        };
        
        // Use body widget for all pawns - it will fall back to icon if no layout available
        var bodyWidget = CreateBodyWidget(BaseContent.IconSizes.Portrait);
        
        // Create vertical blood bar
        _bloodBar = new VerticalBloodBar(Pawn) { Width = 16, Height = BaseContent.IconSizes.Portrait };
        
        // Create effects panel for all pawns
        var pawnEffectsPanel = new PawnBodyEffectsPanel(_gui, Pawn, EffectsPanelOrientation.Vertical)
        {
            VerticalAlignment = VerticalAlignment.Bottom,
        };
        _updatables.Add(pawnEffectsPanel);
        
        // Create horizontal container for body, blood bar, and effects
        // Player: effects on left, body, blood bar on right
        // Enemy: blood bar on left, body, effects on right
        var bodyAndBloodContainer = new HorizontalStackPanel
        {
            Spacing = 5,
            VerticalAlignment = VerticalAlignment.Bottom,
        };
        
        if (Pawn.PawnType == PawnType.Player)
        {
            bodyAndBloodContainer.Widgets.Add(pawnEffectsPanel);
            bodyAndBloodContainer.Widgets.Add(bodyWidget);
            bodyAndBloodContainer.Widgets.Add(_bloodBar);
            bodyAndBloodContainer.HorizontalAlignment = HorizontalAlignment.Right;
        }
        else
        {
            bodyAndBloodContainer.Widgets.Add(_bloodBar);
            bodyAndBloodContainer.Widgets.Add(bodyWidget);
            bodyAndBloodContainer.Widgets.Add(pawnEffectsPanel);
        }
        
        panel.Widgets.Add(bodyAndBloodContainer);

        Label namePlate = new()
        {
            Text = Pawn.LabelShort,
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.IconFrame],
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Padding = new Thickness(12)
        };
        namePlate.TouchDown += (_, _) => {
            _gui.ViewEntity(Pawn);
        };
        var attackSpeed = new AttackSpeedIcon(Pawn){ VerticalAlignment = VerticalAlignment.Stretch};
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
        _potionBar?.Update(deltaTime);
        foreach (var u in _updatables)
        {
            if (u == _potionBar)
            {
                continue;
            }

            u.Update();
        }
    }
}
