using System.Globalization;

namespace Wendlemire.Scenes.MainGameScene.Gui.Widgets.EntityWidgets;

public class EntityPanelProperties
{
    public bool ShowTitle { get; set; }
    public bool ShowCloseButton { get; set; }
    public TextureRegion? Background { get; set; } = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrame];
    public Action? CloseButtonAction;
}

[UsedImplicitly(ImplicitUseTargetFlags.WithInheritors)]
public abstract class EntityPanelBase : VerticalStackPanel
{
    protected readonly BaseGui Gui;
    public readonly HorizontalStackPanel Header;

    protected EntityPanelBase(BaseGui gui, Entity entity, EntityPanelProperties? properties)
    {
        Gui = gui;
        Background = properties?.Background;
        Padding = EntityCardChrome.CardPadding;
        Spacing = EntityCardChrome.CardSpacing;
        Header = new HorizontalStackPanel { Spacing = 8 };

        var showTitle = properties?.ShowTitle ?? false;
        var showClose = properties?.ShowCloseButton ?? false;
        if (showTitle || showClose)
        {
            Header.Margin = new Thickness(0, 0, 0, 4);
            Widgets.Add(Header);
        }

        if (showTitle)
        {
            Header.Widgets.Add(new Label("small") { Text = entity.Label, VerticalAlignment = VerticalAlignment.Center });
        }

        if (showClose)
        {
            var spacer = new Panel();
            StackPanel.SetProportionType(spacer, ProportionType.Fill);
            Header.Widgets.Add(spacer);

            var closeButton = new CursorButton(BaseContent.Styles.Button.Small)
            {
                Width = 22,
                Height = 22,
                Padding = new Thickness(3),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Content = new Image
                {
                    Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.Close],
                    Width = 12,
                    Height = 12,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };
            var closeAction = properties?.CloseButtonAction;
            closeButton.Click += (_, _) => { closeAction?.Invoke(); };
            Header.Widgets.Add(closeButton);
        }
    }

    //public EntityPanelBase(Entity entity) { }
    public abstract void Update();
}

public class EntityPanel : EntityPanelBase
{
    public EntityPanel(BaseGui gui, Entity entity, EntityPanelProperties? properties = null) : base(gui, entity, properties)
    {
        EntityCardChrome.BeginInspect(this, entity);

        if (entity.Def.BaseStats.Count > 0)
        {
            Widgets.Add(EntityCardChrome.StatStrip(entity.Def.BaseStats
                .Select(stat => (
                    stat.Def.Label,
                    entity.GetStatValue(stat.Def).ToString(CultureInfo.InvariantCulture),
                    Color.LightGoldenrodYellow))
                .ToArray()));
        }
    }

    public override void Update()
    {
    }
}