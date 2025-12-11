namespace Grafted.Sim.Entities.Items.Trinkets;
using Myra.Graphics2D.Brushes;

public abstract class TrinketHandler : IExposable
{
    private ColoredRegion? _dimmedBackground;
    private TextureRegion? _originalBackground;

    public bool IsActive { get; protected set; }
    public int Cooldown;
    public int Kills;

    public Item Trinket = null!;

    public string Label => Trinket.Label;

    public virtual void Tick()
    {
        Cooldown = Math.Clamp(Cooldown - 1, 0, int.MaxValue);
    }

    public virtual void ExposeData()
    {
        ScribeReferences.Look(ref Trinket!, "Def");
        ScribeValues.Look(ref Cooldown, "Cooldown");
        ScribeValues.Look(ref Kills, "Kills");
    }

    public override string ToString()
    {
        return $"{Trinket.Label} Handler";
    }

    public virtual bool Activate()
    {
        if (Cooldown > 0)
        {
            return false;
        }

        IsActive = true;

        return true;
    }

    public virtual void DeActivate()
    {
        IsActive = false;
    }

    public virtual void Stop()
    {
        DeActivate();
    }

    public virtual void PostCombatAction(PostCombatReport postCombatReport)
    {
    }

    public virtual DamageRecord? PostAttackHandler(Pawn victim, DamageRequest request, DamageResponse response)
    {
        return null;
    }

    public virtual void OnClick()
    {
    }

    /// <summary>
    /// Called by the update the trinket's UI.
    /// </summary>
    public virtual void Update(Button button)
    {
        if (_dimmedBackground == null)
        {
             _originalBackground= (TextureRegion)button.Content.Background;
            _dimmedBackground = new ColoredRegion(_originalBackground, new Color(100, 100, 100));
        }

        if (IsActive == true)
        {
            button.Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrameBright];
        }
        else
        {
            if (Cooldown > 0)
            {
                button.Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrameRed];
                button.Content.Background = _dimmedBackground;
            }
            else
            {
                button.Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrame];
                button.Content.Background = _originalBackground;
            }
        }
    }
    
    public virtual void PrepareTrinketButton(Button button)
    {
    }
}