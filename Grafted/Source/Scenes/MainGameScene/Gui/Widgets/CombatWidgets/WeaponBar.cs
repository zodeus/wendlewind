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
            var button = new Image
            {
                Background = new TextureRegion(weapon.Icon),
                Width = BaseContent.IconSizes.Medium, Height = BaseContent.IconSizes.Medium
            };
            Widgets.Add(button);
            _weapons.Add(new(weapon, button));
        }
    }
}