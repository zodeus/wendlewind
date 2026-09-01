namespace Wendlemire.Sim.Arena;

public class MerchantDef : Def
{
    public MerchantKind Kind;
    public MerchantAbilityKind Ability = MerchantAbilityKind.None;
    public string? TexturePath;
    public List<MerchantShelf> Shelves = [];

    public bool IsGeneralStore => Kind == MerchantKind.GeneralStore;

    public IEnumerable<MerchantOffer> AllOffers => Shelves.SelectMany(shelf => shelf.Offers);
}
