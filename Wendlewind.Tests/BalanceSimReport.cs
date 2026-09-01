using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Wendlewind.NetCode;
using Wendlewind.NetCode.Contracts;
using Wendlewind.Sim.Combat;
using Wendlewind.Sim.Entities.Pawns;
using Xunit;
using Xunit.Abstractions;

namespace Wendlewind.Tests;

/// <summary>
/// Human-vs-human balance across a ~13-round arena.
/// Shop timing used for loadouts:
///   R1+  primitive/iron, cloth/leather, Festering, Soothing
///   R2+  SpidersBite, ElvishLeaf, WD pieces
///   R4+  chain, WD set, BoneEater, BloodBath, FireStaff
///   R6+  Everburning, RhinoSkin, BlessedIronCollar
/// Writes balance-report.txt at the repo root.
/// Run: dotnet test --filter FullyQualifiedName~BalanceSimReport
/// </summary>
[Collection("Sim")]
public class BalanceSimReport
{
    private static readonly JsonSerializerOptions SidecarJson = new()
    {
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals
    };

    private const int SeedCount = 24;
    private const int TargetMinTicks = 900;
    private const int TargetMaxTicks = 1500;

    private readonly ITestOutputHelper _output;

    public BalanceSimReport(ITestOutputHelper output)
    {
        _output = output;
        TestData.EnsureLoaded();
    }

    #region Sets

    private static readonly string[] ClothCore = ["ClothHelmet", "ClothTunic"];
    private static readonly string[] LeatherCore = ["LeatherHelmet", "LeatherTunic"];

    private static readonly string[] ClothSet =
    [
        "ClothHelmet", "ClothGorget", "ClothTunic",
        "ClothGlove", "ClothGlove", "ClothVambrace", "ClothVambrace",
        "ClothGreave", "ClothGreave", "ClothBoot", "ClothBoot"
    ];

    private static readonly string[] LeatherSet =
    [
        "LeatherHelmet", "LeatherGorget", "LeatherTunic",
        "LeatherGlove", "LeatherGlove", "LeatherVambrace", "LeatherVambrace",
        "LeatherGreave", "LeatherGreave", "LeatherBoot", "LeatherBoot"
    ];

    private static readonly string[] ChainSet =
    [
        "ChainHelmet", "ChainGorget", "ChainTunic",
        "ChainGlove", "ChainGlove", "ChainVambrace", "ChainVambrace",
        "ChainGreave", "ChainGreave", "ChainBoot", "ChainBoot"
    ];

    private static readonly string[] WitchDoctorSet =
    [
        "WitchDoctorHelmet", "WitchDoctorGorget", "WitchDoctorTunic",
        "WitchDoctorGlove", "WitchDoctorGlove", "WitchDoctorVambrace", "WitchDoctorVambrace",
        "WitchDoctorGreave", "WitchDoctorGreave", "WitchDoctorBoot", "WitchDoctorBoot"
    ];

    private static readonly string[] WdUniqueMix =
    [
        "PlagueMask", "BlessedIronCollar", "WitchDoctorTunic",
        "WitchDoctorGlove", "WitchDoctorGlove", "WitchDoctorVambrace", "WitchDoctorVambrace",
        "WitchDoctorGreave", "WitchDoctorGreave", "WitchDoctorBoot", "WitchDoctorBoot"
    ];

    #endregion

    #region Builder

    private static BuildSnapshot Fighter(
        string id,
        string[] weapons,
        string[]? armor = null,
        SocketedItemConfig[]? sockets = null)
    {
        var items = new List<string>();
        items.AddRange(weapons);
        if (armor != null)
        {
            items.AddRange(armor);
        }

        return new BuildSnapshot
        {
            PlayerId = id,
            BuildId = id,
            PawnDefMoniker = "HumanA",
            EntityDefMonikers = items.ToArray(),
            StanceMoniker = "Offensive",
            Weapons = weapons
                .Select(w => new WeaponConfig { ItemMoniker = w, UseInCombat = true })
                .ToArray(),
            Sockets = sockets ?? []
        };
    }

    private static SocketedItemConfig Sock(string item, params string[] enchants) =>
        new() { ItemMoniker = item, EnchantmentMonikers = enchants };

    private static SocketedItemConfig[] LeatherLightEnchants() =>
    [
        Sock("LeatherHelmet", "ElvishLeaf"),
        Sock("LeatherGlove", "SoothingVibrations"),
        Sock("LeatherBoot", "ElvishLeaf")
    ];

