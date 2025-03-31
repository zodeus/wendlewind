using System.ComponentModel;

namespace Grafted.Scenes.MainGameScene.Gui.Widgets.CombatWidgets;

public sealed class WeaponBar : HorizontalStackPanel, IUpdatable
{
    private readonly Pawn _pawn;
    private List<KeyValuePair<Item, Widget>> _weapons = new();
    private List<Item> _usableWeaponCache = new List<Item>();


    public WeaponBar(Pawn pawn)
    {
        _pawn = pawn;
        Refresh(pawn.Equipment.UsableWeapons);
    }

    public void Update()
    {
        _usableWeaponCache = _pawn.Equipment.UsableWeapons.ToList();
        if (_usableWeaponCache.Count != _weapons.Count)
        {
            Refresh(_usableWeaponCache);
        }
    }

    private void Refresh(IEnumerable<Item> usableWeapons)
    {
        Widgets.Clear();
        foreach (var weapon in usableWeapons)
        {
            var initialColor = weapon.UseInCombat ? Color.White : new Color(80, 80, 80, 160);
            var button = new Button(BaseContent.Styles.Button.Icon)
            {
                Content = new Image
                {
                    Background = new ColoredRegion(
                        new TextureRegion(weapon.Icon),
                        initialColor
                    ),
                    Width = BaseContent.IconSizes.Medium, Height = BaseContent.IconSizes.Medium,
                }
            };
            button.TouchDown += (_, _) =>
            {
                weapon.UseInCombat = !weapon.UseInCombat;
                var color = weapon.UseInCombat ? Color.White : new Color(80, 80, 80, 160);

                ((ColoredRegion)button.Content.Background).Color = color;
            };
            Widgets.Add(button);
            _weapons.Add(new KeyValuePair<Item, Widget>(weapon, button));
        }
    }
}