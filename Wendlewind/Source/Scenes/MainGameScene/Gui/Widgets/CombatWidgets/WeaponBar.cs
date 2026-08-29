namespace Wendlewind.Scenes.MainGameScene.Gui.Widgets.CombatWidgets;

public sealed class WeaponBar : HorizontalStackPanel, IUpdatable
{
    private static Color DisabledWeaponColor = new Color(50, 50, 50, 160);
    private static Color EnabledWeaponColor = Color.White;
    private static Color NonFunctionalWeaponColor = Color.Red;
    private readonly Pawn _pawn;
    private List<KeyValuePair<Item, (ColoredRegion, BodyPart)>> _weapons = new();

    public WeaponBar(Pawn pawn)
    {
        _pawn = pawn;
        Refresh();
    }

    public void Update()
    {
        if (WeaponsDirty())
        {
            Refresh();
        }
        foreach (var (weapon, (backgroundImage, bodyPart)) in _weapons)
        {
            // if body part is disabled, make the border red
            if (bodyPart.IsFunctional == false)
            {
                backgroundImage.Color = NonFunctionalWeaponColor;
            }
            else
            {
                backgroundImage.Color = weapon.UseInCombat ? EnabledWeaponColor : DisabledWeaponColor;
            }
        }
    }

    private bool WeaponsDirty()
    {
        var actualWeapons = _pawn.Equipment.Weapons.ToList();
        return actualWeapons.Select(w => w.Item1).Intersect(_weapons.Select(w => w.Key)).Count() != _weapons.Count;
        
    }

    private void Refresh()
    {
        Widgets.Clear();
        _weapons.Clear();
        foreach (var (weapon, bodyPart) in _pawn.Equipment.Weapons)
        {
            var initialColor = weapon.UseInCombat ? EnabledWeaponColor : DisabledWeaponColor;
            var backgroundImage = new ColoredRegion(new TextureRegion(weapon.GetIcon()), initialColor);
            var button = new CursorButton(BaseContent.Styles.Button.Icon)
            {
                Content = new Image
                {
                    Background = backgroundImage,
                    Width = BaseContent.IconSizes.Medium,
                    Height = BaseContent.IconSizes.Medium,
                }
            };
            button.TouchDown += (_, _) =>
            {
                weapon.UseInCombat = !weapon.UseInCombat;
                backgroundImage.Color = weapon.UseInCombat ? EnabledWeaponColor : DisabledWeaponColor;
            };
            Widgets.Add(button);
            _weapons.Add(new KeyValuePair<Item, (ColoredRegion, BodyPart)>(weapon, (backgroundImage, bodyPart)));
        }
    }
}