    private static SocketedItemConfig[] LeatherMidEnchants() =>
    [
        Sock("LeatherHelmet", "ElvishLeaf"),
        Sock("LeatherGlove", "BloodBath"),
        Sock("LeatherGlove", "ElvishLeaf"),
        Sock("LeatherBoot", "SoothingVibrations")
    ];

    private static SocketedItemConfig[] ChainFullEnchants() =>
    [
        Sock("ChainHelmet", "RhinoSkin"),
        Sock("ChainGorget", "ElvishLeaf"),
        Sock("ChainTunic", "RhinoSkin"),
        Sock("ChainGlove", "BloodBath"),
        Sock("ChainGlove", "ElvishLeaf"),
        Sock("ChainVambrace", "SoothingVibrations"),
        Sock("ChainVambrace", "ElvishLeaf"),
        Sock("ChainGreave", "RhinoSkin"),
        Sock("ChainGreave", "BloodBath"),
        Sock("ChainBoot", "SoothingVibrations"),
        Sock("ChainBoot", "ElvishLeaf")
    ];

    private static SocketedItemConfig[] WdHealStack() =>
    [
        Sock("WitchDoctorHelmet", "RhinoSkin", "ElvishLeaf"),
        Sock("WitchDoctorGorget", "ElvishLeaf", "SoothingVibrations"),
        Sock("WitchDoctorTunic", "RhinoSkin", "ElvishLeaf"),
        Sock("WitchDoctorGlove", "BloodBath", "ElvishLeaf"),
        Sock("WitchDoctorGlove", "SoothingVibrations", "ElvishLeaf"),
        Sock("WitchDoctorVambrace", "RhinoSkin", "BloodBath"),
        Sock("WitchDoctorVambrace", "ElvishLeaf", "SoothingVibrations"),
        Sock("WitchDoctorGreave", "RhinoSkin", "ElvishLeaf"),
        Sock("WitchDoctorGreave", "BloodBath", "SoothingVibrations"),
        Sock("WitchDoctorBoot", "RhinoSkin", "ElvishLeaf"),
        Sock("WitchDoctorBoot", "BloodBath", "ElvishLeaf")
    ];

    private static SocketedItemConfig[] WdReflectStack() =>
    [
        Sock("WitchDoctorHelmet", "SpidersBite", "RhinoSkin"),
        Sock("WitchDoctorGorget", "SpidersBite", "ElvishLeaf"),
        Sock("WitchDoctorTunic", "SpidersBite", "BloodBath"),
        Sock("WitchDoctorGlove", "SpidersBite", "ElvishLeaf"),
        Sock("WitchDoctorGlove", "SpidersBite", "SoothingVibrations"),
        Sock("WitchDoctorVambrace", "SpidersBite", "RhinoSkin"),
        Sock("WitchDoctorVambrace", "ElvishLeaf", "BloodBath"),
        Sock("WitchDoctorGreave", "SpidersBite", "ElvishLeaf"),
        Sock("WitchDoctorGreave", "RhinoSkin", "SoothingVibrations"),
        Sock("WitchDoctorBoot", "SpidersBite", "BloodBath"),
        Sock("WitchDoctorBoot", "ElvishLeaf", "RhinoSkin")
    ];

    private static SocketedItemConfig[] UniqueMixEnchants() =>
    [
        Sock("PlagueMask", "RhinoSkin", "SpidersBite"),
        Sock("BlessedIronCollar", "RhinoSkin", "ElvishLeaf", "BloodBath"),
        Sock("WitchDoctorTunic", "RhinoSkin", "ElvishLeaf"),
        Sock("WitchDoctorGlove", "BloodBath", "SpidersBite"),
        Sock("WitchDoctorGlove", "ElvishLeaf", "SoothingVibrations"),
        Sock("WitchDoctorVambrace", "RhinoSkin", "BloodBath"),
        Sock("WitchDoctorVambrace", "ElvishLeaf", "SpidersBite"),
        Sock("WitchDoctorGreave", "RhinoSkin", "SoothingVibrations"),
        Sock("WitchDoctorGreave", "BloodBath", "ElvishLeaf"),
        Sock("WitchDoctorBoot", "RhinoSkin", "SpidersBite"),
        Sock("WitchDoctorBoot", "BloodBath", "ElvishLeaf")
    ];

    private static SocketedItemConfig[] Weapon(string moniker, string enchant) =>
        [Sock(moniker, enchant)];

