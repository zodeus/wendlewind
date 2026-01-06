namespace Grafted.Scenes.MainGameScene.Gui.CombatGui;

/// <summary>
/// Shared control panel for zone navigation between encounters.
/// Used by CombatPreparationScreen and CombatResultsScreen.
/// </summary>
public sealed class ZoneControls : Panel
{
    private static readonly Color AccentColor = new(232, 170, 0);
    private static readonly Color BossColor = new(220, 80, 80);
    private static readonly Color MysteryColor = new(140, 120, 200);

    public ZoneControls(Encounter encounter, Action onContinue, Action? onExit = null)
    {
        var isMystery = encounter.Def.MysteryProperties != null;
        var isBoss = encounter.AtBoss;
        var isZoneComplete = isBoss && onExit != null;

        var enemyName = !isMystery && encounter.Def.Enemies.Count > 0
            ? encounter.Def.Enemies.First().PawnName
            : null;

        Initialize(isMystery, isBoss, isZoneComplete, enemyName, onContinue, onExit);
    }

    private ZoneControls(EncounterProperties nextEncounterDef, bool nextIsBoss, Action onContinue)
    {
        var isMystery = nextEncounterDef.MysteryProperties != null;
        var isBoss = nextIsBoss || nextEncounterDef.IsBoss;

        var enemyName = !isMystery && nextEncounterDef.Enemies.Count > 0
            ? nextEncounterDef.Enemies.First().PawnName
            : null;

        Initialize(isMystery, isBoss, false, enemyName, onContinue, null);
    }

    private void Initialize(bool isMystery, bool isBoss, bool isZoneComplete, string? enemyName, Action onContinue, Action? onExit)
    {
        Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.FancyDark];
        Padding = new Thickness(30, 20);

        var content = new HorizontalStackPanel
        {
            Spacing = 20,
            VerticalAlignment = VerticalAlignment.Center
        };

        content.Widgets.Add(CreateButtonSection(isMystery, isBoss, isZoneComplete, onContinue, onExit));

        if (!isZoneComplete)
        {
            if (isMystery)
                content.Widgets.Add(CreateMysteryInfoSection());
            else if (enemyName != null)
                content.Widgets.Add(CreateEnemyInfoSection(enemyName, isBoss));
        }

        Widgets.Add(content);
    }

    /// <summary>
    /// Creates the ZoneControls for the results screen (post-combat).
    /// </summary>
    public static ZoneControls ForResults(Encounter encounter, Action onContinue, Action onExit)
    {
        var zone = encounter.Zone;

        // If we just beat the boss, show exit. Otherwise show next encounter info.
        if (encounter.AtBoss)
            return new ZoneControls(encounter, onContinue, onExit);

        // Get the next encounter config to show in the controls
        var nextStage = zone.Stage;
        if (nextStage < zone.ZoneDef.Encounters.Count)
        {
            var nextEncounterDef = zone.ZoneDef.Encounters[nextStage];
            var nextIsBoss = nextStage == zone.ZoneDef.Encounters.Count - 1;
            return new ZoneControls(nextEncounterDef, nextIsBoss, onContinue);
        }

        return new ZoneControls(encounter, onContinue, onExit);
    }

    private Widget CreateButtonSection(bool isMystery, bool isBoss, bool isZoneComplete, Action onContinue, Action? onExit)
    {
        var (buttonText, buttonStyle, buttonAction, labelColor) = (isZoneComplete, isMystery, isBoss) switch
        {
            (true, _, _) => ("Leave Zone", BaseContent.Styles.Button.Large, onExit!, (Color?)null),
            (_, true, _) => ("Continue", BaseContent.Styles.Button.Large, onContinue, (Color?)MysteryColor),
            (_, _, true) => ("Boss!", BaseContent.Styles.Button.LargeGold, onContinue, (Color?)BossColor),
            _ => ("Fight!", BaseContent.Styles.Button.Dark, onContinue, (Color?)null)
        };

        var button = new CursorButton(buttonStyle)
        {
            Content = new Label(BaseContent.Styles.Label.Medium)
            {
                Text = buttonText,
                TextColor = labelColor ?? Color.White,
                HorizontalAlignment = HorizontalAlignment.Center
            },
            MinWidth = 140,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        button.Click += (_, _) => buttonAction();

        return new VerticalStackPanel
        {
            Spacing = 4,
            VerticalAlignment = VerticalAlignment.Center,
            Widgets = { button }
        };
    }

    private Widget CreateEnemyInfoSection(string enemyName, bool isBoss)
    {
        var section = new VerticalStackPanel
        {
            Spacing = 4,
            VerticalAlignment = VerticalAlignment.Center,
            Widgets =
            {
                new Label(BaseContent.Styles.Label.Medium)
                {
                    Text = enemyName,
                    TextColor = isBoss ? BossColor : AccentColor
                }
            }
        };

        return section;
    }

    private Widget CreateMysteryInfoSection()
    {
        return new HorizontalStackPanel
        {
            Spacing = 8,
            VerticalAlignment = VerticalAlignment.Center,
            Widgets =
            {
                new Image
                {
                    Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.QuestionMark],
                    Width = 32,
                    Height = 32
                },
                new Label(BaseContent.Styles.Label.Medium)
                {
                    Text = "Mystery awaits...",
                    TextColor = MysteryColor,
                    VerticalAlignment = VerticalAlignment.Center
                }
            }
        };
    }
}
