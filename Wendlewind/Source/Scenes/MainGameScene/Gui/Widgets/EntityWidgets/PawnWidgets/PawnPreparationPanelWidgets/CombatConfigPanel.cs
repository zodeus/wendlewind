using Image = Myra.Graphics2D.UI.Image;

namespace Wendlewind.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets.PawnPreparationPanelWidgets;

public sealed class CombatConfigPanel : VerticalStackPanel, IUpdatable
{
    private readonly Pawn _pawn;
    private readonly WeaponBar _weaponBar;
    private readonly VerticalStackPanel _potionEditors;

    public CombatConfigPanel(Pawn pawn)
    {
        _pawn = pawn;
        Spacing = 8;
        HorizontalAlignment = HorizontalAlignment.Left;

        Widgets.Add(new Label(BaseContent.Styles.Label.Normal)
        {
            Text = "Combat Config",
            TextColor = Color.Goldenrod
        });

        Widgets.Add(new Label(BaseContent.Styles.Label.Small)
        {
            Text = "Stance",
            TextColor = new Color(180, 180, 180)
        });
        Widgets.Add(new BodyStanceBar(pawn));

        Widgets.Add(new Label(BaseContent.Styles.Label.Small)
        {
            Text = "Weapons",
            TextColor = new Color(180, 180, 180)
        });
        _weaponBar = new WeaponBar(pawn);
        Widgets.Add(_weaponBar);

        Widgets.Add(new Label(BaseContent.Styles.Label.Small)
        {
            Text = "Potion Triggers",
            TextColor = new Color(180, 180, 180)
        });
        _potionEditors = new VerticalStackPanel { Spacing = 6 };
        Widgets.Add(_potionEditors);
        RefreshPotionEditors();
    }

    public void Update()
    {
        _weaponBar.Update();
        var equipped = _pawn.Equipment.Potions.ToList();
        if (_potionEditors.Widgets.Count != equipped.Count)
        {
            RefreshPotionEditors();
        }
    }

    private void RefreshPotionEditors()
    {
        _potionEditors.Widgets.Clear();
        foreach (var potion in _pawn.Equipment.Potions)
        {
            _potionEditors.Widgets.Add(new PotionTriggerEditor(potion));
        }

        if (_potionEditors.Widgets.Count == 0)
        {
            _potionEditors.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = "No potions equipped",
                TextColor = new Color(140, 140, 140)
            });
        }
    }
}

internal sealed class PotionTriggerEditor : HorizontalStackPanel
{
    private readonly Item _potion;
    private readonly Label _typeLabel;
    private readonly TextBox _valueField;
    private readonly Label _valueHint;
    private IReadOnlyList<PotionTriggerType> _allowed = [];

    public PotionTriggerEditor(Item potion)
    {
        _potion = potion;
        Spacing = 8;
        VerticalAlignment = VerticalAlignment.Center;

        potion.PotionTrigger ??= potion.ItemDef.PotionProperties?.DefaultTrigger?.Clone()
                                 ?? new PotionTrigger { Type = PotionTriggerType.Immediately };

        _allowed = potion.ItemDef.PotionProperties?.GetAllowedTriggerTypes()
                   ?? Enum.GetValues<PotionTriggerType>();

        Widgets.Add(new Image
        {
            Background = new TextureRegion(potion.GetIcon()),
            Width = BaseContent.IconSizes.Small,
            Height = BaseContent.IconSizes.Small,
            VerticalAlignment = VerticalAlignment.Center
        });

        Widgets.Add(new Label(BaseContent.Styles.Label.Small)
        {
            Text = potion.Label,
            VerticalAlignment = VerticalAlignment.Center,
            MinWidth = 120
        });

        var cycle = new CursorButton(BaseContent.Styles.Button.Small)
        {
            Content = _typeLabel = new Label(BaseContent.Styles.Label.Small)
            {
                Text = potion.PotionTrigger.Type.ToString()
            }
        };
        cycle.Click += (_, _) => CycleType();
        Widgets.Add(cycle);

        _valueHint = new Label(BaseContent.Styles.Label.Small)
        {
            Text = ValueHint(potion.PotionTrigger.Type),
            VerticalAlignment = VerticalAlignment.Center,
            TextColor = new Color(160, 160, 160)
        };
        Widgets.Add(_valueHint);

        _valueField = new TextBox
        {
            Width = 70,
            Text = CurrentValueText(potion.PotionTrigger),
            TextColor = Color.White,
            Background = new SolidBrush(new Color(25, 25, 30)),
            Padding = new Thickness(4, 2)
        };
        _valueField.TextChanged += (_, _) => ApplyValue();
        Widgets.Add(_valueField);

        RefreshValueVisibility();
    }

    private void CycleType()
    {
        if (_potion.PotionTrigger == null || _allowed.Count == 0)
        {
            return;
        }

        var current = Array.IndexOf(_allowed.ToArray(), _potion.PotionTrigger.Type);
        var next = _allowed[(current + 1) % _allowed.Count];
        _potion.PotionTrigger.Type = next;
        _typeLabel.Text = next.ToString();
        _valueHint.Text = ValueHint(next);
        _valueField.Text = CurrentValueText(_potion.PotionTrigger);
        RefreshValueVisibility();
    }

    private void ApplyValue()
    {
        if (_potion.PotionTrigger == null || !float.TryParse(_valueField.Text, out var value))
        {
            return;
        }

        switch (_potion.PotionTrigger.Type)
        {
            case PotionTriggerType.AfterSeconds:
                _potion.PotionTrigger.AfterSeconds = Math.Max(0, value);
                break;
            case PotionTriggerType.SelfBloodBelow:
            case PotionTriggerType.EnemyBloodBelow:
            case PotionTriggerType.SelfPartsDamaged:
                _potion.PotionTrigger.Threshold = Math.Clamp(value, 0, 1);
                break;
        }
    }

    private void RefreshValueVisibility()
    {
        var show = _potion.PotionTrigger?.Type != PotionTriggerType.Immediately;
        _valueField.Visible = show;
        _valueHint.Visible = show;
    }

    private static string ValueHint(PotionTriggerType type)
    {
        return type switch
        {
            PotionTriggerType.AfterSeconds => "seconds",
            PotionTriggerType.SelfBloodBelow => "blood 0-1",
            PotionTriggerType.EnemyBloodBelow => "blood 0-1",
            PotionTriggerType.SelfPartsDamaged => "parts 0-1",
            _ => ""
        };
    }

    private static string CurrentValueText(PotionTrigger trigger)
    {
        return trigger.Type == PotionTriggerType.AfterSeconds
            ? trigger.AfterSeconds.ToString("0.##")
            : trigger.Threshold.ToString("0.##");
    }
}