    private static SocketedItemConfig[] Combine(params SocketedItemConfig[][] groups) =>
        groups.SelectMany(g => g).ToArray();

    #endregion

    #region Loadouts by round band

    // R1-3
    private static BuildSnapshot ClubNaked(string id) => Fighter(id, ["WoodClub"]);
    private static BuildSnapshot AxeNaked(string id) => Fighter(id, ["BoneAxe"]);
    private static BuildSnapshot SpearNaked(string id) => Fighter(id, ["BoneSpear"]);
    private static BuildSnapshot AxeCloth(string id) => Fighter(id, ["BoneAxe"], ClothSet);
    private static BuildSnapshot AxeLeatherCore(string id) => Fighter(id, ["BoneAxe"], LeatherCore);
    private static BuildSnapshot SwordFesterCloth(string id) =>
        Fighter(id, ["IronSword"], ClothSet, Weapon("IronSword", "FesteringWounds"));

    // R4-6
    private static BuildSnapshot SwordLeather(string id) => Fighter(id, ["IronSword"], LeatherSet);
    private static BuildSnapshot MaceLeather(string id) => Fighter(id, ["IronMace"], LeatherSet);
    private static BuildSnapshot DualIronLeather(string id) =>
        Fighter(id, ["IronSword", "IronDagger"], LeatherSet,
            Combine(Weapon("IronDagger", "FesteringWounds"), LeatherLightEnchants()));
    private static BuildSnapshot SwordLeatherLeaf(string id) =>
        Fighter(id, ["IronSword"], LeatherSet,
            Combine(Weapon("IronSword", "FesteringWounds"), LeatherMidEnchants()));
    private static BuildSnapshot WdPartialBoneEater(string id) =>
        Fighter(id, ["IronSword"],
            ["WitchDoctorHelmet", "WitchDoctorTunic", "LeatherGorget",
             "LeatherGlove", "LeatherGlove", "LeatherGreave", "LeatherGreave"],
            Combine(Weapon("IronSword", "BoneEater"),
                [Sock("WitchDoctorHelmet", "ElvishLeaf"), Sock("WitchDoctorTunic", "ElvishLeaf")]));

    // R7-9
    private static BuildSnapshot SwordChain(string id) => Fighter(id, ["IronSword"], ChainSet);
    private static BuildSnapshot ChainBurn(string id) =>
        Fighter(id, ["IronSword"], ChainSet, Weapon("IronSword", "EverburningStone"));
    private static BuildSnapshot ChainBone(string id) =>
        Fighter(id, ["IronMace"], ChainSet, Weapon("IronMace", "BoneEater"));
    private static BuildSnapshot ChainSpider(string id) =>
        Fighter(id, ["IronSword"], ChainSet, Weapon("IronSword", "SpidersBite"));
    private static BuildSnapshot DualDoTLeather(string id) =>
        Fighter(id, ["IronSword", "IronDagger"], LeatherSet,
            Combine(
                [Sock("IronSword", "EverburningStone"), Sock("IronDagger", "FesteringWounds")],
                LeatherMidEnchants()));
    private static BuildSnapshot WdPlainBone(string id) =>
        Fighter(id, ["IronSword"], WitchDoctorSet, Weapon("IronSword", "BoneEater"));

    // R10-13
    private static BuildSnapshot ChainStackedBurn(string id) =>
        Fighter(id, ["IronSword", "IronDagger"], ChainSet,
            Combine(
                [Sock("IronSword", "EverburningStone"), Sock("IronDagger", "BoneEater")],
                ChainFullEnchants()));
    private static BuildSnapshot WdHealBurn(string id) =>
        Fighter(id, ["IronSword"], WitchDoctorSet,
            Combine(Weapon("IronSword", "EverburningStone"), WdHealStack()));
    private static BuildSnapshot WdHealFester(string id) =>
        Fighter(id, ["IronSword"], WitchDoctorSet,
            Combine(Weapon("IronSword", "FesteringWounds"), WdHealStack()));
    private static BuildSnapshot WdReflectBite(string id) =>
        Fighter(id, ["IronClaws"], WitchDoctorSet,
            Combine(Weapon("IronClaws", "SpidersBite"), WdReflectStack()));
    private static BuildSnapshot UniqueMixBurn(string id) =>
        Fighter(id, ["IronSword", "IronMace"], WdUniqueMix,
            Combine(
                [Sock("IronSword", "EverburningStone"), Sock("IronMace", "BoneEater")],
                UniqueMixEnchants()));
    private static BuildSnapshot FireStaffFester(string id) =>
        Fighter(id, ["FireStaff", "IronDagger"], WitchDoctorSet,
            Combine(Weapon("IronDagger", "FesteringWounds"), WdHealStack()));

