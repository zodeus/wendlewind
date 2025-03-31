namespace Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets.PawnBodyPanelWidgets;

internal sealed class BodyPartTargetButton : Button
{
    private readonly BodyPart _bodyPart;
    private readonly CombatHandler? _combatHandler;
    private bool _isTargeting;
    private float _minimumHealthPercent = 0.9f;

    public BodyPartTargetButton(BodyPart bodyPart, CombatHandler? combatHandler)
    {
        _bodyPart = bodyPart;
        _combatHandler = combatHandler;
        Width = BaseContent.IconSizes.Small;
        Height = BaseContent.IconSizes.Small;
        VerticalAlignment = VerticalAlignment.Center;
        Enabled = false;
        Background = new ColoredRegion(Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.Target], Color.Transparent);
        Click += (_, _) =>
        {
            if (combatHandler == null) return;

            if (_isTargeting == false)
            {
                Target();
            }
            else
            {
                UnTarget();
            } 
        };
    }

    public void Update()
    {
        if (_bodyPart.Type==BodyPartType.Eye) return;
        _minimumHealthPercent = 0.2f;
        if (_isTargeting && (!Equals(_combatHandler?.TargetedPart, _bodyPart) || _bodyPart.IsSevered))
        {
            UnTarget();
            return;
        }

        if (_bodyPart.HealthPercent < _minimumHealthPercent && Enabled == false && _isTargeting == false)
        {
            AllowTargeting();
            return;
        }

         if (_bodyPart.HealthPercent >= _minimumHealthPercent && Enabled)
        {
            DisallowTargeting();
        }
    }

    private void DisallowTargeting()
    {
        Enabled = false;
        Background = new ColoredRegion(Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.Target], Color.Transparent);
        OverBackground = null;
        PressedBackground = null;
    }

    private void AllowTargeting()
    {
        Enabled = true;
        Background = new ColoredRegion(Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.Target], Color.DarkGoldenrod);
        OverBackground = new ColoredRegion(Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.Target], Color.Goldenrod);
        PressedBackground = new ColoredRegion(Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.Target], Color.Red);
    }

    private void Target()
    {
        _combatHandler!.TargetedPart = _bodyPart;
        _isTargeting = true;
        Background = new ColoredRegion(Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.Target], Color.Red);
        PressedBackground = new ColoredRegion(Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.Target], Color.Red);
        OverBackground = new ColoredRegion(Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.Target], Color.Red);
    }

    private void UnTarget()
    {
        _isTargeting = false;
        if (Equals(_combatHandler?.TargetedPart, _bodyPart))
        {
            _combatHandler!.TargetedPart = null;
        }

        AllowTargeting();
    }
}