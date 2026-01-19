namespace Grafted.Sim.Entities.Items.Trinkets;

[UsedImplicitly]
public class DeathRattleHandler : TrinketHandler
{
    public readonly RangeFloat _damage = new(80, 160);
    public const double KillDamageMultiplier = 1;
    private const int CooldownValue = 1200;

    private const int KillCooldownMultiplier = 20;

    public const int TotalHitsToCharge = 17;

    private int _maxCharges = 3;

    private int _hitsToCharge = 0;

    private int _charges;

    private Label _cooldownLabel = null!;
    private Label _chargesLabel = null!;
    private HorizontalProgressBar _hitProgressBar = null!;

    public int Charges => _charges;
    public int HitsToCharge => _hitsToCharge;

    public int KillCooldown => CooldownValue + (Kills * KillCooldownMultiplier);
    public int MaxCharges => _maxCharges;

    public override DamageRecord? PostAttackHandler(Pawn victim, DamageRequest request, DamageResponse response)
    {
        if (request.TargetedPart?.Type == BodyPartType.Head)
        {
            _hitsToCharge++;
            if (_hitsToCharge >= TotalHitsToCharge)
            {
                _charges = Math.Min(_charges + 1, MaxCharges);
                _hitsToCharge = 0;
            }
        }

        if (IsActive)
        {
            return UseCharge(request);
        }

        return null;
    }

    public override void DeActivate()
    {
        base.DeActivate();
    }


    public override void OnClick()
    {
        if (_charges < 1) return;

        if (IsActive)
        {
            DeActivate();
        }
        else
        {
            Activate();
        }
    }

    private DamageRecord? UseCharge(DamageRequest request)
    {
        var part = request.TargetedPart;
        if (part?.Type != BodyPartType.Head) return null;

        DeActivate();
        _charges--;
        Cooldown = KillCooldown;

        List<DamagedBodyPartRecord> damagedParts = [];
        var minDamage = _damage.Min + (float)(_damage.Min * Kills * KillDamageMultiplier);
        var maxDamage = _damage.Max + (float)(_damage.Max * Kills * KillDamageMultiplier);
        var damage = (double)new RangeFloat(minDamage, maxDamage).RandomValue;
        part.ApplyDamageToExternalPart(new Damage(Trinket, damage, "Rattle"), damagedParts);

        if (part.Body?.Pawn.IsDeadFromPartFailure() != null)
        {
            Kills++;
        }

        var damageRecord = new DamageRecord(Trinket.Label, "Head Rattle", Trinket.ItemDef.WeaponProperties?.DamageType ?? DamageType.Invalid, part, damage, amountBlocked: 0)
        {
            ActualAmount = damage,
            BodyParts = damagedParts
        };
        damageRecord.ReflectedEffects.Add(new ReflectedStatusEffect(part.Body?.Pawn ?? null!, Trinket.ItemDef, "Death Rattle"));
        return damageRecord;
    }

    public override void PrepareTrinketButton(CursorButton button)
    {
        var panel = new Panel
        {
            VerticalAlignment = VerticalAlignment.Stretch,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };

        _cooldownLabel = new Label(BaseContent.Styles.Label.Small)
        {
            TextColor = Color.Red,
            Visible = false,
            VerticalAlignment = VerticalAlignment.Center,
        };

        _chargesLabel = new Label(BaseContent.Styles.Label.Small)
        {
            Margin = new Thickness(15, 20, 0, 0),
            VerticalAlignment = VerticalAlignment.Bottom,
            HorizontalAlignment = HorizontalAlignment.Right,
            TextColor = Color.Gold,
            Visible = false
        };

        _hitProgressBar = new HorizontalProgressBar(BaseContent.Styles.Bar.Health)
        {
            Height = 12,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(2, 0, 2, 2),
            Filler = new ColoredRegion(Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Bar.Neutral], Color.GreenYellow),
            Value = (float)_hitsToCharge / TotalHitsToCharge * 100
        };

        panel.Widgets.Add(_cooldownLabel);
        panel.Widgets.Add(_chargesLabel);
        panel.Widgets.Add(_hitProgressBar);

        if (button.Content is Panel content)
        {
            content.Widgets.Add(panel);
        }
    }

    public override void Update(CursorButton button)
    {
        base.Update(button);

        _hitProgressBar.Value = (float)_hitsToCharge / TotalHitsToCharge * 100;

        if (Cooldown > 0)
        {
            _cooldownLabel.Text = Cooldown.ToString();
            _cooldownLabel.Visible = true;
            _chargesLabel.Visible = false;
            _hitProgressBar.Visible = false;
        }
        else if (_charges > 0)
        {
            _cooldownLabel.Visible = false;
            _chargesLabel.Text = $"+{_charges}";
            _chargesLabel.Visible = true;
            _hitProgressBar.Visible = true;
        }
        else
        {
            button.Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrameRed];
            _cooldownLabel.Visible = false;
            _chargesLabel.Visible = false;
            _hitProgressBar.Visible = true;
        }
    }

    public override void ExposeData()
    {
        ScribeValues.Look(ref _hitsToCharge, "HitsToCharge");
        ScribeValues.Look(ref _charges, "Charges");
        base.ExposeData();
    }
}