    #endregion

    private sealed record Matchup(string Band, string Name, BuildSnapshot Attacker, BuildSnapshot Defender);

    private static List<Matchup> Matchups() =>
    [
        // --- R1-3: unarmored / partial + primitive, first iron/enchant ---
        new("R1-3", "Club vs Club (naked)", ClubNaked("A"), ClubNaked("B")),
        new("R1-3", "Axe vs Axe (naked)", AxeNaked("A"), AxeNaked("B")),
        new("R1-3", "Spear vs Axe (naked)", SpearNaked("A"), AxeNaked("B")),
        new("R1-3", "Axe+cloth vs Axe+cloth", AxeCloth("A"), AxeCloth("B")),
        new("R1-3", "Axe+leather-core vs Axe+leather-core", AxeLeatherCore("A"), AxeLeatherCore("B")),
        new("R1-3", "Sword+Fester+cloth vs Axe+cloth", SwordFesterCloth("A"), AxeCloth("B")),

        // --- R4-6: full leather, first WD/chain pieces, BoneEater ---
        new("R4-6", "Sword+leather vs Sword+leather", SwordLeather("A"), SwordLeather("B")),
        new("R4-6", "Mace+leather vs Sword+leather", MaceLeather("A"), SwordLeather("B")),
        new("R4-6", "Dual-iron+Fester+leaf vs same", DualIronLeather("A"), DualIronLeather("B")),
        new("R4-6", "Sword+Fester+leather-ench vs same", SwordLeatherLeaf("A"), SwordLeatherLeaf("B")),
        new("R4-6", "WD-partial+BoneEater vs Sword+leather", WdPartialBoneEater("A"), SwordLeather("B")),
        new("R4-6", "Sword+leather vs Axe+leather-core", SwordLeather("A"), AxeLeatherCore("B")),

        // --- R7-9: full chain/WD, Everburning / Rhino come online ---
        new("R7-9", "Sword+chain vs Sword+chain", SwordChain("A"), SwordChain("B")),
        new("R7-9", "Chain+Burn vs Chain (plain)", ChainBurn("A"), SwordChain("B")),
        new("R7-9", "Chain+Burn vs Chain+BoneEater", ChainBurn("A"), ChainBone("B")),
        new("R7-9", "Chain+Burn vs Chain+SpidersBite", ChainBurn("A"), ChainSpider("B")),
        new("R7-9", "Dual-DoT+leather vs same", DualDoTLeather("A"), DualDoTLeather("B")),
        new("R7-9", "WD+BoneEater vs Sword+chain", WdPlainBone("A"), SwordChain("B")),

        // --- R10-13: stacked sockets. Symmetric full-heal mirrors omitted —
        // WD heal-stack+Burn vs same already measured ~400s / 96% bleed (regen wins).
        new("R10-13", "Chain stacked+Burn/Bone vs same", ChainStackedBurn("A"), ChainStackedBurn("B")),
        new("R10-13", "WD heal+Burn vs Chain stacked", WdHealBurn("A"), ChainStackedBurn("B")),
        new("R10-13", "WD heal+Fester vs Chain stacked", WdHealFester("A"), ChainStackedBurn("B")),
        new("R10-13", "WD reflect+Bite vs Chain stacked", WdReflectBite("A"), ChainStackedBurn("B")),
        new("R10-13", "Unique-mix+Burn/Bone vs Chain stacked", UniqueMixBurn("A"), ChainStackedBurn("B")),
        new("R10-13", "FireStaff+Fester+WD vs Chain stacked", FireStaffFester("A"), ChainStackedBurn("B")),
        new("R10-13", "WD heal+Burn vs WD+BoneEater (no stack)", WdHealBurn("A"), WdPlainBone("B")),
        new("R10-13", "Unique-mix vs WD+BoneEater (no stack)", UniqueMixBurn("A"), WdPlainBone("B")),

        // --- Cross-band (what a leftover early build faces later) ---
        new("X", "R1 axe vs R5 sword-leather", AxeNaked("A"), SwordLeather("B")),
        new("X", "R5 sword-leather vs R8 chain-burn", SwordLeather("A"), ChainBurn("B")),
        new("X", "R6 dual-iron vs R12 WD-heal-burn", DualIronLeather("A"), WdHealBurn("B")),
        new("X", "R8 chain-plain vs R12 unique-mix", SwordChain("A"), UniqueMixBurn("B")),
    ];

