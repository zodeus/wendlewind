using System;
using System.Globalization;
using Grafted.Sim.Entities;
using Myra.Graphics2D;
using Myra.Graphics2D.TextureAtlases;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;

namespace Grafted.Sim.Gui.Widgets.EntityWidgets;

public class EntityPanelProperties {
    public bool ShowTitle { get; set; } = true;
    public bool ShowCloseButton { get; set; }
    public TextureRegion? Background { get; set; } = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrame];
    public Action? CloseButtonAction;
}

public abstract class EntityPanelBase : VerticalStackPanel {
    public readonly HorizontalStackPanel Header;

    protected EntityPanelBase(Entity entity, EntityPanelProperties? properties) {
        Background = properties?.Background;
        Padding = new Thickness(15);
        Header = new HorizontalStackPanel { Spacing = 20 };
        Header.Proportions.Add(Proportion.Fill);
        AddChild(Header);
        if (properties?.ShowTitle ?? false) {
            Header.Margin = new Thickness(0, 0, 0, 10);
            Header.AddChild(new Label("large") { Text = entity.Label, VerticalAlignment = VerticalAlignment.Center });
        }

        if (properties?.ShowCloseButton ?? false) {
            Header.Margin = new Thickness(0, 0, 0, 10);
            ImageButton closeButton = new(BaseContent.Styles.Button.Small) {
                Image = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.Close],
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };
            closeButton.Click += (_, _) => {
                properties.CloseButtonAction?.Invoke();
            };
            Header.AddChild(closeButton);
        }
    }

    //public EntityPanelBase(Entity entity) { }
    public abstract void Update();
}

public class EntityPanel : EntityPanelBase {
    private readonly Entity _entity;

    public EntityPanel(Entity entity, EntityPanelProperties? properties = null) : base(entity, properties) {
        _entity = entity;
        MinWidth = 300;
        Spacing = 5;
        AddChild(new Image { Background = new TextureRegion(entity.Icon), Width = 128, Height = 128 });
        AddChild(new Label { Text = entity.Def.Description, Wrap = true, Margin = new Thickness(10), Font = BaseContent.Fonts.Default.Small });


        foreach (BaseStat baseStat in entity.Def.BaseStats) {
            var row = new HorizontalStackPanel { Spacing = 10 };
            row.AddChild(new Label { Text = $"{baseStat.Def.Label}:" });
            row.AddChild(new Label { Text = entity.GetStatValue(baseStat.Def).ToString(CultureInfo.InvariantCulture) });
            AddChild(row);

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

    public override void Update() { }
}