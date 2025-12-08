namespace Grafted.Sim.Entities.Items.Trinkets;

[UsedImplicitly]
public class SlingshotHandler : TrinketHandler
{
    private Item? _ammo;

    public Item? Ammo => _ammo;

    public AmmoProperties? AmmoProperties => _ammo?.ItemDef.AmmoProperties;

    private Label _cooldownLabel = null!;
    private Label _chargesLabel = null!;
    private Image _ammoIcon = null!;

    private const int CooldownValue = 180;

    public override void ExposeData()
    {
        base.ExposeData();
        ScribeDeep.Look(ref _ammo, "Ammo");
    }

    public override DamageRecord? PostAttackHandler(Pawn victim, DamageRequest request, DamageResponse response)
    {
        if (_ammo == null || !IsActive) return null;

        var ammoProps = AmmoProperties!;
        var damage = ammoProps.DamageRange.RandomValue;
        var randomPart = victim.Body.AllExternalParts.RandomElement();
        var damageRecord = new DamageRecord(Trinket.Label, "Fling", ammoProps.DamageType, randomPart, damage, amountBlocked: 0);
        randomPart.ApplyDamageToExternalPart(new Damage(Trinket, damage, "Fling"));
        
        damageRecord.ActualAmount = damage;
        _ammo.StackSize--;
        if (_ammo.StackSize < 1)
        {
            _ammo.Destroy();
            _ammo = null;
        }
        Cooldown = CooldownValue;
        DeActivate();

        return damageRecord;
    }

    public override void OnClick()
    {
        if (_ammo == null) return;
        if (IsActive)
        {
            DeActivate();
        }
        else
        {
            Activate();
        }
    }

    public Item? LoadAmmo(Item? ammo)
    {
        var oldAmmo = _ammo;
        _ammo = ammo;
        return oldAmmo;
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
        
        _ammoIcon = new Image
        {
            Width = 12,
            Height = 12,
            VerticalAlignment = VerticalAlignment.Bottom,
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, -4, -4, 0),
            Visible = false
        };
        
        panel.Widgets.Add(_cooldownLabel);
        panel.Widgets.Add(_chargesLabel);
        panel.Widgets.Add(_ammoIcon);
        
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
            _ammoIcon.Visible = false;
        }
        else if (_ammo != null)
        {
            _cooldownLabel.Visible = false;
            _chargesLabel.Text = $"+{_ammo.StackSize}";
            _chargesLabel.Visible = true;
            _ammoIcon.Background = new TextureRegion(_ammo.Icon);
            _ammoIcon.Visible = true;
        }
        else
        {
            _cooldownLabel.Visible = false;
            _chargesLabel.Visible = false;
            _ammoIcon.Visible = false;
        }
    }
}

