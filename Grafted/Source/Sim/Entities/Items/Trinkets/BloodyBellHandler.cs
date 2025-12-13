namespace Grafted.Sim.Entities.Items.Trinkets;

[UsedImplicitly]
public class BloodyBellHandler : TrinketHandler
{
    private const int DefaultCooldown = 300;
    private const float BaseBloodDrainPercent = 0.08f;
    private const float BloodDrainPerRing = 0.02f;
    private const float MaxBloodDrainPercent = 0.25f;
    
    private Label _cooldownLabel = null!;
    private Label _ringsLabel = null!;
    
    private int _totalRings;
    
    public int TotalRings => _totalRings;
    
    public float CurrentBloodDrainPercent => Math.Min(BaseBloodDrainPercent + (_totalRings * BloodDrainPerRing), MaxBloodDrainPercent);
    
    public override void OnClick()
    {
        if (Cooldown > 0) return;
        
        if (IsActive)
        {
            DeActivate();
        }
        else
        {
            Activate();
        }
    }
    
    public override void PrepareTrinketButton(Button button)
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
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        
        _ringsLabel = new Label(BaseContent.Styles.Label.Small)
        {
            Margin = new Thickness(0, 0, 2, 2),
            VerticalAlignment = VerticalAlignment.Bottom,
            HorizontalAlignment = HorizontalAlignment.Right,
            TextColor = Color.Crimson,
            Visible = false
        };
        
        panel.Widgets.Add(_cooldownLabel);
        panel.Widgets.Add(_ringsLabel);
        
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
            _ringsLabel.Visible = false;
        }
        else
        {
            _cooldownLabel.Visible = false;
            if (_totalRings > 0)
            {
                _ringsLabel.Text = $"x{_totalRings}";
                _ringsLabel.Visible = true;
            }
            else
            {
                _ringsLabel.Visible = false;
            }
        }
    }
    
    public override void ExposeData()
    {
        base.ExposeData();
        ScribeValues.Look(ref _totalRings, "TotalRings");
    }
}

