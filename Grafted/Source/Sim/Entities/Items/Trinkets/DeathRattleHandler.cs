namespace Grafted.Sim.Entities.Items.Trinkets;
using Myra.Graphics2D.Brushes;
[UsedImplicitly]
public class DeathRattleHandler : TrinketHandler
{
    public readonly RangeFloat _damage = new(80, 160);
    public const double KillDamageMultiplier = .2; // 100 percent per kills    
    private const int CooldownValue = 1200;

    private const int KillCooldownMultiplier = 100;

    public const int TotalHitsToCharge = 17;

    private int _maxCharges = 3;

    private int _hitsToCharge = 0;

    private int _charges;
    
    private Label _cooldownLabel = null!;
    private Label _chargesLabel = null!;

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

        if (part.DidPawnDieFromPartFailure())
        {
            Kills++;
        }

        return new DamageRecord(Trinket.Label, "Head Rattle", Trinket.ItemDef.WeaponProperties?.DamageType ?? DamageType.Invalid, part, damage, amountBlocked: 0)
        {
            ActualAmount = damage,
            BodyParts = damagedParts
        };
    }

    public override void PrepareTrinketButton(Button button)
    {
        var panel = new Panel
        {
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
        };

        _cooldownLabel = new Label(BaseContent.Styles.Label.Small)
        {
            TextColor = Color.Red,
            Visible = false
        };
        
        _chargesLabel = new Label(BaseContent.Styles.Label.Small)
        {
            Margin = new Thickness(15, 20, 0, 0),
            VerticalAlignment = VerticalAlignment.Bottom,
            HorizontalAlignment = HorizontalAlignment.Right,
            TextColor = Color.Gold,
            Visible = false
        };
        
        panel.Widgets.Add(_cooldownLabel);
        panel.Widgets.Add(_chargesLabel);
        
        if (button.Content is Panel content)
        {
            content.Widgets.Add(panel);
        }
    }
    
    public override void Update(Button button)
    {
        base.Update(button);
        
        if (Cooldown > 0)
        {
            _cooldownLabel.Text = Cooldown.ToString();
            _cooldownLabel.Visible = true;
            _chargesLabel.Visible = false;
        }
        else if (_charges > 0)
        {
            _cooldownLabel.Visible = false;
            _chargesLabel.Text = $"+{_charges}";
            _chargesLabel.Visible = true;
        }
        else
        {
            _cooldownLabel.Visible = false;
            _chargesLabel.Visible = false;
        }
    }

    public override void ExposeData()
    {
        ScribeValues.Look(ref _hitsToCharge, "HitsToCharge");
        ScribeValues.Look(ref _charges, "Charges");
        base.ExposeData();
    }
}