    [Fact]
    public void GenerateReport()
    {
        const string path = @"c:\Users\hawkk\dev-personal\wendlewind\balance-report.txt";
        const string sidecarPath = @"c:\Users\hawkk\dev-personal\wendlewind\balance-report.blood.jsonl";
        var sb = new StringBuilder();
        void Flush() => File.WriteAllText(path, sb.ToString());
        var done = new HashSet<string>(StringComparer.Ordinal);
        var rows = new List<(Matchup Matchup, MatchupResult Row)>();
        if (File.Exists(sidecarPath))
        {
            foreach (var line in File.ReadAllLines(sidecarPath))
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var stored = JsonSerializer.Deserialize<StoredRow>(line, SidecarJson);
                if (stored == null)
                {
                    continue;
                }

                done.Add(stored.Name);
                var matchup = Matchups().First(m => m.Name == stored.Name);
                rows.Add((matchup, stored.Row));
            }
        }

        sb.AppendLine("=== Wendlewind Human-vs-Human Balance (13-round curve) ===");
        sb.AppendLine($"Seeds/matchup: {SeedCount}   Target: {TargetMinTicks / 60}-{TargetMaxTicks / 60}s @ 60tps");
        sb.AppendLine("Knobs this pass: AS 1 = 2 swings/s; ElvishLeaf 0.0001; RhinoSkin refund 10%; primitive 58; iron 52; chain 10; leather 5; WD 12; human HP -15%");
        sb.AppendLine("Sever dump: currentBlood * (subtree BloodAmount / body BloodAmount) on Severe()");
        sb.AppendLine();
        AppendHumanBloodShares(sb);
        sb.AppendLine();
        sb.AppendLine($"{"Band",-7} {"Matchup",-46} {"med.s",6} {"mean",6} {"p10",5} {"p90",5} {"band%",6} {"Awin%",6} {"bleed%",7} {"organ%",7} {"sever%",7} {"loseB%",7} {"winB%",6} {"DPS",5}  topCause");
        sb.AppendLine(new string('-', 180));
        string? lastBand = null;
        foreach (var (m, row) in rows.OrderBy(r => Matchups().FindIndex(x => x.Name == r.Matchup.Name)))
        {
            if (lastBand != null && lastBand != m.Band)
            {
                sb.AppendLine();
            }

            lastBand = m.Band;
            sb.AppendLine(FormatMatchupLine(m, row));
        }

        Flush();

        var bandMedians = new Dictionary<string, List<double>>();
        foreach (var (m, row) in rows)
        {
            if (!bandMedians.TryGetValue(m.Band, out var prior))
            {
                bandMedians[m.Band] = prior = [];
            }

            prior.Add(row.MedianSeconds);
        }

        foreach (var m in Matchups())
        {
            if (done.Contains(m.Name))
            {
                continue;
            }

            if (lastBand != null && lastBand != m.Band)
            {
                sb.AppendLine();
            }

            lastBand = m.Band;
            var row = RunMatchup(m);
            rows.Add((m, row));
            GC.Collect();
            GC.WaitForPendingFinalizers();
            if (!bandMedians.TryGetValue(m.Band, out var list))
            {
                bandMedians[m.Band] = list = [];
            }

            list.Add(row.MedianSeconds);

            var line = FormatMatchupLine(m, row);
            sb.AppendLine(line);
            _output.WriteLine(line);
            File.AppendAllText(sidecarPath, JsonSerializer.Serialize(new StoredRow(m.Band, m.Name, row), SidecarJson) + Environment.NewLine);
            Flush();
        }

        sb.AppendLine();
        sb.AppendLine("--- Blood & severs ---");
        sb.AppendLine($"{"Band",-7} {"Matchup",-46} {"bleed%",7} {"sever%",7} {"b|sev",6} {"b|no",5} {"loseB%",7} {"winB%",6} {"sev->s",6} {"inst%",6}");
        sb.AppendLine(new string('-', 130));
        lastBand = null;
        foreach (var (m, row) in rows.OrderBy(r => Matchups().FindIndex(x => x.Name == r.Matchup.Name)))
        {
            if (lastBand != null && lastBand != m.Band)
            {
                sb.AppendLine();
            }

            lastBand = m.Band;
            sb.AppendLine(
                $"{m.Band,-7} {m.Name,-46} {row.BleedPct,7:0} {row.SeverPct,7:0} {FmtPct(row.BleedGivenSever),6} {FmtPct(row.BleedGivenNoSever),5} " +
                $"{row.MedianLoserBlood,7:0} {row.MedianWinnerBlood,6:0} {FmtSec(row.MedianSeverToDeathSeconds),6} {row.InstantBleedPct,6:0}");
        }

