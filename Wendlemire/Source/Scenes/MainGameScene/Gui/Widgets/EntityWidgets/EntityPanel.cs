using System.Globalization;

namespace Wendlemire.Scenes.MainGameScene.Gui.Widgets.EntityWidgets;

public class EntityPanelProperties
{
    public bool ShowTitle { get; set; }
    public bool ShowCloseButton { get; set; }
    public TextureRegion? Background { get; set; } = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrame];
    public Action? CloseButtonAction;
}

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
            var closeButton = new CursorButton(BaseContent.Styles.Button.Small)
            {
                Content = new Image { Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.Close] },
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };
            StackPanel.SetProportionType(closeButton, ProportionType.Fill);
            closeButton.Click += (_, _) => { properties.CloseButtonAction?.Invoke(); };
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
        EntityCardChrome.ApplyCard(this);
        Widgets.Add(EntityCardChrome.Header(entity));

        foreach (var baseStat in entity.Def.BaseStats)
        {
            Widgets.Add(EntityCardChrome.StatRow(
                baseStat.Def.Label,
                entity.GetStatValue(baseStat.Def).ToString(CultureInfo.InvariantCulture)));

            /*row.RegisterCallback<MouseEnterEvent>(evt => {
                key.AddToClassList("text--hover");
                value.AddToClassList("text--hover");
            });
            row.RegisterCallback<MouseLeaveEvent>(evt => {
                key.RemoveFromClassList("text--hover");
                value.RemoveFromClassList("text--hover");
            });*/
        }
    }

    public override void Update()
    {
    }
}