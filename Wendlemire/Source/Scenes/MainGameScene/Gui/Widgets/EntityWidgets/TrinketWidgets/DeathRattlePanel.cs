namespace Wendlemire.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.TrinketWidgets;

[UsedImplicitly]
public sealed class DeathRattlePanel : EntityPanelBase
{
    private readonly DeathRattleHandler _handler;
    private readonly Item _item;
    private readonly HorizontalProgressBar _hitProgressBar;
    private readonly Label _hitProgressLabel;
    private readonly Label _chargesLabel;
    private readonly Label _damageLabel;
    private readonly Label _killsLabel;

    public DeathRattlePanel(BaseGui gui, Item item, EntityPanelProperties? properties = null) : base(gui, item, properties)
    {
        _item = item;
        _handler = (DeathRattleHandler)item.TrinketHandler!;
        Padding = new Thickness(20);
        Width = 400;

        // Header with icon and title
        var header = new HorizontalStackPanel
        {
            Spacing = 15,
            Margin = new Thickness(0, 0, 0, 10),
            Widgets =
            {
                new Image
                {
                    Background = item.GetIconImage(),
                    Width = 64, Height = 64
                },
                new VerticalStackPanel
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    Widgets =
                    {
                        new Label(BaseContent.Styles.Label.Normal)
                        {
                            Text = item.Def.Description,
                            TextColor = Color.LightGray,
                            Wrap = true,
                            MaxWidth = 280
                        }
                    }
                }
            }
        };
        Widgets.Add(header);

        // Separator
        Widgets.Add(new HorizontalSeparator { Margin = new Thickness(0, 5, 0, 15) });

        // Kills counter - prominent display
        _killsLabel = new Label(BaseContent.Styles.Label.Large)
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            TextColor = Color.Crimson,
            Text = $"{_handler.Kills}"
        };
        Widgets.Add(new VerticalStackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 15),
            Widgets =
            {
                new Label(BaseContent.Styles.Label.Small)
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextColor = Color.Gray,
                    Text = "KILLS"
                },
                _killsLabel
            }
        });

        // Charges display
        _chargesLabel = new Label(BaseContent.Styles.Label.Normal)
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            TextColor = Color.Orange,
            Text = $"Charges: {_handler.Charges} / {_handler.MaxCharges}",
            Margin = new Thickness(0, 0, 0, 10)
        };
        Widgets.Add(_chargesLabel);

        // Hit progress bar (progress toward next charge)
        _hitProgressBar = new HorizontalProgressBar(BaseContent.Styles.Bar.Health)
        {
            Value = (float)_handler.HitsToCharge / DeathRattleHandler.TotalHitsToCharge * 100,
            Height = 20,
            Margin = new Thickness(0, 0, 0, 5),
            Filler = new ColoredRegion(Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Bar.Neutral], Color.DarkOrange)
        };
        _hitProgressLabel = new Label(BaseContent.Styles.Label.Small)
        {
            Text = $"Head Hits: {_handler.HitsToCharge} / {DeathRattleHandler.TotalHitsToCharge}",
            TextColor = Color.Gray,
            Margin = new Thickness(0, 0, 0, 15)
        };
        Widgets.Add(_hitProgressBar);
        Widgets.Add(_hitProgressLabel);

        // Stats section
        var minDamage = _handler._damage.Min + (float)(_handler._damage.Min * _handler.Kills * DeathRattleHandler.KillDamageMultiplier);
        var maxDamage = _handler._damage.Max + (float)(_handler._damage.Max * _handler.Kills * DeathRattleHandler.KillDamageMultiplier);

        _damageLabel = new Label(BaseContent.Styles.Label.Normal)
        {
            Text = $"{minDamage:F0} - {maxDamage:F0}",
            TextColor = Color.White
        };

        Widgets.Add(MakeStatRow("Damage", 85, _damageLabel));
        Widgets.Add(new Label(BaseContent.Styles.Label.Small)
        {
            Text = $"+{DeathRattleHandler.KillDamageMultiplier * 100}% per kill",
            TextColor = Color.DarkGray,
            Margin = new Thickness(20, 0, 0, 10)
        });
    }

    private static HorizontalStackPanel MakeStatRow(string label, int width, Widget valueWidget)
    {
        return new HorizontalStackPanel
        {
            Spacing = 10,
            Margin = new Thickness(0, 5, 0, 0),
            Widgets =
            {
                new Label(BaseContent.Styles.Label.Normal)
                {
                    Text = $"{label}:",
                    TextColor = Color.Gray,
                    Width = width
                },
                valueWidget
            }
        };
    }

    public override void Update()
    {
        _killsLabel.Text = $"{_handler.Kills}";
        _chargesLabel.Text = $"Charges: {_handler.Charges} / {_handler.MaxCharges}";
        _hitProgressBar.Value = (float)_handler.HitsToCharge / DeathRattleHandler.TotalHitsToCharge * 100;
        _hitProgressLabel.Text = $"Head Hits: {_handler.HitsToCharge} / {DeathRattleHandler.TotalHitsToCharge}";

        var minDamage = _handler._damage.Min + (float)(_handler._damage.Min * _handler.Kills * DeathRattleHandler.KillDamageMultiplier);
        var maxDamage = _handler._damage.Max + (float)(_handler._damage.Max * _handler.Kills * DeathRattleHandler.KillDamageMultiplier);
        _damageLabel.Text = $"{minDamage:F0} - {maxDamage:F0}";
    }
}
