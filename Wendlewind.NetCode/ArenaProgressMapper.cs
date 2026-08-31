using Wendlewind.Definitions;
using Wendlewind.NetCode.Contracts;
using Wendlewind.Sim.Arena;

namespace Wendlewind.NetCode;

public static class ArenaProgressMapper
{
    public static ArenaProgressRecord FromRun(
        ArenaRun run,
        BuildSnapshot? loadout,
        string runId,
        DateTimeOffset startedAt)
    {
        return new ArenaProgressRecord
        {
            RunId = runId,
            PlayerId = run.PlayerId,
            PlayerName = run.PlayerName,
            RunSeed = run.RunSeed,
            Gold = run.Gold,
            Wins = run.Wins,
            Losses = run.Losses,
            Phase = run.Phase.ToString(),
            CurrentMerchantMoniker = run.CurrentMerchant?.Moniker,
            FoughtPlayerIds = [..run.FoughtPlayerIds],
            LastOpponentPlayerId = run.LastOpponentPlayerId,
            LastFightWon = run.LastFightWon,
            LastGoldDelta = run.LastGoldDelta,
            Loadout = loadout,
            StartedAt = startedAt,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    public static void ApplyTo(ArenaRun run, ArenaProgressRecord record)
    {
        run.PlayerId = record.PlayerId;
        run.PlayerName = record.PlayerName;
        run.RunSeed = record.RunSeed;
        run.Gold = record.Gold;
        run.Wins = record.Wins;
        run.Losses = record.Losses;
        run.FoughtPlayerIds = [..record.FoughtPlayerIds];
        run.LastOpponentPlayerId = record.LastOpponentPlayerId;
        run.LastFightWon = record.LastFightWon;
        run.LastGoldDelta = record.LastGoldDelta;
        run.CurrentMerchant = string.IsNullOrWhiteSpace(record.CurrentMerchantMoniker)
            ? null
            : DefRepository<MerchantDef>.GetByMoniker(record.CurrentMerchantMoniker, raiseError: false);
        run.SetPhase(Enum.TryParse<ArenaPhase>(record.Phase, out var phase) ? phase : ArenaPhase.GeneralStore);
    }
}
