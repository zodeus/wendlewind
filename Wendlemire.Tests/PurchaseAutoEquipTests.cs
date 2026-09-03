using Microsoft.Extensions.DependencyInjection;
using Wendlemire.Definitions;
using Wendlemire.Sim;
using Wendlemire.Sim.Arena;
using Wendlemire.Sim.Entities.Items;
using Wendlemire.Sim.Entities.Items.Medicinals;
using Xunit;

namespace Wendlemire.Tests;

[Collection("Sim")]
public class PurchaseAutoEquipTests
{
    public PurchaseAutoEquipTests()
    {
        TestData.EnsureLoaded();
    }

    [Fact]
    public void EmptyArmorSlotHintsUntilThatSlotIsFilled()
    {
        using var scope = CreateArena();
        var context = scope.Context;
        context.ArenaRun!.Gold = 1000;
        var helm = Def("LeatherHelmet");
        var clothHelm = Def("ClothHelmet");
        var tunic = Def("LeatherTunic");

        Assert.True(PurchaseAutoEquip.WouldFillEmptySlot(context.PlayerPawn, helm));
        Assert.True(context.ArenaRun.TryBuy(context, Offer(helm)));
        Assert.False(PurchaseAutoEquip.WouldFillEmptySlot(context.PlayerPawn, helm));
        Assert.False(PurchaseAutoEquip.WouldFillEmptySlot(context.PlayerPawn, clothHelm));
        Assert.True(PurchaseAutoEquip.WouldFillEmptySlot(context.PlayerPawn, tunic));
    }

    [Fact]
    public void ArmorSetHintsWhenAnyPieceHasAnEmptySlot()
    {
        using var scope = CreateArena();
        var context = scope.Context;
        context.ArenaRun!.Gold = 1000;
        var set = DefRepository<MerchantDef>.GetByMoniker("Blacksmith")!
            .AllOffers.First(offer => offer.IsSet && offer.SetLabel == "Leather Set");

        Assert.True(PurchaseAutoEquip.WouldFillEmptySlot(context.PlayerPawn, set));
        Assert.True(context.ArenaRun.TryBuy(context, set));
        Assert.False(PurchaseAutoEquip.WouldFillEmptySlot(context.PlayerPawn, set));
    }

    [Fact]
    public void WeaponsHintUntilEveryHandSlotIsFilled()
    {
        using var scope = CreateArena();
        var context = scope.Context;
        context.ArenaRun!.Gold = 1000;

        Assert.True(PurchaseAutoEquip.WouldFillEmptySlot(context.PlayerPawn, Def("BoneAxe")));
        Assert.True(context.ArenaRun.TryBuy(context, Offer("BoneAxe")));
        Assert.True(PurchaseAutoEquip.WouldFillEmptySlot(context.PlayerPawn, Def("StoneHammer")));
        Assert.True(context.ArenaRun.TryBuy(context, Offer("StoneHammer")));
        Assert.False(PurchaseAutoEquip.WouldFillEmptySlot(context.PlayerPawn, Def("IronSword")));
    }

    [Fact]
    public void PotionsHintUntilEveryPotionSlotIsFilled()
    {
        using var scope = CreateArena();
        var context = scope.Context;
        context.ArenaRun!.Gold = 1000;

        Assert.True(PurchaseAutoEquip.WouldFillEmptySlot(context.PlayerPawn, Def("AcidFlask")));
        Assert.True(context.ArenaRun.TryBuy(context, Offer("AcidFlask")));
        Assert.True(context.ArenaRun.TryBuy(context, Offer("AntiStaticFlask")));
        Assert.False(PurchaseAutoEquip.WouldFillEmptySlot(context.PlayerPawn, Def("JarOfBlood")));
    }

    [Fact]
    public void FoodHintsUntilMealPlanIsFull()
    {
        using var scope = CreateArena();
        var context = scope.Context;
        context.ArenaRun!.Gold = 1000;

        Assert.True(PurchaseAutoEquip.WouldFillEmptySlot(context.PlayerPawn, Def("CookedCorn")));
        Assert.True(context.ArenaRun.TryBuy(context, Offer("CookedCorn")));
        Assert.False(PurchaseAutoEquip.WouldFillEmptySlot(context.PlayerPawn, Def("CookedMeat")));
    }

    [Fact]
    public void IncenseHintsUntilEveryIncenseSlotIsFilled()
    {
        using var scope = CreateArena();
        var context = scope.Context;
        context.ArenaRun!.Gold = 1000;

        Assert.True(PurchaseAutoEquip.WouldFillEmptySlot(context.PlayerPawn, Def("MullinStick")));
        Assert.True(context.ArenaRun.TryBuy(context, Offer("MullinStick")));
        Assert.False(PurchaseAutoEquip.WouldFillEmptySlot(context.PlayerPawn, Def("ShadeWood")));
    }

    [Fact]
    public void MedicineHintsUntilMedicalChestIsFull()
    {
        using var scope = CreateArena();
        var pawn = scope.Context.PlayerPawn;
        var medKit = Def("MedKit");

        Assert.True(PurchaseAutoEquip.WouldFillEmptySlot(pawn, medKit));
        while (pawn.MedicalChest.Slots.Count < pawn.MedicalChest.Capacity)
        {
            Assert.True(pawn.MedicalChest.TryInstall(medKit, 1));
        }

        Assert.False(PurchaseAutoEquip.WouldFillEmptySlot(pawn, medKit));
    }

    [Fact]
    public void EnchantmentsHintWhenAnEquippedHostHasAnEmptySocket()
    {
        using var scope = CreateArena();
        var context = scope.Context;
        context.ArenaRun!.Gold = 1000;
        var leaf = Def("ElvishLeaf");

        Assert.False(PurchaseAutoEquip.WouldFillEmptySlot(context.PlayerPawn, leaf));
        Assert.True(context.ArenaRun.TryBuy(context, Offer("LeatherHelmet")));
        Assert.True(PurchaseAutoEquip.WouldFillEmptySlot(context.PlayerPawn, leaf));
    }

    [Fact]
    public void TrinketsNeverHintEmptySlots()
    {
        using var scope = CreateArena();
        Assert.False(PurchaseAutoEquip.WouldFillEmptySlot(scope.Context.PlayerPawn, Def("CookingPot")));
    }

    private static ItemDef Def(string moniker) =>
        DefRepository<ItemDef>.GetByMoniker(moniker)
        ?? throw new InvalidOperationException($"Missing def '{moniker}'.");

    private static MerchantOffer Offer(string moniker) => Offer(Def(moniker));

    private static MerchantOffer Offer(ItemDef def) => new() { ItemDef = def };

    private static ArenaContextScope CreateArena() => new();

    private sealed class ArenaContextScope : IDisposable
    {
        private readonly ServiceProvider _root = SimServices.BuildRoot();
        private readonly IServiceScope _scope;

        public GameContext Context { get; }

        public ArenaContextScope()
        {
            _scope = _root.CreateScope();
            Context = _scope.ServiceProvider.GetRequiredService<GameContext>();
            Context.InitializeArena("tester", "Tester", 99);
        }

        public void Dispose()
        {
            _scope.Dispose();
            _root.Dispose();
        }
    }
}
