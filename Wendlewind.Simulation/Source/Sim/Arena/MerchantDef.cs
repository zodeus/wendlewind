namespace Wendlewind.Sim.Arena;

public class MerchantDef : Def
{
    public MerchantKind Kind;
    public MerchantAbilityKind Ability = MerchantAbilityKind.None;
    public int StockSize = 6;
    public List<MerchantOffer> Offers = [];

    public bool IsGeneralStore => Kind == MerchantKind.GeneralStore;
}