        sb.AppendLine();
        sb.AppendLine("--- Band medians ---");
        foreach (var (band, values) in bandMedians)
        {
            values.Sort();
            sb.AppendLine($"  {band}: median-of-medians {values[values.Count / 2]:0.0}s   range {values[0]:0.0}-{values[^1]:0.0}s");
        }

        AppendSeverDumpVerdict(sb, rows.Select(r => r.Row).ToList());

        Flush();
        if (File.Exists(sidecarPath))
        {
            File.Delete(sidecarPath);
        }

        _output.WriteLine(sb.ToString());
    }

    private sealed record StoredRow(string Band, string Name, MatchupResult Row);

    private static string FormatMatchupLine(Matchup m, MatchupResult row) =>
        $"{m.Band,-7} {m.Name,-46} {row.MedianSeconds,6:0.0} {row.MeanSeconds,6:0.0} {row.P10,5:0.0} {row.P90,5:0.0} " +
        $"{row.BandPct,6:0} {row.AWinPct,6:0} {row.BleedPct,7:0} {row.OrganPct,7:0} {row.SeverPct,7:0} " +
        $"{row.MedianLoserBlood,7:0} {row.MedianWinnerBlood,6:0} {row.MedianDps,5:0.0}  " +
        $"{Trunc(row.TopCause, 36)} ({row.TopCauseCount})";

    private sealed record MatchupResult(
        double MedianSeconds,
        double MeanSeconds,
        double P10,
        double P90,
        double BandPct,
        double AWinPct,
        double BleedPct,
        double OrganPct,
        double SeverPct,
        double BleedGivenSever,
        double BleedGivenNoSever,
        double MedianLoserBlood,
        double MedianBleedLoserBlood,
        double MedianWinnerBlood,
        double MedianSeverToDeathSeconds,
        double InstantBleedPct,
        int WithSever,
        int BleedWithSever,
        int BleedWithoutSever,
        int WithoutSever,
        int InstantBleed,
        double MedianDps,
        string TopCause,
        int TopCauseCount);

    private static MatchupResult RunMatchup(Matchup m)
    {
        var ticks = new List<int>(SeedCount);
        var dps = new List<double>(SeedCount);
        var loserBlood = new List<double>(SeedCount);
        var bleedLoserBlood = new List<double>();
        var winnerBlood = new List<double>(SeedCount);
        var severToDeath = new List<double>();
        var inBand = 0;
        var aWins = 0;
        var bleed = 0;
        var organ = 0;
        var withSever = 0;
        var withoutSever = 0;
        var bleedWithSever = 0;
        var bleedWithoutSever = 0;
        var instantBleed = 0;
        var causes = new Dictionary<string, int>();

        for (var seed = 1; seed <= SeedCount; seed++)
        {
            int t;
            string cause;
            double atkDps = 0;
            double loseB = 0;
            double winB = 0;
            var severs = 0;
            int? firstSeverTick = null;
            try
            {
                var sim = DuelSimulator.Simulate(m.Attacker, m.Defender, seed);
                t = sim.Result.Ticks;
                cause = sim.Result.CauseOfDeath ?? "(none/draw)";
                atkDps = sim.Analytics.Attacker.DamagePerSecond;
                severs = sim.Analytics.Attacker.Severs + sim.Analytics.Defender.Severs;
                firstSeverTick = FirstSeverTick(sim.Log);
                var aWon = sim.Result.WinnerPlayerId == "A";
                if (aWon)
                {
                    aWins++;
                    winB = sim.Analytics.Attacker.BloodPercent * 100;
                    loseB = sim.Analytics.Defender.BloodPercent * 100;
                }
                else
                {
                    winB = sim.Analytics.Defender.BloodPercent * 100;
                    loseB = sim.Analytics.Attacker.BloodPercent * 100;
                }
            }
            catch (TimeoutException)
            {
                t = CombatReplay.MaxTicks;
                cause = "(timeout/unresolved)";
            }

            ticks.Add(t);
            dps.Add(atkDps);
            loserBlood.Add(loseB);
            winnerBlood.Add(winB);
            if (t is >= TargetMinTicks and <= TargetMaxTicks)
            {
                inBand++;
            }

            var isBleed = IsBleed(cause);
            if (isBleed)
            {
                bleed++;
                bleedLoserBlood.Add(loseB);
            }
            else if (IsOrgan(cause))
            {
                organ++;
            }

            if (severs > 0)
            {
                withSever++;
                if (isBleed)
                {
                    bleedWithSever++;
                }

                if (firstSeverTick is int severTick)
                {
                    severToDeath.Add((t - severTick) / 60.0);
                    if (isBleed && t - severTick <= 60)
                    {
                        instantBleed++;
                    }
                }
            }
            else
            {
                withoutSever++;
                if (isBleed)
                {
                    bleedWithoutSever++;
                }
            }

            causes[cause] = causes.GetValueOrDefault(cause) + 1;
        }

        ticks.Sort();
        dps.Sort();
        loserBlood.Sort();
        bleedLoserBlood.Sort();
        winnerBlood.Sort();
        severToDeath.Sort();
        var top = causes.OrderByDescending(kv => kv.Value).First();
        return new MatchupResult(
            MedianSeconds: ticks[ticks.Count / 2] / 60.0,
            MeanSeconds: ticks.Average() / 60.0,
            P10: ticks[(int)(ticks.Count * 0.10)] / 60.0,
            P90: ticks[(int)(ticks.Count * 0.90)] / 60.0,
            BandPct: 100.0 * inBand / SeedCount,
            AWinPct: 100.0 * aWins / SeedCount,
            BleedPct: 100.0 * bleed / SeedCount,
            OrganPct: 100.0 * organ / SeedCount,
            SeverPct: 100.0 * withSever / SeedCount,
            BleedGivenSever: withSever == 0 ? double.NaN : 100.0 * bleedWithSever / withSever,
            BleedGivenNoSever: withoutSever == 0 ? double.NaN : 100.0 * bleedWithoutSever / withoutSever,
            MedianLoserBlood: loserBlood[loserBlood.Count / 2],
            MedianBleedLoserBlood: bleedLoserBlood.Count == 0 ? double.NaN : bleedLoserBlood[bleedLoserBlood.Count / 2],
            MedianWinnerBlood: winnerBlood[winnerBlood.Count / 2],
            MedianSeverToDeathSeconds: severToDeath.Count == 0 ? double.NaN : severToDeath[severToDeath.Count / 2],
            InstantBleedPct: 100.0 * instantBleed / SeedCount,
            WithSever: withSever,
            BleedWithSever: bleedWithSever,
            BleedWithoutSever: bleedWithoutSever,
            WithoutSever: withoutSever,
            InstantBleed: instantBleed,
            MedianDps: dps[dps.Count / 2],
            TopCause: top.Key,
            TopCauseCount: top.Value);
    }

    private static void AppendHumanBloodShares(StringBuilder sb)
    {
        using var human = BodyTestHarness.Human();
        var body = human.Pawn.Body;
        var total = body.AllParts.Sum(p => p.BloodAmount);
        var maxBlood = body.MaxBlood;

        sb.AppendLine("--- Human blood shares if severed (subtree / body @ full pool) ---");
        sb.AppendLine($"  Weight total {total:0.#}   MaxBlood {maxBlood:0}");

        void Row(string name, BodyPart part)
        {
            var weight = part.GetSubtreeBloodWeight();
            sb.AppendLine($"  {name,-22} {100f * weight / total,5:0.0}%   {maxBlood * weight / total,5:0} blood");
        }

        Row("Finger", human.External(BodyPartType.Finger));
        Row("Hand+digits", human.External(BodyPartType.Hand));
        Row("Arm+hand", human.External(BodyPartType.Arm));
        Row("Foot", human.External(BodyPartType.Foot));
        Row("Leg+foot", human.External(BodyPartType.Leg));
        sb.AppendLine($"  {"Head (own, not severable)",-22} {100f * human.External(BodyPartType.Head).BloodAmount / total,5:0.0}%   {maxBlood * human.External(BodyPartType.Head).BloodAmount / total,5:0} blood");
        sb.AppendLine($"  {"Torso (own)",-22} {100f * human.External(BodyPartType.Torso).BloodAmount / total,5:0.0}%   {maxBlood * human.External(BodyPartType.Torso).BloodAmount / total,5:0} blood");
    }

    private static void AppendSeverDumpVerdict(StringBuilder sb, List<MatchupResult> rows)
    {
        var withSever = rows.Sum(r => r.WithSever);
        var withoutSever = rows.Sum(r => r.WithoutSever);
        var bleedWith = rows.Sum(r => r.BleedWithSever);
        var bleedWithout = rows.Sum(r => r.BleedWithoutSever);
        var instant = rows.Sum(r => r.InstantBleed);
        var fights = rows.Count * SeedCount;
        var bleedGivenSever = withSever == 0 ? double.NaN : 100.0 * bleedWith / withSever;
        var bleedGivenNo = withoutSever == 0 ? double.NaN : 100.0 * bleedWithout / withoutSever;
        var instantPct = fights == 0 ? 0 : 100.0 * instant / fights;
        var bleedLoseBloods = rows.Select(r => r.MedianBleedLoserBlood).Where(v => !double.IsNaN(v)).OrderBy(v => v).ToList();
        var medianLose = bleedLoseBloods.Count == 0 ? double.NaN : bleedLoseBloods[bleedLoseBloods.Count / 2];

        sb.AppendLine();
        sb.AppendLine("--- Sever dump check ---");
        sb.AppendLine($"  Fights: {fights}   with sever: {withSever} ({100.0 * withSever / fights:0}%)   bleed deaths: {bleedWith + bleedWithout} ({100.0 * (bleedWith + bleedWithout) / fights:0}%)");
        sb.AppendLine($"  bleed|sever {FmtPct(bleedGivenSever)}   bleed|no-sever {FmtPct(bleedGivenNo)}");
        sb.AppendLine($"  Instant bleed-out within 1s of first sever: {instantPct:0.0}% ({instant}/{fights})");
        sb.AppendLine($"  Median-of-medians loser blood on bleed deaths: {(double.IsNaN(medianLose) ? "n/a" : $"{medianLose:0}%")}");

        var notes = new List<string>();
        if (!double.IsNaN(bleedGivenSever) && !double.IsNaN(bleedGivenNo) && bleedGivenSever + 5 >= bleedGivenNo)
        {
            notes.Add("severs raise or hold bleed deaths (dump + stump hemorrhage matter)");
        }
        else if (!double.IsNaN(bleedGivenSever) && !double.IsNaN(bleedGivenNo))
        {
            notes.Add("WARN: fights with a sever bleed out less often than fights without — dump may be too small or severs hit after the kill");
        }

        if (instantPct <= 15)
        {
            notes.Add("dump is not a one-shot (instant bleed-out after sever is rare)");
        }
        else
        {
            notes.Add("WARN: too many bleed-outs within 1s of sever — dump may be too large");
        }

        if (double.IsNaN(medianLose))
        {
            notes.Add("no bleed deaths to judge remaining blood");
        }
        else if (medianLose <= 15)
        {
            notes.Add("bleed-death loser blood is low (pool is actually spent)");
        }
        else
        {
            notes.Add("WARN: bleed-death losers still have a lot of blood — dump or hemorrhage may be too weak");
        }

        sb.AppendLine($"  Verdict: {string.Join("; ", notes)}");
    }

    private static int? FirstSeverTick(IReadOnlyList<CombatLogEvent> log)
    {
        int? first = null;
        foreach (var ev in log)
        {
            if (ev.Kind == CombatEventKind.PartSevered)
            {
                first = first is int t ? Math.Min(t, ev.Tick) : ev.Tick;
            }

            foreach (var sub in ev.SubEffects)
            {
                if (sub.Kind == CombatEventKind.PartSevered)
                {
                    first = first is int t ? Math.Min(t, ev.Tick) : ev.Tick;
                }
            }
        }

        return first;
    }

    private static bool IsBleed(string cause) =>
        cause.Contains("Blood", StringComparison.OrdinalIgnoreCase);

    private static bool IsOrgan(string cause) =>
        cause.Contains("failed", StringComparison.OrdinalIgnoreCase)
        || cause.Contains("destroyed", StringComparison.OrdinalIgnoreCase);

    private static string Trunc(string s, int n) => s.Length <= n ? s : s[..n];

    private static string FmtPct(double value) => double.IsNaN(value) ? "  n/a" : $"{value,5:0}";

    private static string FmtSec(double value) => double.IsNaN(value) ? "   n/a" : $"{value,5:0.0}";
}
