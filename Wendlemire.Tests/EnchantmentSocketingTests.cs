using Wendlemire.Sim.Entities.Items.Enchantments;
using Xunit;

namespace Wendlemire.Tests;

[Collection("Sim")]
public class EnchantmentSocketingTests
{
    public EnchantmentSocketingTests()
    {
        TestData.EnsureLoaded();
    }

    [Fact]
    public void ArmorEnchantmentSocketsLeatherButNotWeapon()
    {
        using var harness = BodyTestHarness.Human();
        var helm = harness.EquipArmor("LeatherHelmet");
        var sword = harness.CreateWeapon("IronSword");
        var leaf = harness.CreateItem("ElvishLeaf");
        harness.Pawn.Inventory.TryAdd(leaf);

        Assert.True(EnchantmentSocketing.CanSocket(helm, leaf));
        Assert.False(EnchantmentSocketing.CanSocket(sword, leaf));
        Assert.True(EnchantmentSocketing.HostAcceptsUnequipped(harness.Pawn, helm));
        Assert.False(EnchantmentSocketing.HostAcceptsUnequipped(harness.Pawn, sword));
        Assert.True(EnchantmentSocketing.EnchantmentHasCompatibleHost(harness.Pawn, leaf));
    }

    [Fact]
    public void TrySocketFillsEmptySocketAndClearsHighlight()
    {
        using var harness = BodyTestHarness.Human();
        var helm = harness.EquipArmor("LeatherHelmet");
        var leaf = harness.CreateItem("ElvishLeaf");
        harness.Pawn.Inventory.TryAdd(leaf);

        Assert.True(EnchantmentSocketing.TrySocket(helm, leaf));
        Assert.Same(leaf, helm.Enchantments!.TryGetAtSocket(0));
        Assert.DoesNotContain(leaf, harness.Pawn.Inventory);
        Assert.False(EnchantmentSocketing.HostAcceptsUnequipped(harness.Pawn, helm));
    }

    [Fact]
    public void WeaponEnchantmentDoesNotHighlightArmor()
    {
        using var harness = BodyTestHarness.Human();
        var helm = harness.EquipArmor("LeatherHelmet");
        var wounds = harness.CreateItem("FesteringWounds");
        harness.Pawn.Inventory.TryAdd(wounds);

        Assert.False(EnchantmentSocketing.CanSocket(helm, wounds));
        Assert.False(EnchantmentSocketing.HostAcceptsUnequipped(harness.Pawn, helm));
        Assert.False(EnchantmentSocketing.EnchantmentHasCompatibleHost(harness.Pawn, wounds));
    }

    [Fact]
    public void DaggerAcceptsWeaponEnchantment()
    {
        using var harness = BodyTestHarness.Human();
        var dagger = harness.CreateWeapon("IronDagger");
        var wounds = harness.CreateItem("FesteringWounds");
        harness.Pawn.Inventory.TryAdd(wounds);

        Assert.NotNull(dagger.Enchantments);
        Assert.True(EnchantmentSocketing.CanSocket(dagger, wounds));
        Assert.True(EnchantmentSocketing.TrySocket(dagger, wounds));
        Assert.Same(wounds, dagger.Enchantments!.TryGetAtSocket(0));
    }

    [Fact]
    public void SwordDoesNotAcceptWeaponEnchantment()
    {
        using var harness = BodyTestHarness.Human();
        var sword = harness.CreateWeapon("IronSword");
        var wounds = harness.CreateItem("FesteringWounds");
        harness.Pawn.Inventory.TryAdd(wounds);

        Assert.Null(sword.Enchantments);
        Assert.False(EnchantmentSocketing.CanSocket(sword, wounds));
        Assert.False(EnchantmentSocketing.TrySocket(sword, wounds));
    }
}
