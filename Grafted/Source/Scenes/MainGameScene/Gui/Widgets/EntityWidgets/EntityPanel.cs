using System.Globalization;
using Grafted.Sim.Entities;

namespace Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets;

public class EntityPanelProperties
{
    public bool ShowTitle { get; set; } = true;
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
        Padding = new Thickness(15);
        Header = new HorizontalStackPanel { Spacing = 20 };
        Header.Proportions.Add(Proportion.Fill);
        Widgets.Add(Header);
        if (properties?.ShowTitle ?? false)
        {
            Header.Margin = new Thickness(0, 0, 0, 10);
            Header.Widgets.Add(new Label("large") { Text = entity.Label, VerticalAlignment = VerticalAlignment.Center });
        }

        if (properties?.ShowCloseButton ?? false)
        {
            Header.Margin = new Thickness(0, 0, 0, 10);
            ImageButton closeButton = new(BaseContent.Styles.Button.Small)
            {
                Image = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.Close],
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };
            closeButton.Click += (_, _) => { properties.CloseButtonAction?.Invoke(); };
            Header.Widgets.Add(closeButton);
        }
    }

    //public EntityPanelBase(Entity entity) { }
    public abstract void Update();
}

public class EntityPanel : EntityPanelBase
{
    private readonly Entity _entity;

    public EntityPanel(BaseGui gui, Entity entity, EntityPanelProperties? properties = null) : base(gui, entity, properties)
    {
        _entity = entity;
        MinWidth = 300;
        Spacing = 5;
        Widgets.Add(new Image { Background = new TextureRegion(entity.Icon), Width = 128, Height = 128 });
        Widgets.Add(new Label { Text = entity.Def.Description, Wrap = true, Margin = new Thickness(10), Font = BaseContent.Fonts.Default.Small, MaxWidth = 600});


        foreach (BaseStat baseStat in entity.Def.BaseStats)
        {
            var row = new HorizontalStackPanel { Spacing = 10 };
            row.Widgets.Add(new Label { Text = $"{baseStat.Def.Label}:" });
            row.Widgets.Add(new Label { Text = entity.GetStatValue(baseStat.Def).ToString(CultureInfo.InvariantCulture) });
            Widgets.Add(row);

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