using Wendlemire.NetCode;
using Wendlemire.NetCode.Contracts;

namespace Wendlemire.Scenes.MainGameScene.Gui.CombatGui;

public sealed class TestSimBuildCard : CursorButton
{
    public const int CatalogWidth = 232;
    public const int SlotWidth = 260;
    private const int Icon = 28;
    private const int IconsPerRow = 5;

    private readonly BuildSnapshot _snapshot;
    private readonly bool _slot;
    private readonly Label _badge;

    public string BuildId => _snapshot.BuildId;

    public TestSimBuildCard(BuildSnapshot snapshot, bool slot, Action<BuildSnapshot, bool> onPick)
        : base(BaseContent.Styles.Button.Dark)
    {
        _snapshot = snapshot;
        _slot = slot;
        Width = slot ? SlotWidth : CatalogWidth;
        Padding = new Thickness(8, 6);

        var stage = BuildCatalog.StageOf(snapshot);
        var stageColor = StageColor(stage);

        var header = new HorizontalStackPanel
        {
            Spacing = 6,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        header.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
        {
            Text = stage.Label().ToUpperInvariant(),
            TextColor = stageColor,
            VerticalAlignment = VerticalAlignment.Center
        });
        _badge = new Label(BaseContent.Styles.Label.Small)
        {
            Text = "",
            TextColor = Color.Goldenrod,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center
        };
        header.Widgets.Add(_badge);

        var body = new VerticalStackPanel
        {
            Spacing = 3,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        body.Widgets.Add(header);
        body.Widgets.Add(new Label(BaseContent.Styles.Label.Normal)
        {
            Text = BuildCatalog.DisplayName(snapshot),
            TextColor = BaseContent.Colors.Text.Golden,
            HorizontalAlignment = HorizontalAlignment.Center,
            Wrap = true,
            MaxWidth = (slot ? SlotWidth : CatalogWidth) - 20
        });
        body.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
        {
            Text = $"{snapshot.StanceMoniker ?? "Offensive"} · {BuildCatalog.ArmorSummary(snapshot)}",
            TextColor = new Color(170, 170, 170),
            HorizontalAlignment = HorizontalAlignment.Center
        });

        foreach (var group in BuildCatalog.LoadoutGroups(snapshot))
        {
            body.Widgets.Add(CreateGroupRow(group));
        }

        Content = body;
        TouchDown += (_, _) => onPick(snapshot, Mouse.GetState().RightButton == ButtonState.Pressed);
        Refresh(TestSimSettings.AttackerBuildId, TestSimSettings.DefenderBuildId, pickingAttacker: true);
    }

    public void Refresh(string attackerId, string defenderId, bool pickingAttacker)
    {
        var isAttacker = BuildId == attackerId;
        var isDefender = BuildId == defenderId;
        _badge.Text = isAttacker && isDefender ? "ATK / DEF"
            : isAttacker ? "ATK"
            : isDefender ? "DEF"
            : "";

        var pickingThis = _slot && ((pickingAttacker && isAttacker) || (!pickingAttacker && isDefender));
        var selected = isAttacker || isDefender || pickingThis;
        Background = Stylesheet.Current.Atlas[selected
            ? BaseContent.Styles.Atlas.Panel.MediumFrameBright
            : BaseContent.Styles.Atlas.Panel.MediumFrame];
        OverBackground = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrameBright];
    }

    public static Color StageColor(BuildStage stage) => stage switch
    {
        BuildStage.Early => new Color(140, 200, 130),
        BuildStage.Mid => new Color(130, 170, 220),
        BuildStage.Late => new Color(220, 150, 90),
        BuildStage.End => new Color(190, 140, 230),
        _ => BaseContent.Colors.Text.Golden
    };

    private static Widget CreateGroupRow(BuildCatalog.LoadoutGroup group)
    {
        var icons = new VerticalStackPanel { Spacing = 2 };
        HorizontalStackPanel? row = null;
        for (var i = 0; i < group.Items.Count; i++)
        {
            if (i % IconsPerRow == 0)
            {
                row = new HorizontalStackPanel { Spacing = 2 };
                icons.Widgets.Add(row);
            }

            var def = group.Items[i];
            row!.Widgets.Add(new Panel
            {
                Width = Icon + 4,
                Height = Icon + 4,
                Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.IconFrame],
                Widgets =
                {
                    new Image
                    {
                        Background = def.GetIconImage(),
                        Width = Icon,
                        Height = Icon,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                }
            }.WithTooltip(def.Label));
        }

        return new HorizontalStackPanel
        {
            Spacing = 4,
            Widgets =
            {
                new Label(BaseContent.Styles.Label.Small)
                {
                    Text = group.Label,
                    TextColor = new Color(140, 130, 110),
                    MinWidth = 28,
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(0, 6, 0, 0)
                },
                icons
            }
        };
    }
}
